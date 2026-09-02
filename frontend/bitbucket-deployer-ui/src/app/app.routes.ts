import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'deploy' },
  {
    path: '',
    loadChildren: () => import('./deployment/deployment.routes').then((m) => m.DEPLOYMENT_ROUTES)
  }
];
