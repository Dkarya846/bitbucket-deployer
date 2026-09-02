import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { DeploymentStatus } from '../../models/deployment-status.model';

interface StatusPresentation {
  label: string;
  icon: string;
  cssClass: string;
}

const STATUS_PRESENTATION: Record<DeploymentStatus, StatusPresentation> = {
  Pending: { label: 'PENDING', icon: 'radio_button_unchecked', cssClass: 'status-pending' },
  Validating: { label: 'VALIDATING', icon: 'pending', cssClass: 'status-pending' },
  Queued: { label: 'QUEUED', icon: 'schedule', cssClass: 'status-pending' },
  Running: { label: 'RUNNING', icon: 'sync', cssClass: 'status-running' },
  Successful: { label: 'SUCCESS', icon: 'check_circle', cssClass: 'status-success' },
  Failed: { label: 'FAILED', icon: 'cancel', cssClass: 'status-failed' },
  Stopped: { label: 'STOPPED', icon: 'stop_circle', cssClass: 'status-failed' },
  Cancelled: { label: 'CANCELLED', icon: 'block', cssClass: 'status-failed' }
};

@Component({
  selector: 'app-status-badge',
  imports: [MatIconModule],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadge {
  readonly status = input.required<DeploymentStatus>();

  protected readonly presentation = computed(() => STATUS_PRESENTATION[this.status()]);
}
