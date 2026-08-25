import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Language, LanguageService, SUPPORTED_LANGUAGES } from '@/core/services/language-service';

@Component({
  selector: 'app-language-switch',
  imports: [TranslatePipe],
  templateUrl: './language-switch.html',
  styleUrl: './language-switch.scss',
})
export class LanguageSwitch {
  protected languageService = inject(LanguageService);
  protected readonly languages = SUPPORTED_LANGUAGES;

  select(lang: Language) {
    if (this.languageService.current() === lang) return;
    this.languageService.use(lang);
  }
}