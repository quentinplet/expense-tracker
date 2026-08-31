import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { RecurringTransactionService } from '@/core/services/recurring-transaction-service';
import { CategorieService } from '@/core/services/categorie-service';
import { LanguageService } from '@/core/services/language-service';
import { CreateRecurringTransactionDto, RecurringTransaction } from '@/types/recurring-transaction';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { RecurringFormDialog, RecurringFormValue } from './recurring-form-dialog/recurring-form-dialog';

@Component({
  selector: 'app-recurring',
  imports: [
    FormsModule,
    ButtonModule,
    ToggleSwitchModule,
    TranslatePipe,
    CurrencyPipe,
    DatePipe,
    CategoryBadge,
    CategoryNamePipe,
    RecurringFormDialog,
  ],
  templateUrl: './recurring.html',
  styleUrl: './recurring.scss',
})
export class Recurring implements OnInit {
  private recurringTransactionService = inject(RecurringTransactionService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);

  protected recurringTransactions = signal<RecurringTransaction[]>([]);
  protected categories = signal<Categorie[]>([]);
  protected errors = signal<Record<string, string[]>>({});

  protected recurringDialog = false;
  protected selectedRecurringTransaction = signal<RecurringTransaction | null>(null);

  /** Présélectionne le sens à l'ouverture d'une création — déclenché par le `+`
   *  d'une colonne plutôt que par le bouton d'en-tête. */
  protected presetType = signal<TransactionType | null>(null);

  protected locale = computed(() => this.languageService.current());

  /** Une colonne par sens (§ demande du 31/08 : « diviser en 2 colonnes »),
   *  même pattern que Categories' `sections` — chaque colonne garde son propre
   *  filtrage plutôt qu'une liste unique mélangeant dépenses et revenus. */
  protected columns = computed(() => [
    {
      type: 'Expense' as TransactionType,
      titleKey: 'transaction.filters.typeExpense',
      // Même icône que le KPI « Dépenses » du dashboard (kpi-cards.ts) — la même
      // paire icône/sens partout dans l'app, jamais réinventée par écran.
      icon: 'pi-arrow-up-right',
      iconClass: 'text-red-500',
      items: this.recurringTransactions().filter((r) => r.type === 'Expense'),
    },
    {
      type: 'Income' as TransactionType,
      titleKey: 'transaction.filters.typeIncome',
      icon: 'pi-arrow-down-left',
      iconClass: 'text-green-500',
      items: this.recurringTransactions().filter((r) => r.type === 'Income'),
    },
  ]);

  ngOnInit() {
    this.loadCategories();
    this.loadRecurringTransactions();
  }

  private loadRecurringTransactions() {
    // Déjà triées par NextDueDate croissant côté serveur (§ requirement).
    this.recurringTransactionService.getRecurringTransactions().subscribe({
      next: (recurringTransactions) => this.recurringTransactions.set(recurringTransactions),
    });
  }

  private loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }

  openNew(type: TransactionType | null = null) {
    this.selectedRecurringTransaction.set(null);
    this.presetType.set(type);
    this.errors.set({});
    this.recurringDialog = true;
  }

  editRecurringTransaction(recurringTransaction: RecurringTransaction) {
    this.selectedRecurringTransaction.set(recurringTransaction);
    this.presetType.set(null);
    this.errors.set({});
    this.recurringDialog = true;
  }

  /**
   * Bascule directement depuis la liste, sans repasser par le dialog complet
   * (§ Key Gotchas frontend : « mettre en pause un abonnement annulé doit être
   * un clic »). Même RecurringTransactionRequestDto que le dialog, seul `active`
   * change — pas d'endpoint dédié, contrairement au toggle Category.Enabled.
   */
  toggleActive(recurringTransaction: RecurringTransaction) {
    const dto: CreateRecurringTransactionDto = {
      label: recurringTransaction.label,
      amount: recurringTransaction.amount,
      type: recurringTransaction.type,
      frequency: recurringTransaction.frequency,
      nextDueDate: recurringTransaction.nextDueDate,
      active: !recurringTransaction.active,
      categoryId: recurringTransaction.categoryId,
    };

    this.recurringTransactionService.updateRecurringTransaction(recurringTransaction.id, dto).subscribe({
      next: (updated) => {
        this.recurringTransactions.update((items) =>
          items.map((item) => (item.id === updated.id ? updated : item)),
        );
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('recurring.list.toggleFailed'),
        });
      },
    });
  }

  deleteRecurringTransaction(recurringTransaction: RecurringTransaction) {
    this.confirmationService.confirm({
      message: this.translate.instant('recurring.list.deleteConfirm', {
        label: recurringTransaction.label,
      }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.recurringTransactionService.deleteRecurringTransaction(recurringTransaction.id).subscribe({
          next: () => {
            this.recurringTransactions.update((items) =>
              items.filter((item) => item.id !== recurringTransaction.id),
            );
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('recurring.list.deleted'),
              life: 3000,
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('recurring.list.deleteFailed'),
            });
          },
        });
      },
    });
  }

  submit(value: RecurringFormValue) {
    const selected = this.selectedRecurringTransaction();
    if (selected) {
      this.updateRecurringTransaction(selected, value);
      return;
    }
    this.createRecurringTransaction(value);
  }

  private createRecurringTransaction(value: RecurringFormValue) {
    const dto: CreateRecurringTransactionDto = {
      ...value,
      nextDueDate: this.toIsoDate(value.nextDueDate),
    };

    this.recurringTransactionService.createRecurringTransaction(dto).subscribe({
      next: (created) => {
        this.recurringTransactions.update((items) => this.sortByNextDueDate([...items, created]));
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('recurring.form.created'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('recurring.form.createFailed'),
        });
      },
    });
  }

  private updateRecurringTransaction(selected: RecurringTransaction, value: RecurringFormValue) {
    const dto: CreateRecurringTransactionDto = {
      ...value,
      nextDueDate: this.toIsoDate(value.nextDueDate),
    };

    this.recurringTransactionService.updateRecurringTransaction(selected.id, dto).subscribe({
      next: (updated) => {
        this.recurringTransactions.update((items) =>
          this.sortByNextDueDate(items.map((item) => (item.id === updated.id ? updated : item))),
        );
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('recurring.form.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('recurring.form.updateFailed'),
        });
      },
    });
  }

  hideDialog() {
    this.recurringDialog = false;
    this.selectedRecurringTransaction.set(null);
    this.presetType.set(null);
    this.errors.set({});
  }

  private sortByNextDueDate(items: RecurringTransaction[]): RecurringTransaction[] {
    return [...items].sort((a, b) => a.nextDueDate.localeCompare(b.nextDueDate));
  }

  private toIsoDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
