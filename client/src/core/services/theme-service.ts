import { Injectable, signal } from '@angular/core';

export type Theme = 'dark' | 'light';

const STORAGE_KEY = 'app.theme';

/**
 * Le mode sombre est la valeur par défaut (§17). La classe `app-dark` sur <html>
 * est le sélecteur commun aux deux systèmes de style : le `@custom-variant dark`
 * de Tailwind et le `darkModeSelector` de PrimeNG. Sans ce point d'ancrage
 * partagé, les composants PrimeNG suivraient la préférence système pendant que
 * les utilitaires Tailwind suivraient la classe — et les deux se contrediraient.
 */
export const DARK_CLASS = 'app-dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  readonly current = signal<Theme>('dark');

  init() {
    this.apply(this.resolveInitial());
  }

  toggle() {
    this.apply(this.current() === 'dark' ? 'light' : 'dark');
  }

  apply(theme: Theme) {
    this.current.set(theme);
    document.documentElement.classList.toggle(DARK_CLASS, theme === 'dark');

    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // Stockage indisponible : le thème vaut pour la session.
    }
  }

  private resolveInitial(): Theme {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'dark' || stored === 'light') return stored;
    } catch {
      // Ignoré : on retombe sur le défaut.
    }

    return 'dark';
  }
}
