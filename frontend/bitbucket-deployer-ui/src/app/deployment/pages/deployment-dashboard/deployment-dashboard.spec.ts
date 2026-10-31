import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { apiErrorInterceptor } from '../../../core/api-error.interceptor';
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
      providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting(), provideRouter([]), provideNoopAnimations()]
    });
    fixture = TestBed.createComponent(DeploymentDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
    // The dashboard loads environments, and repository-selector fires an initial search, both on
    // construction — drain both so they don't leak into other tests via httpMock.verify(). Each
    // has a different response shape (plain array vs. paged), so they're matched separately.
    httpMock.match((r) => r.url.includes('/environments')).forEach((req) =>
      req.flush([{ id: 'uat', name: 'UAT', requiresExtraConfirmation: false }])
    );
    httpMock.match((r) => r.url.includes('/repositories') && !r.url.includes('/environments')).forEach((req) =>
      req.flush({ items: [], nextPageToken: null })
    );
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

describe('DeploymentDashboard environments loading', () => {
  // Separate from the outer describe's beforeEach, which auto-flushes /environments with a fixed
  // response — these tests need to control that response themselves.
  let fixture: ReturnType<typeof TestBed.createComponent<DeploymentDashboard>>;
  let component: any;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DeploymentDashboard],
      providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting(), provideRouter([]), provideNoopAnimations()]
    });
    fixture = TestBed.createComponent(DeploymentDashboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.match(() => true).forEach((req) => req.flush({ items: [], nextPageToken: null }));
    httpMock.verify();
  });

  it('defaults selectedEnvironmentId to the first loaded environment', () => {
    expect(component.environmentsLoading()).toBe(true);

    httpMock
      .expectOne((r) => r.url.includes('/environments'))
      .flush([
        { id: 'uat', name: 'UAT', requiresExtraConfirmation: false },
        { id: 'production', name: 'Production', requiresExtraConfirmation: true }
      ]);

    expect(component.environmentsLoading()).toBe(false);
    expect(component.environments()).toHaveLength(2);
    expect(component.selectedEnvironmentId()).toBe('uat');
  });

  it('shows an error and stops loading when the environments request fails', () => {
    httpMock
      .expectOne((r) => r.url.includes('/environments'))
      .flush({ message: 'Server exploded' }, { status: 500, statusText: 'Server Error' });

    expect(component.environmentsLoading()).toBe(false);
    expect(component.environmentsError()).toBe('Server exploded');
    expect(component.environments()).toEqual([]);
  });

  it('retryLoadEnvironments re-fetches and clears the error on success', () => {
    httpMock
      .expectOne((r) => r.url.includes('/environments'))
      .flush({ message: 'Server exploded' }, { status: 500, statusText: 'Server Error' });
    expect(component.environmentsError()).toBe('Server exploded');

    component.retryLoadEnvironments();

    httpMock.expectOne((r) => r.url.includes('/environments')).flush([{ id: 'uat', name: 'UAT', requiresExtraConfirmation: false }]);

    expect(component.environmentsError()).toBeNull();
    expect(component.environments()).toHaveLength(1);
  });
});
