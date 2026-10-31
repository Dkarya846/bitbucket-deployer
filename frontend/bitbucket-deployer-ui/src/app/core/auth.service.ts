import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser } from './current-user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  /** Null until refresh() resolves at least once — the toolbar treats null the same as logged-out. */
  readonly currentUser = signal<CurrentUser | null>(null);

  refresh(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.baseUrl}/me`).pipe(tap((user) => this.currentUser.set(user)));
  }

  /** Full browser navigation, not an XHR — the backend responds with a redirect to Bitbucket's consent screen. */
  login(): void {
    window.location.href = `${this.baseUrl}/login`;
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(tap(() => this.currentUser.set({ isLoggedIn: false })));
  }
}
