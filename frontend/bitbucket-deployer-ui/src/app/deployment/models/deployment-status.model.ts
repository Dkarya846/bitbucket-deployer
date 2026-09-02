/** Mirrors the backend's DeploymentStatus enum exactly — see BitbucketDeployer.Api Deployment/Models/DeploymentStatus.cs. */
export type DeploymentStatus =
  | 'Pending'
  | 'Validating'
  | 'Queued'
  | 'Running'
  | 'Successful'
  | 'Failed'
  | 'Stopped'
  | 'Cancelled';

export const TERMINAL_DEPLOYMENT_STATUSES: ReadonlySet<DeploymentStatus> = new Set([
  'Successful',
  'Failed',
  'Stopped',
  'Cancelled'
]);

export function isTerminalStatus(status: DeploymentStatus): boolean {
  return TERMINAL_DEPLOYMENT_STATUSES.has(status);
}
