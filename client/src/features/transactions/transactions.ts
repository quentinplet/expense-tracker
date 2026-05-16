import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { RatingModule } from 'primeng/rating';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TransactionService } from '@/core/services/transaction-service';
import {
  CreateTransactionDto,
  Transaction,
  TransactionParams,
  UpdateTransactionDto,
} from '@/types/transaction';
import { Paginator, PaginatorState } from 'primeng/paginator';
import { BusyService } from '@/core/services/busy-service';
import { CategorieService } from '@/core/services/categorie-service';
import { TabsModule } from 'primeng/tabs';
import { Categorie } from '@/types/categorie';
import { TransactionModalForm } from './transaction-modal-form/transaction-modal-form';

interface Column {
  field: string;
  header: string;
  customExportHeader?: string;
}

interface ExportColumn {
  title: string;
  dataKey: string;
}

@Component({
  selector: 'app-transactions',
  imports: [
    CommonModule,
    TableModule,
    FormsModule,
    ButtonModule,
    RippleModule,
    ToastModule,
    ToolbarModule,
    RatingModule,
    InputTextModule,
    SelectModule,
    DialogModule,
    TagModule,
    InputIconModule,
    IconFieldModule,
    ConfirmDialogModule,
    Paginator,
    ReactiveFormsModule,
    TransactionModalForm,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.scss',
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

  selectedTransaction = signal<Transaction | null>(null);
  selectedTransactions = signal<Transaction[]>([]);

  submitted: boolean = false;
  exportColumns!: ExportColumn[];

  cols!: Column[];

  private fb = inject(FormBuilder);
  private transactionService = inject(TransactionService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  protected busyService = inject(BusyService);
  protected categorieService = inject(CategorieService);

  transactionForm = this.fb.nonNullable.group({
    description: ['', Validators.required],
    categoryId: [0, Validators.required],
    amount: [0, Validators.required],
    date: [new Date(), Validators.required],
  });

  exportCSV() {
    this.dt.exportCSV();
  }

  ngOnInit() {
    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
    this.loadCategories();
  }

  loadTransactions(event: TableLazyLoadEvent) {
    const page =
      Math.floor((event.first ?? 0) / (event.rows ?? this.transactionParams.pageSize)) + 1;
    const pageSize = event.rows ?? this.transactionParams.pageSize;
    this.transactionService.getTransactions(page, pageSize).subscribe({
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

  onGlobalFilter(table: Table, event: Event) {
    table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
  }

  openNew() {
    this.selectedTransaction.set(null);
    this.transactionForm.reset();
    this.submitted = false;
    this.errors.set({});
    this.transactionDialog = true;
  }

  // ✅ Type correct — plus de `{ id: string; name: string }`
  editTransaction(transaction: Transaction) {
    this.selectedTransaction.set(transaction);

    this.transactionForm.patchValue({
      description: transaction.description,
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
      message: 'Are you sure you want to delete the selected transactions?',
      header: 'Confirm',
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
              summary: 'Successful',
              detail: 'Transactions Deleted',
              life: 3000,
            });
          },
        });
      },
    });
  }

  deleteTransaction(transaction: Transaction) {
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete ' + transaction.description + '?',
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.transactionService.deleteTransaction(transaction.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Successful',
              detail: 'Transaction Deleted',
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
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Deletion Failed',
              detail: 'An error occurred while deleting the transaction.',
            });
          },
        });
      },
    });
  }

  submit() {
    const transactionData = this.transactionForm.getRawValue();
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
          summary: 'Successful',
          detail: 'Transaction Created',
          life: 3000,
        });
      },
      error: (error) => {
        this.errors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: 'Creation Failed',
          detail: 'An error occurred while creating the transaction.',
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
            summary: 'Successful',
            detail: 'Transaction Updated',
            life: 3000,
          });
        },
        error: (error) => {
          this.errors.set(error);
          this.messageService.add({
            severity: 'error',
            summary: 'Update Failed',
            detail: 'An error occurred while updating the transaction.',
          });
        },
      });
  }

  hideDialog() {
    this.transactionDialog = false;
    this.selectedTransaction.set(null);
    this.transactionForm.reset();
    this.submitted = false;
    this.errors.set({});
  }

  reloadCurrentPage() {
    const page = this.transactionParams.pageNumber;
    const pageSize = this.transactionParams.pageSize;
    this.loadTransactions({ first: (page - 1) * pageSize, rows: pageSize });
  }
}
