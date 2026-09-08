import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { Budget } from '@/types/budget';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { MonthKey } from '../../month';

type BudgetStatus = 'normal' | 'warning' | 'over';

type BudgetRow = {
  budget: Budget;
  isGlobal: boolean;
  /** Plafonnée à 100 — même règle que BudgetCard (§ budget-card.ts). */
  percentage: number;
  status: BudgetStatus;
};

const GLOBAL_ACCENT = 'var(--p-primary-500)';

/** Nombre de lignes catégorie affichées, les plus urgentes d'abord — un aperçu, pas la
 *  liste complète (confirmé : lien « voir tout » vers /budgets pour le reste). */
const PREVIEW_COUNT = 4;

@Component({
  selector: 'app-budgets-summary',
  imports: [RouterLink, TranslatePipe, CategoryBadge, CategoryNamePipe, CurrencyPipe, NgTemplateOutlet],
  templateUrl: './budgets-summary.html',
  styleUrl: './budgets-summary.scss',
})
export class BudgetsSummary {
  private languageService = inject(LanguageService);

  budgets = input.required<Budget[]>();
  month = input.required<MonthKey>();

  protected locale = computed(() => this.languageService.current());

  protected globalRow = computed<BudgetRow | null>(() => {
    const global = this.budgets().find((b) => b.categoryId === null);
    return global ? toRow(global, true) : null;
  });

  /** Les plus proches (ou déjà au-delà) du plafond en premier : c'est l'information
   *  la plus utile dans un aperçu forcément tronqué. */
  protected categoryRows = computed<BudgetRow[]>(() =>
    this.budgets()
      .filter((b) => b.categoryId !== null)
      .map((b) => toRow(b, false))
      .sort((a, b) => b.percentage - a.percentage)
      .slice(0, PREVIEW_COUNT),
  );

  protected hasAny = computed(() => this.globalRow() !== null || this.categoryRows().length > 0);

  protected badgeColor(row: BudgetRow) {
    return row.isGlobal ? GLOBAL_ACCENT : (row.budget.categoryColor ?? 'var(--p-surface-400)');
  }

  /**
   * Même courbe vert → rouge que BudgetCard (§ budget-card.ts) : un mélange linéaire
   * faisait virer la barre au jaune/orange dès la moitié du plafond. Exposant 3 sur le
   * ratio (pas le pourcentage affiché) : le vert domine jusque tard, puis le rouge
   * monte en flèche à l'approche de la limite.
   */
  protected barColor(row: BudgetRow) {
    const redRatio = Math.pow(row.percentage / 100, 3) * 100;
    return `color-mix(in oklch, var(--p-red-500) ${redRatio}%, var(--p-green-500))`;
  }
}

function toRow(budget: Budget, isGlobal: boolean): BudgetRow {
  const percentage =
    budget.amountLimit <= 0
      ? budget.spent > 0
        ? 100
        : 0
      : Math.min(100, (budget.spent / budget.amountLimit) * 100);

  const status: BudgetStatus = percentage >= 100 ? 'over' : percentage >= 90 ? 'warning' : 'normal';

  return { budget, isGlobal, percentage, status };
}
