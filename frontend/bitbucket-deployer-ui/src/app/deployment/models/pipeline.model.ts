import { DeploymentStatus } from './deployment-status.model';

export interface PipelineVariable {
  name: string;
  default?: string;
  allowedValues?: string[];
}

export interface Pipeline {
  pattern: string;
  displayName: string;
  variables: PipelineVariable[];
  lastRunStatus?: DeploymentStatus;
  lastRunAt?: string;
}
