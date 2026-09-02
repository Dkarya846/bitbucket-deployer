import { TestBed } from '@angular/core/testing';
import { DeploymentSummary, DeploymentSummaryRow } from './deployment-summary';

describe('DeploymentSummary', () => {
  const rows: DeploymentSummaryRow[] = [
    { repository: 'ss-web', branch: 'uat', pipeline: 'deploy-uat', isValid: true },
    { repository: 'ss-api', branch: 'uat', pipeline: 'deploy-uat', isValid: false, errorMessage: 'Pipeline does not exist.' }
  ];

  it('renders one row per entry with repository and branch/pipeline text', async () => {
    const fixture = TestBed.createComponent(DeploymentSummary);
    fixture.componentRef.setInput('rows', rows);
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('li').length).toBe(2);
    expect(el.textContent).toContain('ss-web');
    expect(el.textContent).toContain('ss-api');
  });

  it('shows the error message only for invalid rows when validation display is on', async () => {
    const fixture = TestBed.createComponent(DeploymentSummary);
    fixture.componentRef.setInput('rows', rows);
    fixture.componentRef.setInput('showValidation', true);
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Pipeline does not exist.');
    expect(el.querySelectorAll('.status-icon.invalid').length).toBe(1);
    expect(el.querySelectorAll('.status-icon.valid').length).toBe(1);
  });

  it('renders plain numbered rows when validation display is off', async () => {
    const fixture = TestBed.createComponent(DeploymentSummary);
    fixture.componentRef.setInput('rows', rows);
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.index')?.textContent?.trim()).toBe('1.');
    expect(el.querySelector('.status-icon')).toBeFalsy();
  });
});
