import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from './api-error.model';

/**
 * Normalizes every failed HTTP call into an ApiError so components never need to know whether
 * the failure came from the backend's structured error envelope, a network drop, or a timeout.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        return throwError(() => toApiError(error));
      }
      return throwError(() => error);
    })
  );

function toApiError(error: HttpErrorResponse): ApiError {
  const body = error.error as { message?: string; errorCode?: string; traceId?: string } | null;

  if (error.status === 0) {
    return { status: 0, message: 'Could not reach the server. Check your network connection.' };
  }

  return {
    status: error.status,
    message: body?.message ?? 'An unexpected error occurred.',
    errorCode: body?.errorCode,
    traceId: body?.traceId
  };
}
