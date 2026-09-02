import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { DeploymentEnvironment } from '../../models/deployment-environment.model';
import { DeploymentSummary, DeploymentSummaryRow } from '../deployment-summary/deployment-summary';

export interface DeploymentConfirmationData {
  environment: DeploymentEnvironment;
  rows: DeploymentSummaryRow[];
}

/** "Deploy to {env}?" dialog. Production requires an extra explicit acknowledgement (section 18). */
@Component({
  selector: 'app-deployment-confirmation',
  imports: [MatButtonModule, MatCheckboxModule, MatDialogModule, DeploymentSummary],
  templateUrl: './deployment-confirmation.html',
  styleUrl: './deployment-confirmation.scss'
})
export class DeploymentConfirmation {
  protected readonly data = inject<DeploymentConfirmationData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<DeploymentConfirmation, boolean>);

  protected readonly extraConfirmationChecked = signal(false);

  protected get canConfirm(): boolean {
    return !this.data.environment.requiresExtraConfirmation || this.extraConfirmationChecked();
  }

  protected cancel(): void {
    this.dialogRef.close(false);
  }

  protected confirm(): void {
    if (this.canConfirm) {
      this.dialogRef.close(true);
    }
  }
}
