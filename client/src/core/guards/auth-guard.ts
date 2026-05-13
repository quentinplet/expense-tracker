import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account-service';
import { MessageService } from 'primeng/api';

export const authGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const toast = inject(MessageService);
  const router = inject(Router);

  if (accountService.currentUser()) return true;
  else {
    toast.add({
      severity: 'error',
      summary: 'Access Denied',
      detail: 'Please log in to access this page.',
    });
    router.navigateByUrl('/login');
    return false;
  }
};
