import { Routes } from '@angular/router';
import { Home } from '../features/home/home';
import { Auth } from '../features/account/auth/auth';
import { Dashboard } from '@/features/dashboard/dashboard';
import { Transactions } from '@/features/transactions/transactions';
import { AuthLayout } from '@/layouts/auth-layout/auth-layout';
import { MainLayout } from '@/layouts/main-layout/main-layout';
import { authGuard } from '@/core/guards/auth-guard';

export const routes: Routes = [
  {
    path: '',
    component: AuthLayout,
    children: [
      { path: 'login', component: Auth },
      // { path: 'register', component: RegisterComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' },
    ],
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    runGuardsAndResolvers: 'always',
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'transactions', component: Transactions },
      // { path: 'categories', component: Categories },
      // { path: 'budgets', component: Budgets },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
