import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { RepositoryService } from './repository.service';
import { environment } from '../../../environments/environment';

describe('RepositoryService', () => {
  let service: RepositoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(RepositoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getRepositories omits search/pageToken params when not provided', () => {
    service.getRepositories().subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/deployments/repositories`);
    expect(req.request.params.keys().length).toBe(0);
    req.flush({ items: [], nextPageToken: null });
  });

  it('getRepositories includes search and pageToken when provided', () => {
    service.getRepositories('ss-', 'cursor-1').subscribe();
    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/deployments/repositories` && r.params.get('search') === 'ss-' && r.params.get('pageToken') === 'cursor-1'
    );
    req.flush({ items: [], nextPageToken: null });
  });

  it('getBranches URL-encodes the repository slug', () => {
    service.getBranches('ss web/weird').subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/deployments/repositories/${encodeURIComponent('ss web/weird')}/branches`);
    req.flush({ items: [], nextPageToken: null });
  });

  it('getPipelines includes the branch as a query param', () => {
    service.getPipelines('ss-web', 'uat').subscribe();
    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/deployments/repositories/ss-web/pipelines` && r.params.get('branch') === 'uat'
    );
    req.flush([]);
  });

  it('getPipelines omits the branch param when not provided', () => {
    service.getPipelines('ss-web').subscribe();
    const req = httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/deployments/repositories/ss-web/pipelines`);
    expect(req.request.params.has('branch')).toBe(false);
    req.flush([]);
  });
});
