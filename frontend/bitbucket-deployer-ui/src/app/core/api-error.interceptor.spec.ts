import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { apiErrorInterceptor } from './api-error.interceptor';
import { ApiError } from './api-error.model';

describe('apiErrorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('maps a structured backend error body into an ApiError', async () => {
    const pending = firstValueFrom(http.get('/api/thing')).catch((err: ApiError) => err);

    httpMock.expectOne('/api/thing').flush(
      { message: 'Repository does not exist.', errorCode: 'BITBUCKET_NOT_FOUND', traceId: 'abc' },
      { status: 404, statusText: 'Not Found' }
    );

    const err = (await pending) as ApiError;
    expect(err.status).toBe(404);
    expect(err.message).toBe('Repository does not exist.');
    expect(err.errorCode).toBe('BITBUCKET_NOT_FOUND');
  });

  it('falls back to a generic message when the error body is unstructured', async () => {
    const pending = firstValueFrom(http.get('/api/thing')).catch((err: ApiError) => err);

    httpMock.expectOne('/api/thing').flush('<html>oops</html>', { status: 500, statusText: 'Server Error' });

    const err = (await pending) as ApiError;
    expect(err.status).toBe(500);
    expect(err.message).toBe('An unexpected error occurred.');
  });

  it('maps a network failure (status 0) to a connectivity message', async () => {
    const pending = firstValueFrom(http.get('/api/thing')).catch((err: ApiError) => err);

    httpMock.expectOne('/api/thing').error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    const err = (await pending) as ApiError;
    expect(err.status).toBe(0);
    expect(err.message).toContain('Could not reach the server');
  });
});
