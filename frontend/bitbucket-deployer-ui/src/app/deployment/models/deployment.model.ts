import { DeploymentStatus } from './deployment-status.model';

export interface DeploymentItemRequest {
  repository: string;
  branch: string;
  pipeline: string;
  variables?: Record<string, string>;
}

export interface CreateDeploymentRequest {
  environment: string;
  repositories: DeploymentItemRequest[];
}

export interface ValidationItemResult {
  repository: string;
  branch: string;
  pipeline: string;
  isValid: boolean;
  errorMessage?: string;
}

export interface ValidationResult {
  isValid: boolean;
  items: ValidationItemResult[];
}

export interface DeploymentItem {
  repository: string;
  branch: string;
  pipeline: string;
  bitbucketPipelineUuid?: string;
  status: DeploymentStatus;
  errorMessage?: string;
}

export interface Deployment {
  id: string;
  environment: string;
  createdBy: string;
  createdAt: string;
  status: DeploymentStatus;
  items: DeploymentItem[];
}

/** Row shape for the /deployments history table — a subset of Deployment. */
export interface DeploymentListItem {
  id: string;
  environment: string;
  createdBy: string;
  createdAt: string;
  status: DeploymentStatus;
  repositoryCount: number;
}
