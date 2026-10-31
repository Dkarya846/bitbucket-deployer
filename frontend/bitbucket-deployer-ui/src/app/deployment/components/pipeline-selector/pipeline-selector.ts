import { Component, forwardRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, combineLatest, of, switchMap } from 'rxjs';
import { RepositoryService } from '../../services/repository.service';
import { Pipeline } from '../../models/pipeline.model';

/**
 * Custom pipeline picker for a fixed repository + (reactively updated) branch. Disabled until a
 * branch is chosen; definitions differ per repository/branch so nothing is assumed or cached
 * across rows (see BitbucketService.GetPipelineDefinitionsAsync on the backend for why).
 */
@Component({
  selector: 'app-pipeline-selector',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatIconModule, MatProgressSpinnerModule, MatSelectModule, MatTooltipModule],
  templateUrl: './pipeline-selector.html',
  styleUrl: './pipeline-selector.scss',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => PipelineSelector), multi: true }]
})
export class PipelineSelector implements ControlValueAccessor {
  readonly repository = input<string | null>(null);
  readonly branch = input<string | null>(null);

  /** The full selected Pipeline (so the parent row can read its variable definitions) — null when nothing valid is selected. */
  readonly pipelineSelected = output<Pipeline | null>();

  private readonly repositoryService = inject(RepositoryService);

  protected readonly control = new FormControl<string>({ value: '', disabled: true }, { nonNullable: true });
  protected readonly pipelines = signal<Pipeline[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    combineLatest([toObservable(this.repository), toObservable(this.branch)])
      .pipe(
        switchMap(([repo, branch]) => {
          this.pipelines.set([]);
          this.error.set(null);

          if (!repo || !branch) {
            this.control.disable({ emitEvent: false });
            return of(null);
          }

          this.loading.set(true);
          return this.repositoryService.getPipelines(repo, branch).pipe(
            catchError(() => {
              this.error.set('Failed to load pipelines.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((list) => {
        this.loading.set(false);
        if (list) {
          this.pipelines.set(list);
          this.control.enable({ emitEvent: false });
          if (!list.some((p) => p.pattern === this.control.value)) {
            this.control.setValue('');
          }
        }
      });

    this.control.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.onChange(value);
      this.pipelineSelected.emit(this.pipelines().find((p) => p.pattern === value) ?? null);
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
    if (isDisabled || !this.branch()) {
      this.control.disable({ emitEvent: false });
    } else {
      this.control.enable({ emitEvent: false });
    }
  }
}
