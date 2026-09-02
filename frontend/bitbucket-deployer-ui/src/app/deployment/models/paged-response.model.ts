export interface PagedResponse<T> {
  items: T[];
  nextPageToken: string | null;
}
