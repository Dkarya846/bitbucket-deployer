import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface DeploymentSummaryRow {
  repository: string;
  branch: string;
  pipeline: string;
  /** undefined = plain summary (e.g. confirmation dialog); boolean = validation result. */
  isValid?: boolean;
  errorMessage?: string;
}

/** Read-only repository/branch/pipeline list, reused by the validation panel and the confirmation dialog. */
@Component({
  selector: 'app-deployment-summary',
  imports: [MatIconModule],
  templateUrl: './deployment-summary.html',
  styleUrl: './deployment-summary.scss'
})
export class DeploymentSummary {
  readonly rows = input.required<DeploymentSummaryRow[]>();
  readonly showValidation = input(false);
}
