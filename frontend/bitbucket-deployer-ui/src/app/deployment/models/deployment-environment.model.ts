/** A deployment target (UAT / Staging / Production). Not to be confused with Angular's src/environments build config. */
export interface DeploymentEnvironment {
  id: string;
  name: string;
  requiresExtraConfirmation: boolean;
}

/** Configurable per section 16 — in a later iteration this can be fetched from the backend instead of hardcoded. */
export const DEPLOYMENT_ENVIRONMENTS: DeploymentEnvironment[] = [
  { id: 'uat', name: 'UAT', requiresExtraConfirmation: false },
  { id: 'staging', name: 'Staging', requiresExtraConfirmation: false },
  { id: 'production', name: 'Production', requiresExtraConfirmation: true }
];
