import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LanguageService } from '../services/language-service';

/**
 * Ajoute Accept-Language à chaque requête (§10), pour que les messages d'erreur
 * du backend reviennent dans la langue de l'utilisateur.
 *
 * Les fichiers de traduction eux-mêmes sont servis par le dev server, pas par l'API :
 * les exclure évite un en-tête inutile sur des ressources statiques.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/assets/i18n/')) return next(req);

  const languageService = inject(LanguageService);

  return next(
    req.clone({
      setHeaders: { 'Accept-Language': languageService.current() },
    }),
  );
};
