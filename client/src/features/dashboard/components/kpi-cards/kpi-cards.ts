import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { MonthTotals } from '@/types/dashboard';
import { MonthKey, monthKeyToDate, shiftMonth } from '../../month';

export type Variation =
  /** Le mois précédent vaut zéro : un pourcentage n'aurait aucun sens. */
  | { kind: 'none' }
  | { kind: 'stable' }
  | { kind: 'percent'; value: number; up: boolean };

export type Kpi = {
  labelKey: string;
  value: number;
  variation: Variation;
  tone: 'income' | 'expense' | 'net';
};

@Component({
  selector: 'app-kpi-cards',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, TranslatePipe],
  templateUrl: './kpi-cards.html',
  styleUrl: './kpi-cards.scss',
})
export class KpiCards {
  private languageService = inject(LanguageService);

  totals = input.required<MonthTotals>();
  month = input.required<MonthKey>();

  protected locale = computed(() => this.languageService.current());
  protected previousDate = computed(() => monthKeyToDate(shiftMonth(this.month(), -1)));

  protected kpis = computed<Kpi[]>(() => {
    const totals = this.totals();

    return [
      {
        labelKey: 'dashboard.totals.income',
        value: totals.income,
        variation: compare(totals.income, totals.previousIncome),
        tone: 'income',
      },
      {
        labelKey: 'dashboard.totals.expenses',
        value: totals.expenses,
        variation: compare(totals.expenses, totals.previousExpenses),
        tone: 'expense',
      },
      {
        labelKey: 'dashboard.totals.net',
        value: totals.net,
        variation: compare(totals.net, totals.previousNet),
        tone: 'net',
      },
    ];
  });
}

/**
 * Un mois précédent à 0 € et un mois courant à 2 400 €, ce n'est pas « +∞ % ».
 * Sans base de comparaison, on n'affiche aucun pourcentage.
 */
function compare(current: number, previous: number): Variation {
  if (previous === 0) return { kind: 'none' };
  if (current === previous) return { kind: 'stable' };

  const ratio = ((current - previous) / Math.abs(previous)) * 100;
  return { kind: 'percent', value: Math.abs(ratio), up: ratio > 0 };
}