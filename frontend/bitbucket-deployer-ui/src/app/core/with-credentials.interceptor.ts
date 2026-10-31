import { HttpInterceptorFn } from '@angular/common/http';

/** Session auth is an HttpOnly cookie — every request to our own API must carry it. */
export const withCredentialsInterceptor: HttpInterceptorFn = (request, next) => next(request.clone({ withCredentials: true }));
