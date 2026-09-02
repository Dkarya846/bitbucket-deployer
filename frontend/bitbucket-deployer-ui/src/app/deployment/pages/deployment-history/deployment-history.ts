import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ApiError } from '../../../core/api-error.model';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { DeploymentListItem } from '../../models/deployment.model';
import { DeploymentService } from '../../services/deployment.service';

/**
 * No persistent store exists yet (see section 1/24 of the design brief) — this list reflects only
 * deployments triggered since the backend process last started, held in its in-memory store.
 */
@Component({
  selector: 'app-deployment-history',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule, StatusBadge],
  templateUrl: './deployment-history.html',
  styleUrl: './deployment-history.scss'
})
export class DeploymentHistory {
  private readonly deploymentService = inject(DeploymentService);

  protected readonly deployments = signal<DeploymentListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly displayedColumns = ['id', 'environment', 'createdBy', 'createdAt', 'repositoryCount', 'status'];

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.deploymentService.getHistory().subscribe({
      next: (deployments) => {
        this.deployments.set(deployments);
        this.loading.set(false);
      },
      error: (error: ApiError) => {
        this.error.set(error.message);
        this.loading.set(false);
      }
    });
  }
}
