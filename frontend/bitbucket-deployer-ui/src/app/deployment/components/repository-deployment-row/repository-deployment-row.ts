import { Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BranchSelector } from '../branch-selector/branch-selector';
import { PipelineSelector } from '../pipeline-selector/pipeline-selector';

export interface DeploymentRowFormControls {
  branch: FormControl<string>;
  pipeline: FormControl<string>;
}

/** One selected-repository row: repo name + its branch/pipeline pickers, bound to a Reactive Forms group. */
@Component({
  selector: 'app-repository-deployment-row',
  imports: [ReactiveFormsModule, BranchSelector, PipelineSelector, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './repository-deployment-row.html',
  styleUrl: './repository-deployment-row.scss'
})
export class RepositoryDeploymentRow {
  readonly formGroup = input.required<FormGroup<DeploymentRowFormControls>>();
  readonly repositoryName = input.required<string>();
  readonly repositorySlug = input.required<string>();

  readonly removeRow = output<void>();

  protected currentBranchValue(): string {
    return this.formGroup().controls.branch.value;
  }
}
