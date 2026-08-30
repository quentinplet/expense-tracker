import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { RecurringExpenseService } from '@/core/services/recurring-expense-service';
import { CategorieService } from '@/core/services/categorie-service';
import { LanguageService } from '@/core/services/language-service';
import { CreateRecurringExpenseDto, RecurringExpense } from '@/types/recurring-expense';
import { Categorie } from '@/types/categorie';
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
  private recurringExpenseService = inject(RecurringExpenseService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);

  protected expenses = signal<RecurringExpense[]>([]);
  protected categories = signal<Categorie[]>([]);
  protected errors = signal<Record<string, string[]>>({});

  protected recurringDialog = false;
  protected selectedExpense = signal<RecurringExpense | null>(null);

  protected locale = computed(() => this.languageService.current());

  ngOnInit() {
    this.loadCategories();
    this.loadExpenses();
  }

  private loadExpenses() {
    // Déjà triées par NextDueDate croissant côté serveur (§ requirement).
    this.recurringExpenseService.getRecurringExpenses().subscribe({
      next: (expenses) => this.expenses.set(expenses),
    });
  }

  private loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }

  openNew() {
    this.selectedExpense.set(null);
    this.errors.set({});
    this.recurringDialog = true;
  }

  editExpense(expense: RecurringExpense) {
    this.selectedExpense.set(expense);
    this.errors.set({});
    this.recurringDialog = true;
  }

  /**
   * Bascule directement depuis la liste, sans repasser par le dialog complet
   * (§ Key Gotchas frontend : « mettre en pause un abonnement annulé doit être
   * un clic »). Même RecurringExpenseRequestDto que le dialog, seul `active`
   * change — pas d'endpoint dédié, contrairement au toggle Category.Enabled.
   */
  toggleActive(expense: RecurringExpense) {
    const dto: CreateRecurringExpenseDto = {
      label: expense.label,
      amount: expense.amount,
      type: expense.type,
      frequency: expense.frequency,
      nextDueDate: expense.nextDueDate,
      active: !expense.active,
      categoryId: expense.categoryId,
    };

    this.recurringExpenseService.updateRecurringExpense(expense.id, dto).subscribe({
      next: (updated) => {
        this.expenses.update((expenses) => expenses.map((e) => (e.id === updated.id ? updated : e)));
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

  deleteExpense(expense: RecurringExpense) {
    this.confirmationService.confirm({
      message: this.translate.instant('recurring.list.deleteConfirm', { label: expense.label }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.recurringExpenseService.deleteRecurringExpense(expense.id).subscribe({
          next: () => {
            this.expenses.update((expenses) => expenses.filter((e) => e.id !== expense.id));
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
    const selected = this.selectedExpense();
    if (selected) {
      this.updateExpense(selected, value);
      return;
    }
    this.createExpense(value);
  }

  private createExpense(value: RecurringFormValue) {
    const dto: CreateRecurringExpenseDto = { ...value, nextDueDate: this.toIsoDate(value.nextDueDate) };

    this.recurringExpenseService.createRecurringExpense(dto).subscribe({
      next: (created) => {
        this.expenses.update((expenses) => this.sortByNextDueDate([...expenses, created]));
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

  private updateExpense(selected: RecurringExpense, value: RecurringFormValue) {
    const dto: CreateRecurringExpenseDto = { ...value, nextDueDate: this.toIsoDate(value.nextDueDate) };

    this.recurringExpenseService.updateRecurringExpense(selected.id, dto).subscribe({
      next: (updated) => {
        this.expenses.update((expenses) =>
          this.sortByNextDueDate(expenses.map((e) => (e.id === updated.id ? updated : e))),
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
    this.selectedExpense.set(null);
    this.errors.set({});
  }

  private sortByNextDueDate(expenses: RecurringExpense[]): RecurringExpense[] {
    return [...expenses].sort((a, b) => a.nextDueDate.localeCompare(b.nextDueDate));
  }

  private toIsoDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
