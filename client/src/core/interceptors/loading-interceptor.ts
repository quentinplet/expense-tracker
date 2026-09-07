import { HttpContext, HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { BusyService } from '../services/busy-service';
import { inject } from '@angular/core/primitives/di';
import { finalize } from 'rxjs/internal/operators/finalize';
import { environment } from '@/environments/environment';
import { delay, identity } from 'rxjs';

/**
 * `busyRequestCount` est global : sans cette échappatoire, une requête de fond
 * (polling des notifications, refresh de token) fait clignoter le `[loading]`
 * de n'importe quel `p-table` de l'app, qui n'a pourtant aucune donnée à voir
 * avec cette requête. À poser via `context: withSkipLoading()` sur les
 * requêtes qui ne doivent jamais compter comme un chargement d'écran.
 */
export const SKIP_LOADING = new HttpContextToken(() => false);

export function withSkipLoading(): HttpContext {
  return new HttpContext().set(SKIP_LOADING, true);
}

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const busyService = inject(BusyService);
  if (req.context.get(SKIP_LOADING)) return next(req);

  busyService.busy();
  return next(req).pipe(
    environment.production ? identity : delay(5),
    finalize(() => busyService.idle()),
  );
};
