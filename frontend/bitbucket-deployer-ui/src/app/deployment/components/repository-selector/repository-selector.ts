import { Component, computed, inject, model, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { RepositoryService } from '../../services/repository.service';
import { Repository } from '../../models/repository.model';

/**
 * Self-contained repository search + multi-select. Owns its own fetch/loading/error state;
 * the parent only cares about which repositories are selected (two-way bound via `selected`).
 */
@Component({
  selector: 'app-repository-selector',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './repository-selector.html',
  styleUrl: './repository-selector.scss'
})
export class RepositorySelector {
  private readonly repositoryService = inject(RepositoryService);

  readonly selected = model<Repository[]>([]);

  protected readonly searchTerm = signal('');
  protected readonly repositories = signal<Repository[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly nextPageToken = signal<string | null>(null);
  protected readonly loadingMore = signal(false);

  protected readonly allVisibleSelected = computed(() => {
    const selectedSlugs = new Set(this.selected().map((r) => r.slug));
    return this.repositories().length > 0 && this.repositories().every((r) => selectedSlugs.has(r.slug));
  });

  constructor() {
    toObservable(this.searchTerm)
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          this.loading.set(true);
          this.error.set(null);
          return this.repositoryService.getRepositories(term || undefined).pipe(
            catchError(() => {
              this.error.set('Failed to load repositories. Please try again.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (page) {
          this.repositories.set(page.items);
          this.nextPageToken.set(page.nextPageToken);
        }
      });
  }

  protected loadMore(): void {
    const token = this.nextPageToken();
    if (!token || this.loadingMore()) {
      return;
    }

    this.loadingMore.set(true);
    this.repositoryService.getRepositories(this.searchTerm() || undefined, token).subscribe({
      next: (page) => {
        this.repositories.update((current) => [...current, ...page.items]);
        this.nextPageToken.set(page.nextPageToken);
        this.loadingMore.set(false);
      },
      error: () => {
        this.error.set('Failed to load more repositories.');
        this.loadingMore.set(false);
      }
    });
  }

  protected isSelected(slug: string): boolean {
    return this.selected().some((r) => r.slug === slug);
  }

  protected toggle(repository: Repository, checked: boolean): void {
    if (checked) {
      if (!this.isSelected(repository.slug)) {
        this.selected.set([...this.selected(), repository]);
      }
    } else {
      this.selected.set(this.selected().filter((r) => r.slug !== repository.slug));
    }
  }

  protected selectAllVisible(): void {
    const bySlug = new Map(this.selected().map((r) => [r.slug, r]));
    this.repositories().forEach((r) => bySlug.set(r.slug, r));
    this.selected.set([...bySlug.values()]);
  }

  protected clearAll(): void {
    this.selected.set([]);
  }
}
