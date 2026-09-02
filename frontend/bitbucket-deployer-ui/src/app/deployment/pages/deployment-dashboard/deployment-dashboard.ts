import { Component, effect, signal, untracked } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ApiError } from '../../../core/api-error.model';
import { DeploymentConfirmation, DeploymentConfirmationData } from '../../components/deployment-confirmation/deployment-confirmation';
import { DeploymentSummary, DeploymentSummaryRow } from '../../components/deployment-summary/deployment-summary';
import { RepositoryDeploymentRow, DeploymentRowFormControls } from '../../components/repository-deployment-row/repository-deployment-row';
import { RepositorySelector } from '../../components/repository-selector/repository-selector';
import { DEPLOYMENT_ENVIRONMENTS, DeploymentEnvironment } from '../../models/deployment-environment.model';
import { CreateDeploymentRequest, ValidationResult } from '../../models/deployment.model';
import { Repository } from '../../models/repository.model';
import { DeploymentService } from '../../services/deployment.service';

interface DeploymentRow {
  slug: string;
  name: string;
  form: FormGroup<DeploymentRowFormControls>;
}

@Component({
  selector: 'app-deployment-dashboard',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
    DeploymentSummary,
    RepositoryDeploymentRow,
    RepositorySelector
  ],
  templateUrl: './deployment-dashboard.html',
  styleUrl: './deployment-dashboard.scss'
})
export class DeploymentDashboard {
  private readonly deploymentService = inject(DeploymentService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly environments = DEPLOYMENT_ENVIRONMENTS;
  protected readonly selectedEnvironmentId = signal(DEPLOYMENT_ENVIRONMENTS[0].id);

  protected readonly selectedRepositories = signal<Repository[]>([]);
  protected readonly rows = signal<DeploymentRow[]>([]);

  protected readonly validating = signal(false);
  protected readonly validationResult = signal<ValidationResult | null>(null);
  protected readonly validationError = signal<string | null>(null);

  protected readonly deploying = signal(false);
  protected readonly deployError = signal<string | null>(null);

  constructor() {
    // reconcileRows reads the `rows` signal (to preserve existing row form state) as well as
    // writing it — without `untracked`, that read makes `rows` a dependency of this effect too,
    // so writing it re-triggers the effect in an infinite loop.
    effect(() => {
      const selected = this.selectedRepositories();
      untracked(() => this.reconcileRows(selected));
    });
  }

  protected selectedEnvironment(): DeploymentEnvironment {
    return this.environments.find((e) => e.id === this.selectedEnvironmentId()) ?? this.environments[0];
  }

  protected canSubmit(): boolean {
    const rows = this.rows();
    return rows.length > 0 && rows.every((r) => r.form.valid);
  }

  protected validationSummaryRows(): DeploymentSummaryRow[] {
    const result = this.validationResult();
    if (!result) {
      return [];
    }
    return result.items.map((item) => {
      const row = this.rows().find((r) => r.slug === item.repository);
      return {
        repository: row?.name ?? item.repository,
        branch: item.branch,
        pipeline: item.pipeline,
        isValid: item.isValid,
        errorMessage: item.errorMessage
      };
    });
  }

  protected runValidation(): void {
    if (this.validating() || this.deploying() || !this.canSubmit()) {
      return;
    }

    this.validating.set(true);
    this.validationError.set(null);
    this.deploymentService.validate(this.buildRequest()).subscribe({
      next: (result) => {
        this.validationResult.set(result);
        this.validating.set(false);
      },
      error: (error: ApiError) => {
        this.validationError.set(error.message);
        this.validating.set(false);
      }
    });
  }

  protected openConfirmation(): void {
    if (this.deploying() || this.validating() || !this.canSubmit()) {
      return;
    }

    const environment = this.selectedEnvironment();
    const data: DeploymentConfirmationData = { environment, rows: this.buildSummaryRows() };

    this.dialog
      .open(DeploymentConfirmation, { data, width: '520px', autoFocus: false })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.submitDeployment();
        }
      });
  }

  private submitDeployment(): void {
    this.deploying.set(true);
    this.deployError.set(null);

    this.deploymentService.deploy(this.buildRequest()).subscribe({
      next: (deployment) => {
        this.deploying.set(false);
        this.router.navigate(['/deployments', deployment.id]);
      },
      error: (error: ApiError) => {
        this.deploying.set(false);
        this.deployError.set(error.message);
      }
    });
  }

  private buildRequest(): CreateDeploymentRequest {
    return {
      environment: this.selectedEnvironmentId(),
      repositories: this.rows().map((r) => ({
        repository: r.slug,
        branch: r.form.controls.branch.value,
        pipeline: r.form.controls.pipeline.value
      }))
    };
  }

  private buildSummaryRows(): DeploymentSummaryRow[] {
    return this.rows().map((r) => ({
      repository: r.name,
      branch: r.form.controls.branch.value,
      pipeline: r.form.controls.pipeline.value
    }));
  }

  private reconcileRows(selected: Repository[]): void {
    const selectedSlugs = new Set(selected.map((r) => r.slug));
    const kept = this.rows().filter((r) => selectedSlugs.has(r.slug));
    const keptSlugs = new Set(kept.map((r) => r.slug));

    const added = selected
      .filter((r) => !keptSlugs.has(r.slug))
      .map((r) => ({ slug: r.slug, name: r.name, form: this.createRowForm() }));

    this.rows.set([...kept, ...added]);
    this.validationResult.set(null);
  }

  private createRowForm(): FormGroup<DeploymentRowFormControls> {
    return new FormGroup<DeploymentRowFormControls>({
      branch: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      pipeline: new FormControl('', { nonNullable: true, validators: [Validators.required] })
    });
  }
}
