import { Component, computed, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import {
  currentMonthKey,
  isAtOrAfterCurrentMonth,
  MonthKey,
  monthKeyToDate,
  shiftMonth,
  toMonthKey,
} from '../../month';

@Component({
  selector: 'app-period-selector',
  imports: [FormsModule, DatePicker, TranslatePipe],
  templateUrl: './period-selector.html',
  styleUrl: './period-selector.scss',
})
export class PeriodSelector {
  private languageService = inject(LanguageService);

  month = input.required<MonthKey>();

  /**
   * Les commandes se figent pendant une requête : sans ça, des clics rapides font
   * courir les réponses les unes après les autres et l'écran finit par afficher
   * un mois que l'utilisateur a déjà quitté.
   */
  busy = input(false);

  monthChange = output<MonthKey>();

  protected locale = computed(() => this.languageService.current());

  protected pickerDate = computed(() => monthKeyToDate(this.month()));

  protected maxDate = computed(() => monthKeyToDate(currentMonthKey()));

  protected atCurrentMonth = computed(() => isAtOrAfterCurrentMonth(this.month()));

  shift(offset: number) {
    this.monthChange.emit(shiftMonth(this.month(), offset));
  }

  onPickerChange(date: Date | null) {
    if (!date) return;
    this.monthChange.emit(toMonthKey(date));
  }
}
