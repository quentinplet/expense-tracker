import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import localeEn from '@angular/common/locales/en';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { ExpenseTrackerPreset } from '@/core/theme/preset';
import { ConfirmationService, MessageService } from 'primeng/api';
import { InitService } from '@/core/services/init-service';
import { lastValueFrom } from 'rxjs/internal/lastValueFrom';
import { jwtInterceptor } from '@/core/interceptors/jwt-interceptor';
import { loadingInterceptor } from '@/core/interceptors/loading-interceptor';
import { errorInterceptor } from '@/core/interceptors/error-interceptor';
import { languageInterceptor } from '@/core/interceptors/language-interceptor';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LanguageService } from '@/core/services/language-service';
import { DARK_CLASS, ThemeService } from '@/core/services/theme-service';

// Angular n'embarque que en-US : sans ces enregistrements, DatePipe et CurrencyPipe
// resteraient en anglais quelle que soit la langue choisie.
registerLocaleData(localeFr);
registerLocaleData(localeEn);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withViewTransitions()),
    provideHttpClient(
      withInterceptors([
        jwtInterceptor,
        languageInterceptor,
        loadingInterceptor,
        errorInterceptor,
      ]),
    ),
    provideTranslateService({
      // Une clé absente doit rester visible en développement (§10, règle 3) : ngx-translate
      // affiche la clé brute par défaut, on ne pose donc pas de MissingTranslationHandler.
      fallbackLang: 'en',
      loader: provideTranslateHttpLoader({
        prefix: '/assets/i18n/',
        suffix: '.json',
      }),
    }),
    provideAppInitializer(() => {
      const languageService = inject(LanguageService);
      languageService.use(languageService.resolveInitial());
      inject(ThemeService).init();
    }),
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
        preset: ExpenseTrackerPreset,
        options: {
          // Sans ça PrimeNG suivrait la préférence système pendant que Tailwind
          // suivrait la classe `app-dark` : les deux se contrediraient.
          darkModeSelector: `.${DARK_CLASS}`,
        },
      },
    }),
    MessageService,
    ConfirmationService,
  ],
};
