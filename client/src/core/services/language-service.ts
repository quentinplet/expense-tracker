import { inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export const SUPPORTED_LANGUAGES = ['fr', 'en'] as const;
export type Language = (typeof SUPPORTED_LANGUAGES)[number];

const STORAGE_KEY = 'app.lang';

function isSupported(value: string | null | undefined): value is Language {
  return !!value && (SUPPORTED_LANGUAGES as readonly string[]).includes(value);
}

@Injectable({
  providedIn: 'root',
})
export class LanguageService {
  private translate = inject(TranslateService);

  /// Lue par l'intercepteur pour l'en-tête Accept-Language, et par les composants qui
  /// doivent se reconstruire au changement de langue — les options Chart.js, par exemple.
  readonly current = signal<Language>('en');

  /**
   * Chaîne de repli de §10 : localStorage → langue du navigateur si fr ou en → en.
   * `User.preferredLanguage` s'insérera entre les deux quand le champ existera côté serveur.
   */
  resolveInitial(): Language {
    const stored = this.readStored();
    if (isSupported(stored)) return stored;

    const browser = navigator.language?.split('-')[0];
    return isSupported(browser) ? browser : 'en';
  }

  use(lang: Language) {
    this.current.set(lang);
    this.translate.use(lang);
    document.documentElement.lang = lang;

    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // Navigation privée ou stockage bloqué : la langue vaut pour la session.
    }
  }

  private readStored(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
