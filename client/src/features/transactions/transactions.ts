import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { RatingModule } from 'primeng/rating';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TransactionService } from '@/core/services/transaction-service';
import { CreateTransactionDto, Transaction, TransactionParams } from '@/types/transaction';
import { Paginator, PaginatorState } from 'primeng/paginator';
import { BusyService } from '@/core/services/busy-service';
import { CategorieService } from '@/core/services/categorie-service';
import { TabsModule } from 'primeng/tabs';
import { Categorie } from '@/types/categorie';
import { DatePicker } from 'primeng/datepicker';

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
    TextareaModule,
    SelectModule,
    RadioButtonModule,
    InputNumberModule,
    DialogModule,
    TagModule,
    InputIconModule,
    IconFieldModule,
    ConfirmDialogModule,
    Paginator,
    ReactiveFormsModule,
    TabsModule,
    DatePicker,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.scss',
})
export class Transactions implements OnInit {
  @ViewChild('dt') dt!: Table;

  protected transactionParams = new TransactionParams();
  totalRecords = signal(0);

  protected readonly Math = Math;

  productDialog: boolean = false;
  transactions = signal<Transaction[]>([]);
  categories = signal<Categorie[]>([]);

  // ✅ Typage correct — plus de `Transaction | {}`
  transaction: Transaction = {} as Transaction;

  selectedProducts!: Transaction[] | null;

  submitted: boolean = false;

  statuses!: any[];

  exportColumns!: ExportColumn[];

  cols!: Column[];

  private fb = inject(FormBuilder);
  protected transactionForm: FormGroup;

  constructor(
    private transactionService: TransactionService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    protected busyService: BusyService,
    protected categorieService: CategorieService,
  ) {
    this.transactionForm = this.fb.nonNullable.group({
      description: ['', [Validators.required]],
      category: ['', [Validators.required]],
      amount: [0, [Validators.required]],
      date: [new Date(), [Validators.required]],
    });
  }

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
        console.log(this.transactions());
      },
    });
  }

  loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        console.log('Loaded categories:', this.categories());
      },
      error: (error) => {
        console.error('Failed to load categories:', error);
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

  loadDemoData() {
    this.statuses = [
      { label: 'INSTOCK', value: 'instock' },
      { label: 'LOWSTOCK', value: 'lowstock' },
      { label: 'OUTOFSTOCK', value: 'outofstock' },
    ];

    this.cols = [
      { field: 'code', header: 'Code', customExportHeader: 'Product Code' },
      { field: 'name', header: 'Name' },
      { field: 'image', header: 'Image' },
      { field: 'price', header: 'Price' },
      { field: 'category', header: 'Category' },
    ];

    this.exportColumns = this.cols.map((col) => ({ title: col.header, dataKey: col.field }));
  }

  onGlobalFilter(table: Table, event: Event) {
    table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
  }

  openNew() {
    console.log('Opening new transaction dialog');
    this.transaction = {} as Transaction;
    this.submitted = false;
    this.productDialog = true;
  }

  // ✅ Type correct — plus de `{ id: string; name: string }`
  editProduct(transaction: Transaction) {
    this.transaction = { ...transaction };
    this.productDialog = true;
  }

  deleteSelectedProducts() {
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete the selected products?',
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.transactions.set(
          this.transactions().filter((val) => !this.selectedProducts?.includes(val)),
        );
        this.selectedProducts = null;
        this.messageService.add({
          severity: 'success',
          summary: 'Successful',
          detail: 'Products Deleted',
          life: 3000,
        });
      },
    });
  }

  submit() {
    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    const transactionData = this.transactionForm.getRawValue();
    this.addTransaction(transactionData);
  }

  addTransaction(transactionData: CreateTransactionDto) {
    const categoryName = this.transactionForm.get('category')?.value;
    const categoryId = this.categories().find((cat) => cat.name === categoryName)?.id;
    if (!categoryId) {
      console.error('Selected category not found');
      return;
    }

    const newTransaction: CreateTransactionDto = {
      ...transactionData,
      categoryId,
    };

    console.log('Creating transaction with data:', newTransaction);
    this.transactionService.addNewTransaction(newTransaction).subscribe({
      next: (createdTransaction) => {
        console.log('Transaction created:', createdTransaction);
      },
      error: (error) => {
        console.error('Failed to create transaction:', error);
      },
    });
  }

  hideDialog() {
    this.productDialog = false;
    this.transactionForm.reset();
    this.submitted = false;
  }

  // ✅ Type correct
  deleteProduct(product: Transaction) {
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete ' + product.description + '?',
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.transactions.set(this.transactions().filter((val) => val.id !== product.id));
        this.transaction = {} as Transaction;
        this.messageService.add({
          severity: 'success',
          summary: 'Successful',
          detail: 'Product Deleted',
          life: 3000,
        });
      },
    });
  }

  findIndexById(id: string): number {
    let index = -1;
    for (let i = 0; i < this.transactions().length; i++) {
      if (this.transactions()[i].id === id) {
        index = i;
        break;
      }
    }
    return index;
  }

  createId(): string {
    let id = '';
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    for (let i = 0; i < 5; i++) {
      id += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return id;
  }

  getSeverity(status: string) {
    switch (status) {
      case 'INSTOCK':
        return 'success';
      case 'LOWSTOCK':
        return 'warn';
      case 'OUTOFSTOCK':
        return 'danger';
      default:
        return 'info';
    }
  }

  saveProduct() {
    this.submitted = true;

    // ✅ Plus de cast `as TransactioN` (typo), copie propre du tableau
    if (this.transaction.description?.trim()) {
      const _transactions = [...this.transactions()];

      if (this.transaction.id) {
        const index = this.findIndexById(this.transaction.id);
        _transactions[index] = this.transaction;
        this.transactions.set(_transactions);
        this.messageService.add({
          severity: 'success',
          summary: 'Successful',
          detail: 'Transaction Updated',
          life: 3000,
        });
      } else {
        this.transaction.id = this.createId();
        this.transactions.set([..._transactions, this.transaction]);
        this.messageService.add({
          severity: 'success',
          summary: 'Successful',
          detail: 'Transaction Created',
          life: 3000,
        });
      }

      this.productDialog = false;
      this.transaction = {} as Transaction;
    }
  }
}
