import { Component, effect, signal, untracked } from '@angular/core';
import { FormControl, FormGroup, FormRecord, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { DeploymentEnvironment } from '../../models/deployment-environment.model';
import { CreateDeploymentRequest, ValidationResult } from '../../models/deployment.model';
import { Repository } from '../../models/repository.model';
import { DeploymentService } from '../../services/deployment.service';
import { RepositoryService } from '../../services/repository.service';

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
  private readonly repositoryService = inject(RepositoryService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly environments = signal<DeploymentEnvironment[]>([]);
  protected readonly selectedEnvironmentId = signal<string | null>(null);
  protected readonly environmentsLoading = signal(true);
  protected readonly environmentsError = signal<string | null>(null);

  protected readonly selectedRepositories = signal<Repository[]>([]);
  protected readonly rows = signal<DeploymentRow[]>([]);

  // Panel visibility only — [hidden], not @if, so collapsing never destroys the repository
  // selector's loaded list/search state or the row forms.
  protected readonly repositoriesPanelCollapsed = signal(false);
  protected readonly configPanelCollapsed = signal(false);

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

    this.loadEnvironments();
  }

  protected retryLoadEnvironments(): void {
    this.loadEnvironments();
  }

  private loadEnvironments(): void {
    this.environmentsLoading.set(true);
    this.environmentsError.set(null);
    this.repositoryService.getEnvironments().subscribe({
      next: (environments) => {
        this.environments.set(environments);
        this.selectedEnvironmentId.set(environments[0]?.id ?? null);
        this.environmentsLoading.set(false);
      },
      error: (error: ApiError) => {
        this.environmentsError.set(error.message);
        this.environmentsLoading.set(false);
      }
    });
  }

  protected selectedEnvironment(): DeploymentEnvironment | undefined {
    return this.environments().find((e) => e.id === this.selectedEnvironmentId());
  }

  protected canSubmit(): boolean {
    const rows = this.rows();
    return rows.length > 0 && rows.every((r) => r.form.valid) && this.selectedEnvironmentId() !== null;
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
    if (!environment) {
      return;
    }
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
    // Only ever called from paths gated by canSubmit(), which requires a non-null environment.
    return {
      environment: this.selectedEnvironmentId()!,
      repositories: this.rows().map((r) => ({
        repository: r.slug,
        branch: r.form.controls.branch.value,
        pipeline: r.form.controls.pipeline.value,
        variables: this.extractVariables(r.form.controls.variables)
      }))
    };
  }

  private extractVariables(variablesGroup: FormRecord<FormControl<string>>): Record<string, string> | undefined {
    const entries = Object.entries(variablesGroup.controls).map(([name, control]) => [name, control.value] as const);
    return entries.length > 0 ? Object.fromEntries(entries) : undefined;
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
      pipeline: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      // Populated dynamically per selected pipeline — see RepositoryDeploymentRow.onPipelineSelected.
      variables: new FormRecord<FormControl<string>>({})
    });
  }
}
