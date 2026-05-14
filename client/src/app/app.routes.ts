import { Routes } from '@angular/router';
import { Home } from '../features/home/home';
import { Auth } from '../features/account/auth/auth';
import { Dashboard } from '@/features/dashboard/dashboard';
import { Transactions } from '@/features/transactions/transactions';
import { AuthLayout } from '@/layout/auth-layout/auth-layout';
import { MainLayout } from '@/layout/main-layout/main-layout';
import { authGuard } from '@/core/guards/auth-guard';
import { NotFound } from '@/shared/errors/not-found/not-found';
import { TestErrors } from '@/features/test-errors/test-errors';
import { ServerError } from '@/shared/errors/server-error/server-error';

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
      {
        path: 'test-errors',
        component: TestErrors,
      },
    ],
  },
  // Pages d'erreur au niveau racine — sans layout
  { path: 'server-error', component: ServerError },
  { path: 'not-found', component: NotFound },
  { path: '**', redirectTo: 'not-found' },
];
