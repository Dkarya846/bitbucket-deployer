import { Routes } from '@angular/router';

export const DEPLOYMENT_ROUTES: Routes = [
  {
    path: 'deploy',
    loadComponent: () => import('./pages/deployment-dashboard/deployment-dashboard').then((m) => m.DeploymentDashboard)
  },
  {
    path: 'deployments',
    loadComponent: () => import('./pages/deployment-history/deployment-history').then((m) => m.DeploymentHistory)
  },
  {
    path: 'deployments/:deploymentId',
    loadComponent: () => import('./pages/deployment-details/deployment-details').then((m) => m.DeploymentDetails)
  }
];
