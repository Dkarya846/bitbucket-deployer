import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, of, switchMap, takeWhile, tap, timer } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiError } from '../../../core/api-error.model';
import { DeploymentProgress } from '../../components/deployment-progress/deployment-progress';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { isTerminalStatus } from '../../models/deployment-status.model';
import { Deployment } from '../../models/deployment.model';
import { DeploymentService } from '../../services/deployment.service';

@Component({
  selector: 'app-deployment-details',
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, DeploymentProgress, StatusBadge],
  templateUrl: './deployment-details.html',
  styleUrl: './deployment-details.scss'
})
export class DeploymentDetails {
  private readonly route = inject(ActivatedRoute);
  private readonly deploymentService = inject(DeploymentService);

  protected readonly deploymentId = this.route.snapshot.paramMap.get('deploymentId')!;
  protected readonly deployment = signal<Deployment | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly actionInFlight = signal(false);

  constructor() {
    this.poll();
  }

  protected canCancel(): boolean {
    const deployment = this.deployment();
    return !!deployment && !isTerminalStatus(deployment.status) && !this.actionInFlight();
  }

  protected canRetry(): boolean {
    const deployment = this.deployment();
    return !!deployment && deployment.status === 'Failed' && !this.actionInFlight();
  }

  protected cancelDeployment(): void {
    if (!this.canCancel()) {
      return;
    }
    this.actionInFlight.set(true);
    this.deploymentService.cancel(this.deploymentId).subscribe({
      next: (deployment) => {
        this.deployment.set(deployment);
        this.actionInFlight.set(false);
      },
      error: (error: ApiError) => {
        this.error.set(error.message);
        this.actionInFlight.set(false);
      }
    });
  }

  protected retryDeployment(): void {
    if (!this.canRetry()) {
      return;
    }
    this.actionInFlight.set(true);
    this.deploymentService.retry(this.deploymentId).subscribe({
      next: (deployment) => {
        this.deployment.set(deployment);
        this.actionInFlight.set(false);
        this.poll();
      },
      error: (error: ApiError) => {
        this.error.set(error.message);
        this.actionInFlight.set(false);
      }
    });
  }

  private poll(): void {
    timer(0, environment.pollingIntervalMs)
      .pipe(
        switchMap(() =>
          this.deploymentService.getDeployment(this.deploymentId).pipe(
            catchError((error: ApiError) => {
              this.error.set(error.message);
              this.loading.set(false);
              return of(null);
            })
          )
        ),
        tap((deployment) => {
          if (deployment) {
            this.deployment.set(deployment);
            this.error.set(null);
            this.loading.set(false);
          }
        }),
        takeWhile((deployment) => !deployment || !isTerminalStatus(deployment.status), true),
        takeUntilDestroyed()
      )
      .subscribe();
  }
}
