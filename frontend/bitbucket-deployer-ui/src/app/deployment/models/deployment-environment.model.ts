/** A deployment target (UAT / Staging / Production). Not to be confused with Angular's src/environments build config. */
export interface DeploymentEnvironment {
  id: string;
  name: string;
  requiresExtraConfirmation: boolean;
}
