import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { Transaction } from '@/types/transaction';

@Component({
  selector: 'app-recent-transactions',
  imports: [CurrencyPipe, DatePipe, RouterLink, TranslatePipe, CategoryNamePipe],
  templateUrl: './recent-transactions.html',
  styleUrl: './recent-transactions.scss',
})
export class RecentTransactions {
  private languageService = inject(LanguageService);

  transactions = input.required<Transaction[]>();

  protected locale = computed(() => this.languageService.current());

  /** Adapte la transaction à la forme attendue par le pipe, qui centralise la
   *  règle « clé de traduction si catégorie système, nom sinon ». */
  protected categoryOf(transaction: Transaction) {
    return {
      name: transaction.categoryName,
      translationKey: transaction.categoryTranslationKey,
    };
  }
}
