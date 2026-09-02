import { Component, forwardRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { RepositoryService } from '../../services/repository.service';
import { Branch } from '../../models/branch.model';

/**
 * Searchable branch picker for a single (fixed-for-the-component-lifetime) repository.
 * Implements ControlValueAccessor so it drops into a Reactive Forms FormGroup via formControlName,
 * matching the rest of the deployment row's form-driven validation.
 */
@Component({
  selector: 'app-branch-selector',
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './branch-selector.html',
  styleUrl: './branch-selector.scss',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => BranchSelector), multi: true }]
})
export class BranchSelector implements ControlValueAccessor {
  readonly repository = input<string | null>(null);

  private readonly repositoryService = inject(RepositoryService);

  protected readonly control = new FormControl<string>('', { nonNullable: true });
  protected readonly branches = signal<Branch[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  private previousRepository: string | null = null;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    toObservable(this.repository)
      .pipe(
        switchMap((repo) => {
          this.branches.set([]);
          if (!repo) {
            return of(null);
          }

          const repositoryChanged = this.previousRepository !== null && this.previousRepository !== repo;
          this.previousRepository = repo;
          if (repositoryChanged) {
            this.control.setValue('');
          }

          this.loading.set(true);
          this.error.set(null);
          return this.repositoryService.getBranches(repo).pipe(
            catchError(() => {
              this.error.set('Failed to load branches.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (page) {
          this.branches.set(page.items);
        }
      });

    this.control.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          this.onChange(term);
          const repo = this.repository();
          const alreadyLoaded = this.branches().some((b) => b.name === term);
          if (!repo || alreadyLoaded) {
            return of(null);
          }
          this.loading.set(true);
          return this.repositoryService.getBranches(repo, term || undefined).pipe(
            catchError(() => {
              this.error.set('Failed to search branches.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (page) {
          this.branches.set(page.items);
        }
      });
  }

  protected markTouched(): void {
    this.onTouched();
  }

  writeValue(value: string | null): void {
    this.control.setValue(value ?? '', { emitEvent: false });
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) {
      this.control.disable({ emitEvent: false });
    } else {
      this.control.enable({ emitEvent: false });
    }
  }
}
