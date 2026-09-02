import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateDeploymentRequest, Deployment, DeploymentListItem, ValidationResult } from '../models/deployment.model';

@Injectable({ providedIn: 'root' })
export class DeploymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/deployments`;

  validate(request: CreateDeploymentRequest): Observable<ValidationResult> {
    return this.http.post<ValidationResult>(`${this.baseUrl}/validate`, request);
  }

  deploy(request: CreateDeploymentRequest): Observable<Deployment> {
    return this.http.post<Deployment>(this.baseUrl, request);
  }

  getDeployment(deploymentId: string): Observable<Deployment> {
    return this.http.get<Deployment>(`${this.baseUrl}/${encodeURIComponent(deploymentId)}`);
  }

  getHistory(): Observable<DeploymentListItem[]> {
    return this.http.get<DeploymentListItem[]>(this.baseUrl);
  }

  cancel(deploymentId: string): Observable<Deployment> {
    return this.http.post<Deployment>(`${this.baseUrl}/${encodeURIComponent(deploymentId)}/cancel`, {});
  }

  retry(deploymentId: string): Observable<Deployment> {
    return this.http.post<Deployment>(`${this.baseUrl}/${encodeURIComponent(deploymentId)}/retry`, {});
  }
}
