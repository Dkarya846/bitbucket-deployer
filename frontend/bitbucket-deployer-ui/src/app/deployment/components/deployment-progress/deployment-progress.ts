import { Component, input } from '@angular/core';
import { DeploymentItem } from '../../models/deployment.model';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-deployment-progress',
  imports: [StatusBadge],
  templateUrl: './deployment-progress.html',
  styleUrl: './deployment-progress.scss'
})
export class DeploymentProgress {
  readonly items = input.required<DeploymentItem[]>();
}
