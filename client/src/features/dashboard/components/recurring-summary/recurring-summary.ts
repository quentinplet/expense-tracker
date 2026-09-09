import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { RecurringTransaction } from '@/types/recurring-transaction';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';

/** Un aperçu, pas la liste complète (confirmé : lien « voir tout » vers /recurring). */
const PREVIEW_COUNT = 5;

@Component({
  selector: 'app-recurring-summary',
  imports: [RouterLink, TranslatePipe, CategoryBadge, CategoryNamePipe, CurrencyPipe, DatePipe],
  templateUrl: './recurring-summary.html',
  styleUrl: './recurring-summary.scss',
})
export class RecurringSummary {
  private languageService = inject(LanguageService);

  recurringTransactions = input.required<RecurringTransaction[]>();

  protected locale = computed(() => this.languageService.current());

  /** Déjà triées par NextDueDate croissant côté serveur (§ RecurringTransactionService) —
   *  filtrer préserve cet ordre, pas besoin de re-trier. Dépenses uniquement : le titre
   *  « Charges récurrentes » ne désigne que des sorties d'argent — un revenu récurrent
   *  (salaire...) n'a pas sa place ici. */
  protected upcoming = computed(() =>
    this.recurringTransactions()
      .filter((r) => r.active && r.type === 'Expense')
      .slice(0, PREVIEW_COUNT),
  );

  protected categoryOf(item: RecurringTransaction) {
    return { name: item.categoryName, translationKey: item.categoryTranslationKey };
  }
}
