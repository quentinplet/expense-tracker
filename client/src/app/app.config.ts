import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { ConfirmationService, MessageService } from 'primeng/api';
import { InitService } from '@/core/services/init-service';
import { lastValueFrom } from 'rxjs/internal/lastValueFrom';
import { jwtInterceptor } from '@/core/interceptors/jwt-interceptor';
import { loadingInterceptor } from '@/core/interceptors/loading-interceptor';
import { errorInterceptor } from '@/core/interceptors/error-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([jwtInterceptor, loadingInterceptor, errorInterceptor])),
    provideAppInitializer(async () => {
      const initService = inject(InitService);
      return new Promise<void>((resolve) => {
        setTimeout(async () => {
          try {
            await lastValueFrom(initService.init());
          } finally {
            const splash = document.getElementById('initial-splash');
            if (splash) {
              splash.remove();
            }
            resolve();
          }
        }, 50);
      });
    }),
    providePrimeNG({
      theme: {
        preset: Aura,
      },
    }),
    MessageService,
    ConfirmationService,
  ],
};
