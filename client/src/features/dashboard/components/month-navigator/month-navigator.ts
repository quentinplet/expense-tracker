import { Component, computed, inject, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { isAtOrAfterCurrentMonth, MonthKey, monthKeyToDate } from '../../month';

@Component({
  selector: 'app-month-navigator',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './month-navigator.html',
  styleUrl: './month-navigator.scss',
})
export class MonthNavigator {
  private languageService = inject(LanguageService);

  month = input.required<MonthKey>();

  /**
   * Les deux flèches se désactivent pendant une requête : sans ça, des clics
   * rapides font courir les réponses les unes après les autres et l'écran finit
   * par afficher un mois que l'utilisateur a déjà quitté.
   */
  busy = input(false);

  previous = output<void>();
  next = output<void>();

  protected locale = computed(() => this.languageService.current());
  protected date = computed(() => monthKeyToDate(this.month()));
  protected atCurrentMonth = computed(() => isAtOrAfterCurrentMonth(this.month()));
}