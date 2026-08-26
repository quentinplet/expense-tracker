import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { TransactionService } from '@/core/services/transaction-service';
import {
  CreateTransactionDto,
  Transaction,
  TransactionParams,
  TransactionType,
  UpdateTransactionDto,
} from '@/types/transaction';
import { Paginator, PaginatorState } from 'primeng/paginator';
import { BusyService } from '@/core/services/busy-service';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { TransactionModalForm } from './transaction-modal-form/transaction-modal-form';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';

@Component({
  selector: 'app-transactions',
  imports: [
    CommonModule,
    TableModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    InputTextModule,
    SelectModule,
    InputIconModule,
    IconFieldModule,
    Paginator,
    ReactiveFormsModule,
    TransactionModalForm,
    TranslatePipe,
    CategoryNamePipe,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.scss',
  // Injecté pour construire les options du filtre : optionLabel prend un nom de
  // champ, pas un pipe, donc les libellés se calculent en TypeScript.
  providers: [CategoryNamePipe],
})
export class Transactions implements OnInit {
  @ViewChild('dt') dt!: Table;

  protected transactionParams = new TransactionParams();
  totalRecords = signal(0);

  protected readonly Math = Math;

  transactionDialog: boolean = false;
  transactions = signal<Transaction[]>([]);
  categories = signal<Categorie[]>([]);
  errors = signal<Record<string, string[]>>({});
  private searchSubject = new Subject<string>();
  protected searchValue: string = '';
  protected selectedCategoryId: string | null = null;
  protected selectedTransactionType: string | null = null;
  private destroyRef = inject(DestroyRef);

  selectedTransaction = signal<Transaction | null>(null);
  selectedTransactions = signal<Transaction[]>([]);

  private fb = inject(FormBuilder);
  private transactionService = inject(TransactionService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  protected busyService = inject(BusyService);
  protected categorieService = inject(CategorieService);
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);
  private categoryNamePipe = inject(CategoryNamePipe);

  /**
   * Reconstruit à chaque changement de langue : les options d'un p-select sont des
   * objets figés, elles ne se retraduisent pas seules.
   */
  protected readonly transactionTypes = computed(() => {
    this.translate.currentLang();
    return [
      { label: this.translate.instant('transaction.type.income'), value: 'income' },
      { label: this.translate.instant('transaction.type.expense'), value: 'expense' },
    ];
  });

  /** Aucun LOCALE_ID n'est fourni : sans cet argument, les pipes rendent en en-US. */
  protected locale = computed(() => this.languageService.current());

  /**
   * Options du filtre catégorie, libellés résolus par le pipe pour que la règle
   * « clé de traduction si catégorie système, nom sinon » ne vive qu'à un endroit (§10).
   */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    return this.categories().map((categorie) => ({
      id: categorie.id,
      label: this.categoryNamePipe.transform(categorie),
    }));
  });

  /** Adapte la transaction à la forme attendue par le pipe. */
  protected categoryOf(transaction: Transaction) {
    return {
      name: transaction.categoryName,
      translationKey: transaction.categoryTranslationKey,
    };
  }

  transactionForm = this.fb.nonNullable.group({
    label: ['', Validators.required],
    note: [''],
    type: ['Expense' as TransactionType, Validators.required],
    categoryId: ['', Validators.required],
    amount: [0, Validators.required],
    date: [new Date(), Validators.required],
  });

  ngOnInit() {
    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
    this.loadCategories();
    this.configureDebounce();
  }

  configureDebounce() {
    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((searchTerm) => {
        this.transactionParams.search = searchTerm;

        // reset to first page when search term changes
        this.transactionParams.pageNumber = 1;

        this.loadTransactions({
          first: 0,
          rows: this.transactionParams.pageSize,
        } as TableLazyLoadEvent);
      });
  }

  onSearch(event: Event) {
    const searchTerm = (event.target as HTMLInputElement).value.trim();
    this.searchValue = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  clearSearch() {
    this.searchValue = '';
    this.transactionParams.search = undefined;
    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
  }

  onCategoryChange(categoryId: string | null) {
    this.selectedCategoryId = categoryId;
    this.transactionParams.categoryId = categoryId ?? undefined;

    // reset to first page when category filter changes
    this.transactionParams.pageNumber = 1;

    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
  }

  onTypeChange(type: string | null) {
    this.selectedTransactionType = type;
    this.transactionParams.transactionType = type ?? undefined;

    // reset to first page when type filter changes
    this.transactionParams.pageNumber = 1;

    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
  }

  resetFilters() {
    this.searchValue = '';
    this.selectedCategoryId = null;
    this.selectedTransactionType = null;

    this.transactionParams.search = undefined;
    this.transactionParams.categoryId = undefined;
    this.transactionParams.transactionType = undefined;
    this.transactionParams.sortBy = undefined;
    this.transactionParams.sortDirection = 'desc';
    this.transactionParams.pageNumber = 1;

    // reset PrimeNG table state
    this.dt.reset();

    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
  }

  loadTransactions(event: TableLazyLoadEvent) {
    this.transactionParams.pageNumber =
      Math.floor((event.first ?? 0) / (event.rows ?? this.transactionParams.pageSize)) + 1;

    this.transactionParams.pageSize = event.rows ?? this.transactionParams.pageSize;

    // Sort
    if (typeof event.sortField === 'string') {
      this.transactionParams.sortBy = event.sortField;
    }
    if (event.sortOrder !== undefined && event.sortOrder !== null) {
      this.transactionParams.sortDirection = event.sortOrder === 1 ? 'asc' : 'desc';
    }

    // La table est paginée côté serveur : une sélection conservée d'une page à l'autre
    // ne serait plus visible à l'écran, et la suppression groupée porterait sur des
    // lignes que l'utilisateur ne voit pas.
    this.selectedTransactions.set([]);

    this.transactionService.getTransactions(this.transactionParams).subscribe({
      next: (result) => {
        this.transactions.set(result.items);
        this.totalRecords.set(result.metadata.totalCount);
      },
    });
  }

  loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
    });
  }

  onPageChange(event: PaginatorState) {
    const page =
      Math.floor((event.first ?? 0) / (event.rows ?? this.transactionParams.pageSize)) + 1;
    this.transactionParams.pageNumber = page;
    this.transactionParams.pageSize = event.rows ?? this.transactionParams.pageSize;
    this.loadTransactions({ first: event.first, rows: event.rows });
  }

  openNew() {
    this.selectedTransaction.set(null);
    this.transactionForm.reset();
    this.errors.set({});
    this.transactionDialog = true;
  }

  // ✅ Type correct — plus de `{ id: string; name: string }`
  editTransaction(transaction: Transaction) {
    this.selectedTransaction.set(transaction);

    this.transactionForm.patchValue({
      label: transaction.label,
      note: transaction.note ?? '',
      type: transaction.type,
      amount: transaction.amount,
      date: new Date(transaction.date),
      categoryId: transaction.categoryId,
    });

    this.transactionDialog = true;
  }

  deleteSelectedTransactions() {
    const selectedTransactions = this.selectedTransactions();
    if (!selectedTransactions.length) return;

    this.confirmationService.confirm({
      message: this.translate.instant('transaction.list.deleteMany'),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.transactionService.deleteTransactions(selectedTransactions).subscribe({
          next: () => {
            const selectedIds = new Set(selectedTransactions.map((t) => t.id));
            this.transactions.update((transactions) =>
              transactions.filter((t) => !selectedIds.has(t.id)),
            );
            this.totalRecords.update((count) => count - selectedTransactions.length);
            this.selectedTransactions.set([]);
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('transaction.list.deletedMany'),
              life: 3000,
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('transaction.list.deleteFailed'),
            });
          },
        });
      },
    });
  }

  deleteTransaction(transaction: Transaction) {
    this.confirmationService.confirm({
      message: this.translate.instant('transaction.list.deleteOne', { label: transaction.label }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.transactionService.deleteTransaction(transaction.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('transaction.list.deleted'),
              life: 3000,
            });
            this.transactions.update((transactions) =>
              transactions.filter((t) => t.id !== transaction.id),
            );
            this.totalRecords.update((count) => count - 1);
            this.selectedTransaction.set(null);
            if (this.transactions().length === 1 && this.transactionParams.pageNumber > 1) {
              this.transactionParams.pageNumber--;
              this.reloadCurrentPage();
            }
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('transaction.list.deleteFailed'),
            });
          },
        });
      },
    });
  }

  private toPayload(): CreateTransactionDto {
    const { label, note, type, categoryId, amount, date } = this.transactionForm.getRawValue();
    return {
      label,
      note: note || null,
      type,
      categoryId,
      amount,
      // L'API attend une DateOnly : date locale au format yyyy-MM-dd, sans fuseau.
      date: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`,
    };
  }

  submit() {
    const transactionData = this.toPayload();
    if (this.selectedTransaction()) {
      this.updateTransaction(transactionData);
      return;
    }
    this.addNewTransaction(transactionData);
  }

  addNewTransaction(transactionData: CreateTransactionDto) {
    console.log('Adding new transaction with data:', transactionData);
    const newTransaction: CreateTransactionDto = {
      ...transactionData,
    };
    this.transactionService.addNewTransaction(newTransaction).subscribe({
      next: (createdTransaction) => {
        this.transactions.update((transactions) => [
          createdTransaction,
          ...transactions.slice(0, -1),
        ]);
        this.totalRecords.update((count) => count + 1);
        this.hideDialog();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('transaction.form.created'),
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('transaction.form.createFailed'),
        });
      },
    });
  }

  updateTransaction(transactionData: UpdateTransactionDto) {
    if (!this.selectedTransaction()) return;

    const updatedTransaction: UpdateTransactionDto = {
      ...transactionData,
    };
    this.transactionService
      .updateTransaction(this.selectedTransaction()!.id, updatedTransaction)
      .subscribe({
        next: (updatedTransaction) => {
          this.transactions.update((transactions) =>
            transactions.map((t) => (t.id === updatedTransaction.id ? updatedTransaction : t)),
          );
          this.hideDialog();
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('transaction.form.updated'),
            life: 3000,
          });
        },
        error: (error) => {
          this.errors.set(error);
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('transaction.form.updateFailed'),
          });
        },
      });
  }

  hideDialog() {
    this.transactionDialog = false;
    this.selectedTransaction.set(null);
    this.transactionForm.reset();
    this.errors.set({});
  }

  reloadCurrentPage() {
    const page = this.transactionParams.pageNumber;
    const pageSize = this.transactionParams.pageSize;
    this.loadTransactions({ first: (page - 1) * pageSize, rows: pageSize });
  }
}
