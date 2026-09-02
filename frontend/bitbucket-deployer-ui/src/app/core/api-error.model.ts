/** Normalized shape of every backend error response (mirrors ApiErrorResponse in the .NET API). */
export interface ApiError {
  status: number;
  message: string;
  errorCode?: string;
  traceId?: string;
}
