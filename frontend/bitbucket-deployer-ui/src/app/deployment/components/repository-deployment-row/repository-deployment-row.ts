import { Component, output, input, signal } from '@angular/core';
import { FormControl, FormGroup, FormRecord, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BranchSelector } from '../branch-selector/branch-selector';
import { PipelineSelector } from '../pipeline-selector/pipeline-selector';
import { Pipeline, PipelineVariable } from '../../models/pipeline.model';

export interface DeploymentRowFormControls {
  branch: FormControl<string>;
  pipeline: FormControl<string>;
  /** Dynamic set of controls, one per variable the currently selected pipeline declares — see onPipelineSelected. */
  variables: FormRecord<FormControl<string>>;
}

const SENSITIVE_NAME_MARKERS = ['PASSWORD', 'SECRET', 'TOKEN', 'KEY', 'CREDENTIAL'];

/** One selected-repository row: repo name + its branch/pipeline pickers, bound to a Reactive Forms group. */
@Component({
  selector: 'app-repository-deployment-row',
  imports: [
    ReactiveFormsModule,
    BranchSelector,
    PipelineSelector,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule
  ],
  templateUrl: './repository-deployment-row.html',
  styleUrl: './repository-deployment-row.scss'
})
export class RepositoryDeploymentRow {
  readonly formGroup = input.required<FormGroup<DeploymentRowFormControls>>();
  readonly repositoryName = input.required<string>();
  readonly repositorySlug = input.required<string>();

  readonly removeRow = output<void>();

  protected readonly selectedPipelineVariables = signal<PipelineVariable[]>([]);

  protected currentBranchValue(): string {
    return this.formGroup().controls.branch.value;
  }

  /**
   * Rebuilds the `variables` FormRecord to match the newly selected pipeline's declared
   * variables, pre-filled with defaults. A variable with no default is marked required, so a
   * row with missing required variables correctly makes formGroup().valid false — the dashboard's
   * canSubmit() picks this up for free, the same way it already does for branch/pipeline.
   */
  protected onPipelineSelected(pipeline: Pipeline | null): void {
    const variablesGroup = this.formGroup().controls.variables;

    for (const name of Object.keys(variablesGroup.controls)) {
      variablesGroup.removeControl(name, { emitEvent: false });
    }

    const variables = pipeline?.variables ?? [];
    for (const variable of variables) {
      variablesGroup.addControl(
        variable.name,
        new FormControl(variable.default ?? '', {
          nonNullable: true,
          validators: variable.default === undefined ? [Validators.required] : []
        }),
        { emitEvent: false }
      );
    }

    this.selectedPipelineVariables.set(variables);
  }

  protected isSensitive(variableName: string): boolean {
    const upper = variableName.toUpperCase();
    return SENSITIVE_NAME_MARKERS.some((marker) => upper.includes(marker));
  }
}
