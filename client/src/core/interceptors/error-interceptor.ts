import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(MessageService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error) {
        switch (error.status) {
          case 400:
            if (error.error.errors) {
              const modelStateErrors = [];
              for (const key in error.error.errors) {
                if (error.error.errors[key]) {
                  modelStateErrors.push(error.error.errors[key]);
                }
              }
              throw modelStateErrors.flat();
            } else {
              toast.add({
                severity: 'error',
                summary: 'Bad Request',
                detail: error.error,
              });
            }
            break;
          case 401:
            toast.add({
              severity: 'error',
              summary: 'Unauthorized',
            });
            break;
          case 404:
            router.navigate(['/not-found']);
            break;
          case 500:
            const navigationExtras: NavigationExtras = { state: { error: error.error } };
            router.navigate(['/server-error'], navigationExtras);
            break;
          default:
            toast.add({
              severity: 'error',
              summary: 'Error',
              detail: 'An unexpected error occurred',
            });
        }
      }
      throw error;
    }),
  );
};
