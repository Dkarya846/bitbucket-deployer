import { EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Observable, firstValueFrom } from 'rxjs';
import { authGuard } from './auth.guard';
import { environment } from '../../environments/environment';

describe('authGuard', () => {
  let httpMock: HttpTestingController;
  let injector: EnvironmentInjector;
  const meUrl = `${environment.apiBaseUrl}/auth/me`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
    injector = TestBed.inject(EnvironmentInjector);
  });

  afterEach(() => httpMock.verify());

  function runGuard() {
    return runInInjectionContext(
      injector,
      () => authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot) as Observable<boolean | UrlTree>
    );
  }

  it('allows activation when the backend reports a logged-in session', async () => {
    // The guard's Observable is lazy — subscribing via firstValueFrom must happen before the
    // mock flush, otherwise the HTTP request is never actually made.
    const pending = firstValueFrom(runGuard());
    httpMock.expectOne(meUrl).flush({ isLoggedIn: true, displayName: 'Darshan Jain' });

    expect(await pending).toBe(true);
  });

  it('redirects to /login when not logged in', async () => {
    const pending = firstValueFrom(runGuard());
    httpMock.expectOne(meUrl).flush({ isLoggedIn: false });

    const resolved = await pending;
    expect(resolved).not.toBe(true);
    expect((resolved as UrlTree).toString()).toBe('/login');
  });

  it('redirects to /login when the /me request fails', async () => {
    const pending = firstValueFrom(runGuard());
    httpMock.expectOne(meUrl).flush('error', { status: 500, statusText: 'Server Error' });

    const resolved = await pending;
    expect(resolved).not.toBe(true);
    expect((resolved as UrlTree).toString()).toBe('/login');
  });
});
