import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Repository } from '../models/repository.model';
import { Branch } from '../models/branch.model';
import { Pipeline } from '../models/pipeline.model';
import { PagedResponse } from '../models/paged-response.model';
import { DeploymentEnvironment } from '../models/deployment-environment.model';

/**
 * All repository/branch/pipeline discovery calls. Deliberately thin — no caching or state here,
 * that lives in the components/signal stores that consume it, since different pages need
 * different lifetimes (e.g. the dashboard caches per-repository branch lists; history doesn't).
 */
@Injectable({ providedIn: 'root' })
export class RepositoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/deployments`;

  getEnvironments(): Observable<DeploymentEnvironment[]> {
    return this.http.get<DeploymentEnvironment[]>(`${this.baseUrl}/environments`);
  }

  getRepositories(search?: string, pageToken?: string): Observable<PagedResponse<Repository>> {
    return this.http.get<PagedResponse<Repository>>(`${this.baseUrl}/repositories`, {
      params: this.buildParams({ search, pageToken })
    });
  }

  getBranches(repository: string, search?: string, pageToken?: string): Observable<PagedResponse<Branch>> {
    return this.http.get<PagedResponse<Branch>>(`${this.baseUrl}/repositories/${encodeURIComponent(repository)}/branches`, {
      params: this.buildParams({ search, pageToken })
    });
  }

  getPipelines(repository: string, branch?: string): Observable<Pipeline[]> {
    return this.http.get<Pipeline[]>(`${this.baseUrl}/repositories/${encodeURIComponent(repository)}/pipelines`, {
      params: this.buildParams({ branch })
    });
  }

  private buildParams(values: Record<string, string | undefined>): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value) {
        params = params.set(key, value);
      }
    }
    return params;
  }
}
