import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { MonthTotals } from '@/types/dashboard';
import { MonthKey, monthKeyToDate, shiftMonth } from '../../month';

const INCOME_ACCENT = '#22c55e';
const EXPENSE_ACCENT = '#ef4444';
/** Suit le preset : le net n'a pas de sens propre, il porte la couleur de marque. */
const NET_ACCENT = 'var(--p-primary-color)';
const CUMULATIVE_ACCENT = '#8b5cf6';

export type Variation =
  /** Aucune base de comparaison : période précédente absente ou à zéro. */
  | { kind: 'none' }
  | { kind: 'stable' }
  /**
   * `up` décrit le mouvement, `good` son interprétation : des dépenses en hausse
   * montent et se lisent en rouge. Les deux ne se confondent pas.
   */
  | { kind: 'percent'; value: number; up: boolean; good: boolean };

export type Kpi = {
  labelKey: string;
  hintKey?: string;
  value: number;
  variation: Variation;
  tone: 'income' | 'expense' | 'net' | 'cumulative';
  /** PrimeIcons, affiché dans une pastille teintée de `accent`. */
  icon: string;
  accent: string;
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
  cumulativeNet = input.required<number>();
  month = input.required<MonthKey>();

  protected locale = computed(() => this.languageService.current());

  protected previousDate = computed(() => monthKeyToDate(shiftMonth(this.month(), -1)));

  protected kpis = computed<Kpi[]>(() => {
    const totals = this.totals();

    return [
      {
        // En tête, à la place du « solde total » des maquettes — mais assumé comme
        // un cumul de flux, jamais présenté comme un solde : l'application ignore
        // ce que l'utilisateur possède réellement.
        labelKey: 'dashboard.cumulative.label',
        hintKey: 'dashboard.cumulative.hint',
        value: this.cumulativeNet(),
        variation: { kind: 'none' },
        tone: 'cumulative',
        icon: 'pi-wallet',
        accent: CUMULATIVE_ACCENT,
      },
      {
        labelKey: 'dashboard.totals.income',
        value: totals.income,
        variation: compare(totals.income, totals.previousIncome, true),
        tone: 'income',
        icon: 'pi-arrow-down-left',
        accent: INCOME_ACCENT,
      },
      {
        // Seul KPI où la hausse est une mauvaise nouvelle.
        labelKey: 'dashboard.totals.expenses',
        value: totals.expenses,
        variation: compare(totals.expenses, totals.previousExpenses, false),
        tone: 'expense',
        icon: 'pi-arrow-up-right',
        accent: EXPENSE_ACCENT,
      },
      {
        labelKey: 'dashboard.totals.net',
        value: totals.net,
        variation: compare(totals.net, totals.previousNet, true),
        tone: 'net',
        icon: 'pi-chart-line',
        accent: NET_ACCENT,
      },
    ];
  });
}

/**
 * Un mois précédent à 0 € et un mois courant à 2 400 €, ce n'est pas « +∞ % » :
 * sans base de comparaison, on n'affiche pas de pourcentage.
 *
 * `higherIsBetter` porte la sémantique du KPI : à mouvement identique, des revenus
 * en hausse sont une bonne nouvelle et des dépenses en hausse une mauvaise.
 */
function compare(current: number, previous: number, higherIsBetter: boolean): Variation {
  if (previous === 0) return { kind: 'none' };
  if (current === previous) return { kind: 'stable' };

  const ratio = ((current - previous) / Math.abs(previous)) * 100;
  const up = ratio > 0;
  return { kind: 'percent', value: Math.abs(ratio), up, good: up === higherIsBetter };
}
