import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { DeploymentDashboard } from './deployment-dashboard';

describe('DeploymentDashboard', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<DeploymentDashboard>>;
  // Cast to `any` deliberately: the fields under test (`rows`, `selectedRepositories`,
  // `buildRequest`) are intentionally `protected`/`private` — template-only state, not a public
  // API — so reaching into them here is a pragmatic test-only exception, not a design change.
  let component: any;
  let httpMock: HttpTestingController;

  const repoA = { slug: 'ss-web', name: 'ss-web', mainBranch: 'master' };

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [DeploymentDashboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideNoopAnimations()]
    });
    fixture = TestBed.createComponent(DeploymentDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
    // repository-selector fires an initial search on construction — drain it so it doesn't
    // leak into other tests via httpMock.verify().
    httpMock.match(() => true).forEach((req) => req.flush({ items: [], nextPageToken: null }));
  });

  afterEach(() => httpMock.verify());

  /**
   * Selecting a repository makes its row mount a branch-selector (and, once a branch value
   * exists, a pipeline-selector) — each fires its own fetch, with different response shapes.
   */
  async function selectRepositories(repos: typeof repoA[]): Promise<void> {
    component.selectedRepositories.set(repos);
    fixture.detectChanges();
    await fixture.whenStable();
    httpMock.match((r) => r.url.includes('/branches')).forEach((req) => req.flush({ items: [], nextPageToken: null }));
    httpMock.match((r) => r.url.includes('/pipelines')).forEach((req) => req.flush([]));
  }

  it('adds a row when a repository is selected, and removes it when deselected', async () => {
    await selectRepositories([repoA]);

    expect(component.rows().length).toBe(1);
    expect(component.rows()[0].slug).toBe('ss-web');

    await selectRepositories([]);

    expect(component.rows().length).toBe(0);
  });

  it('preserves an existing row (and its form values) when the same repository stays selected', async () => {
    await selectRepositories([repoA]);

    const originalForm = component.rows()[0].form;
    originalForm.controls.branch.setValue('uat');

    // Re-affirming the same selection should not recreate the row's form.
    await selectRepositories([repoA]);

    expect(component.rows()[0].form).toBe(originalForm);
    expect(component.rows()[0].form.controls.branch.value).toBe('uat');
  });

  it('canSubmit is false until every row has a branch and pipeline selected', async () => {
    await selectRepositories([repoA]);

    expect(component.canSubmit()).toBe(false);

    const form = component.rows()[0].form;
    form.controls.branch.setValue('uat');
    form.controls.pipeline.setValue('deploy-uat');

    expect(component.canSubmit()).toBe(true);
  });

  it('buildRequest reflects the selected environment and each row', async () => {
    await selectRepositories([repoA]);

    component.rows()[0].form.controls.branch.setValue('uat');
    component.rows()[0].form.controls.pipeline.setValue('deploy-uat');
    component.selectedEnvironmentId.set('production');

    expect(component.buildRequest()).toEqual({
      environment: 'production',
      repositories: [{ repository: 'ss-web', branch: 'uat', pipeline: 'deploy-uat' }]
    });
  });
});
