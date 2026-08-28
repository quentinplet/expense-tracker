import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie, CreateCategorieDto } from '@/types/categorie';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryFormDialog, CategoryFormValue } from './category-form-dialog/category-form-dialog';

@Component({
  selector: 'app-categories',
  imports: [ButtonModule, TooltipModule, TranslatePipe, CategoryNamePipe, CategoryBadge, CategoryFormDialog],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
  providers: [CategoryNamePipe],
})
export class Categories implements OnInit {
  categories = signal<Categorie[]>([]);
  errors = signal<Record<string, string[]>>({});

  categoryDialog = false;
  selectedCategory = signal<Categorie | null>(null);

  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);

  /** Deux sections par Type : c'est le seul écran où les deux « Other »
   *  apparaissent ensemble sans filtre, grouper évite d'avoir à lire chaque
   *  icône une par une pour les distinguer. */
  protected sections = computed(() => [
    {
      titleKey: 'transaction.filters.typeExpense',
      categories: this.categories().filter((c) => c.type === 'Expense'),
    },
    {
      titleKey: 'transaction.filters.typeIncome',
      categories: this.categories().filter((c) => c.type === 'Income'),
    },
  ]);

  ngOnInit() {
    this.loadCategories();
  }

  private loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }

  openNew() {
    this.selectedCategory.set(null);
    this.errors.set({});
    this.categoryDialog = true;
  }

  editCategory(category: Categorie) {
    this.selectedCategory.set(category);
    this.errors.set({});
    this.categoryDialog = true;
  }

  deleteCategory(category: Categorie) {
    if (category.isLocked) return;

    this.confirmationService.confirm({
      message: this.translate.instant('category.list.deleteOne', { name: category.name }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.categorieService.deleteCategory(category.id).subscribe({
          next: (result) => {
            this.categories.update((categories) => categories.filter((c) => c.id !== category.id));

            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail:
                result.reassignedTransactionCount > 0
                  ? this.translate.instant('category.list.deletedWithReassignment', {
                      count: result.reassignedTransactionCount,
                    })
                  : this.translate.instant('category.list.deleted'),
              life: 3000,
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('category.list.deleteFailed'),
            });
          },
        });
      },
    });
  }

  submit(value: CategoryFormValue) {
    const selected = this.selectedCategory();
    if (selected) {
      this.updateCategory(selected, value);
      return;
    }
    this.createCategory(value);
  }

  private createCategory(value: CategoryFormValue) {
    const categoryData: CreateCategorieDto = value;

    this.categorieService.createCategory(categoryData).subscribe({
      next: (created) => {
        this.categories.update((categories) => [...categories, created]);
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('category.form.created'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('category.form.createFailed'),
        });
      },
    });
  }

  private updateCategory(selected: Categorie, value: CategoryFormValue) {
    this.categorieService.updateCategory(selected.id, value).subscribe({
      next: (updated) => {
        this.categories.update((categories) =>
          categories.map((c) => (c.id === updated.id ? updated : c)),
        );
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('category.form.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('category.form.updateFailed'),
        });
      },
    });
  }

  hideDialog() {
    this.categoryDialog = false;
    this.selectedCategory.set(null);
    this.errors.set({});
  }
}
