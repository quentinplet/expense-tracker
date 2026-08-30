import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { BudgetService } from '@/core/services/budget-service';
import { CategorieService } from '@/core/services/categorie-service';
import { Budget, CreateBudgetDto } from '@/types/budget';
import { Categorie } from '@/types/categorie';
import { PeriodSelector } from '@/features/dashboard/components/period-selector/period-selector';
import { currentMonthKey, isValidMonthKey, MonthKey } from '@/features/dashboard/month';
import { BudgetCard } from './components/budget-card/budget-card';
import { BudgetFormDialog, BudgetFormValue } from './budget-form-dialog/budget-form-dialog';

@Component({
  selector: 'app-budgets',
  imports: [Button, TranslatePipe, PeriodSelector, BudgetCard, BudgetFormDialog],
  templateUrl: './budgets.html',
  styleUrl: './budgets.scss',
})
export class Budgets {
  private budgetService = inject(BudgetService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected loading = signal(false);
  protected failed = signal(false);
  protected errors = signal<Record<string, string[]>>({});

  protected budgetDialog = false;
  protected selectedBudget = signal<Budget | null>(null);

  /** Présélectionne « Budget global » à l'ouverture — déclenché par le `+` de la
   *  carte placeholder plutôt que par le bouton d'en-tête (§ editGlobalBudget). */
  protected presetGlobalOnCreate = signal(false);

  protected currentMonth = signal<MonthKey>(currentMonthKey());
  protected budgets = signal<Budget[]>([]);
  protected expenseCategories = signal<Categorie[]>([]);

  /** CategoryId (ou `null` pour le global) déjà budgétés ce mois-ci — exclus du
   *  select de création (§ Key Gotchas frontend). */
  protected takenSelections = signal<(string | null)[]>([]);

  /** Le budget global du mois, ou `null` s'il n'a pas encore été créé. */
  protected globalBudget = computed(() => this.budgets().find((b) => b.categoryId === null) ?? null);

  /** Toujours affiché : une ligne placeholder à 0 € tant qu'aucun budget global
   *  n'existe pour ce mois (demande du 30/08 — la carte reste visible et cliquable
   *  plutôt que de disparaître). `id: ''` marque le placeholder pour BudgetCard. */
  protected globalBudgetDisplay = computed<Budget>(
    () =>
      this.globalBudget() ?? {
        id: '',
        month: this.currentMonth(),
        categoryId: null,
        categoryName: null,
        categoryTranslationKey: null,
        categoryIcon: null,
        categoryColor: null,
        amountLimit: 0,
        autoRenew: false,
        autoRenewConflict: false,
        spent: 0,
        remaining: 0,
      },
  );

  protected categoryBudgets = computed(() => this.budgets().filter((b) => b.categoryId !== null));

  constructor() {
    /** Le mois vit dans l'URL, même pattern que le dashboard (§ dashboard.ts) —
     *  mais un rechargement après mutation (create/update/delete) doit reposer sur
     *  un appel direct : renaviguer vers la même query ne réémet rien de nouveau. */
    this.route.queryParamMap
      .pipe(
        map((params) => params.get('month')),
        map((month) => (isValidMonthKey(month) ? month : currentMonthKey())),
        takeUntilDestroyed(),
      )
      .subscribe((month) => {
        this.currentMonth.set(month);
        this.loadData(month);
      });
  }

  private loadData(month: MonthKey) {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      budgets: this.budgetService.getBudgets(month),
      categories: this.categorieService.getCategories('Expense'),
    }).subscribe({
      next: ({ budgets, categories }) => {
        this.budgets.set(budgets);
        this.expenseCategories.set(categories);
        this.takenSelections.set(budgets.map((b) => b.categoryId));
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  setMonth(month: MonthKey) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { month },
      queryParamsHandling: 'merge',
    });
  }

  openNew() {
    this.selectedBudget.set(null);
    this.presetGlobalOnCreate.set(false);
    this.errors.set({});
    this.budgetDialog = true;
  }

  editBudget(budget: Budget) {
    this.selectedBudget.set(budget);
    this.presetGlobalOnCreate.set(false);
    this.errors.set({});
    this.budgetDialog = true;
  }

  /** Depuis la carte globale : une vraie ligne s'édite normalement, le placeholder
   *  (`id` vide) ouvre une création avec « Budget global » déjà sélectionné. */
  editGlobalBudget(budget: Budget) {
    if (budget.id) {
      this.editBudget(budget);
      return;
    }
    this.selectedBudget.set(null);
    this.presetGlobalOnCreate.set(true);
    this.errors.set({});
    this.budgetDialog = true;
  }

  deleteBudget(budget: Budget) {
    this.confirmationService.confirm({
      message: this.translate.instant('budgets.list.deleteConfirm'),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.budgetService.deleteBudget(budget.id).subscribe({
          next: () => {
            this.loadData(this.currentMonth());
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('budgets.list.deleted'),
              life: 3000,
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('budgets.list.deleteFailed'),
            });
          },
        });
      },
    });
  }

  submit(value: BudgetFormValue) {
    const selected = this.selectedBudget();
    if (selected) {
      this.updateBudget(selected, value);
      return;
    }
    this.createBudget(value);
  }

  private createBudget(value: BudgetFormValue) {
    this.budgetService.createBudget(value).subscribe({
      next: () => {
        this.loadData(this.currentMonth());
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('budgets.form.created'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('budgets.form.createFailed'),
        });
      },
    });
  }

  private updateBudget(selected: Budget, value: BudgetFormValue) {
    const dto: CreateBudgetDto = { ...value, month: selected.month };

    this.budgetService.updateBudget(selected.id, dto).subscribe({
      next: () => {
        this.loadData(this.currentMonth());
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('budgets.form.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('budgets.form.updateFailed'),
        });
      },
    });
  }

  hideDialog() {
    this.budgetDialog = false;
    this.selectedBudget.set(null);
    this.presetGlobalOnCreate.set(false);
    this.errors.set({});
  }
}