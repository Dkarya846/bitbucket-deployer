import { DeploymentStatus } from './deployment-status.model';

export interface Pipeline {
  pattern: string;
  displayName: string;
  requiresVariables: boolean;
  lastRunStatus?: DeploymentStatus;
  lastRunAt?: string;
}
