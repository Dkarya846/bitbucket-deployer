import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./auth/login/login').then((m) => m.Login) },
  { path: '', pathMatch: 'full', redirectTo: 'deploy' },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () => import('./deployment/deployment.routes').then((m) => m.DEPLOYMENT_ROUTES)
  }
];
