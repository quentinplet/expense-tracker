import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Language, LanguageService, SUPPORTED_LANGUAGES } from '@/core/services/language-service';

@Component({
  selector: 'app-language-switch',
  imports: [TranslatePipe],
  template: `
    <div
      class="flex items-center gap-0.5 rounded-full border border-surface-700 bg-surface-800 p-0.5"
      role="group"
      [attr.aria-label]="'topbar.language' | translate"
    >
      @for (lang of languages; track lang) {
        <button
          type="button"
          class="cursor-pointer rounded-full px-2.5 py-1 text-xs font-semibold uppercase transition-colors"
          [class]="
            languageService.current() === lang
              ? 'bg-primary/15 text-primary'
              : 'text-surface-400 hover:text-surface-200'
          "
          [attr.aria-pressed]="languageService.current() === lang"
          (click)="select(lang)"
        >
          {{ lang }}
        </button>
      }
    </div>
  `,
})
export class LanguageSwitch {
  protected languageService = inject(LanguageService);
  protected readonly languages = SUPPORTED_LANGUAGES;

  select(lang: Language) {
    if (this.languageService.current() === lang) return;
    this.languageService.use(lang);
  }
}