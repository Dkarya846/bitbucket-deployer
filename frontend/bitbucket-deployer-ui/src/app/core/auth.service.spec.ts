import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('currentUser is null before refresh() resolves', () => {
    expect(service.currentUser()).toBeNull();
  });

  it('refresh() GETs /auth/me and updates currentUser', () => {
    service.refresh().subscribe();

    const req = httpMock.expectOne(`${baseUrl}/me`);
    expect(req.request.method).toBe('GET');
    req.flush({ isLoggedIn: true, displayName: 'Darshan Jain' });

    expect(service.currentUser()).toEqual({ isLoggedIn: true, displayName: 'Darshan Jain' });
  });

  it('logout() POSTs /auth/logout and marks currentUser as logged out', () => {
    service.logout().subscribe();

    const req = httpMock.expectOne(`${baseUrl}/logout`);
    expect(req.request.method).toBe('POST');
    req.flush(null);

    expect(service.currentUser()).toEqual({ isLoggedIn: false });
  });
});
