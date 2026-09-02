import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { DeploymentService } from './deployment.service';
import { CreateDeploymentRequest } from '../models/deployment.model';
import { environment } from '../../../environments/environment';

describe('DeploymentService', () => {
  let service: DeploymentService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/deployments`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(DeploymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const request: CreateDeploymentRequest = {
    environment: 'uat',
    repositories: [{ repository: 'ss-web', branch: 'uat', pipeline: 'deploy-uat' }]
  };

  it('validate POSTs to /validate with the request body', () => {
    service.validate(request).subscribe();
    const req = httpMock.expectOne(`${baseUrl}/validate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ isValid: true, items: [] });
  });

  it('deploy POSTs to the base deployments URL', () => {
    service.deploy(request).subscribe();
    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'DEP-1', environment: 'uat', createdBy: 'alice', createdAt: '', status: 'Running', items: [] });
  });

  it('getDeployment GETs /{id}', () => {
    service.getDeployment('DEP-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/DEP-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'DEP-1', environment: 'uat', createdBy: 'alice', createdAt: '', status: 'Running', items: [] });
  });

  it('getHistory GETs the base deployments URL', () => {
    service.getHistory().subscribe();
    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('cancel POSTs to /{id}/cancel', () => {
    service.cancel('DEP-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/DEP-1/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'DEP-1', environment: 'uat', createdBy: 'alice', createdAt: '', status: 'Cancelled', items: [] });
  });

  it('retry POSTs to /{id}/retry', () => {
    service.retry('DEP-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/DEP-1/retry`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'DEP-1', environment: 'uat', createdBy: 'alice', createdAt: '', status: 'Running', items: [] });
  });
});
