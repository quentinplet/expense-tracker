import { Routes } from '@angular/router';
import { Home } from '../features/home/home';
import { Auth } from '../features/account/auth/auth';

export const routes: Routes = [
  {
    path: '',
    component: Home,
  },
  {
    path: 'login',
    component: Auth,
  },
  {
    path: 'register',
    component: Auth,
  },
  {
    path: '**',
    redirectTo: '',
  },
];
