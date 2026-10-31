import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, FormRecord, Validators } from '@angular/forms';
import { RepositoryDeploymentRow, DeploymentRowFormControls } from './repository-deployment-row';
import { Pipeline } from '../../models/pipeline.model';

describe('RepositoryDeploymentRow', () => {
  // No detectChanges() anywhere here — these tests only exercise onPipelineSelected's form
  // logic directly, so the template (and its HTTP-dependent child selectors) never renders,
  // which means no HttpClientTesting setup is needed.
  let component: any;
  let form: FormGroup<DeploymentRowFormControls>;

  function createRowForm(): FormGroup<DeploymentRowFormControls> {
    return new FormGroup<DeploymentRowFormControls>({
      branch: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      pipeline: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      variables: new FormRecord<FormControl<string>>({})
    });
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [RepositoryDeploymentRow] });
    const fixture = TestBed.createComponent(RepositoryDeploymentRow);
    component = fixture.componentInstance;
    form = createRowForm();
    fixture.componentRef.setInput('formGroup', form);
    fixture.componentRef.setInput('repositoryName', 'ss-web');
    fixture.componentRef.setInput('repositorySlug', 'ss-web');
  });

  const pipelineWithVariables: Pipeline = {
    pattern: 'deploy-with-approval',
    displayName: 'Deploy With Approval',
    variables: [
      { name: 'RELEASE_VERSION' },
      { name: 'CONFIRM', default: 'no', allowedValues: ['yes', 'no'] }
    ]
  };

  it('adds no variable controls for a pipeline with none declared', () => {
    component.onPipelineSelected({ pattern: 'deploy-uat', displayName: 'Deploy Uat', variables: [] } as Pipeline);

    expect(component.selectedPipelineVariables()).toEqual([]);
    expect(Object.keys(form.controls.variables.controls)).toHaveLength(0);
  });

  it('adds one control per declared variable, pre-filled with defaults', () => {
    component.onPipelineSelected(pipelineWithVariables);

    const controls = form.controls.variables.controls;
    expect(Object.keys(controls)).toEqual(['RELEASE_VERSION', 'CONFIRM']);
    expect(controls['RELEASE_VERSION'].value).toBe('');
    expect(controls['CONFIRM'].value).toBe('no');
  });

  it('marks a variable with no default as required, making the row invalid until filled', () => {
    component.onPipelineSelected(pipelineWithVariables);
    form.controls.branch.setValue('uat');
    form.controls.pipeline.setValue('deploy-with-approval');

    // CONFIRM has a default so it's already satisfied; RELEASE_VERSION is empty and required.
    expect(form.valid).toBe(false);

    form.controls.variables.controls['RELEASE_VERSION'].setValue('1.2.3');
    expect(form.valid).toBe(true);
  });

  it('does not require a variable that has a default', () => {
    component.onPipelineSelected(pipelineWithVariables);
    expect(form.controls.variables.controls['CONFIRM'].valid).toBe(true);
  });

  it('switching pipelines removes the previous pipeline\'s variable controls', () => {
    component.onPipelineSelected(pipelineWithVariables);
    expect(Object.keys(form.controls.variables.controls)).toHaveLength(2);

    component.onPipelineSelected({ pattern: 'migration', displayName: 'Migration', variables: [{ name: 'TARGET_DB' }] } as Pipeline);

    expect(Object.keys(form.controls.variables.controls)).toEqual(['TARGET_DB']);
  });

  it('selecting null (e.g. branch changed) clears all variable controls', () => {
    component.onPipelineSelected(pipelineWithVariables);

    component.onPipelineSelected(null);

    expect(Object.keys(form.controls.variables.controls)).toHaveLength(0);
    expect(component.selectedPipelineVariables()).toEqual([]);
  });

  it('isSensitive flags common secret-like variable names, case-insensitively', () => {
    expect(component.isSensitive('DEPLOY_TOKEN')).toBe(true);
    expect(component.isSensitive('db_password')).toBe(true);
    expect(component.isSensitive('RELEASE_VERSION')).toBe(false);
  });
});
