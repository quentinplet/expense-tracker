import { CommonModule, Location } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { FormsModule } from '@angular/forms';
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
  UpdateTransactionDto,
} from '@/types/transaction';
import { DatePicker } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { BusyService } from '@/core/services/busy-service';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import {
  TransactionFormValue,
  TransactionModalForm,
} from './transaction-modal-form/transaction-modal-form';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';

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
    TransactionModalForm,
    TranslatePipe,
    CategoryNamePipe,
    DatePicker,
    TooltipModule,
    CategoryBadge,
    RouterLink,
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

  /** Aura fixe le padding des en-têtes en CSS non-layered : une classe Tailwind
   *  py-* n'aurait aucun effet dessus, il faut passer par le token du composant. */
  protected readonly headerTokens = { headerCell: { padding: '1rem 1rem' } };

  transactionDialog = false;
  transactions = signal<Transaction[]>([]);
  categories = signal<Categorie[]>([]);
  errors = signal<Record<string, string[]>>({});
  private searchSubject = new Subject<string>();
  protected searchValue = '';
  protected selectedCategoryId: string | null = null;
  protected selectedTransactionType: string | null = null;
  private destroyRef = inject(DestroyRef);

  selectedTransaction = signal<Transaction | null>(null);
  selectedTransactions = signal<Transaction[]>([]);

  private transactionService = inject(TransactionService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  protected busyService = inject(BusyService);
  protected categorieService = inject(CategorieService);
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);
  private categoryNamePipe = inject(CategoryNamePipe);
  private route = inject(ActivatedRoute);
  private location = inject(Location);

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

  /** Le format du p-datepicker ne se déduit pas de la locale, il faut le lui donner. */
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /**
   * Source unique de la pagination, écrite uniquement par `loadTransactions`. La
   * p-table la **lit** via `first()`/`rows()` ; son propre setter `first` est une
   * simple affectation, il n'émet pas `onLazyLoad` et ne peut donc pas boucler.
   */
  private paging = signal({ first: 0, rows: new TransactionParams().pageSize });
  protected first = computed(() => this.paging().first);
  protected rows = computed(() => this.paging().rows);

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

  /** Segmenté `Tout | Dépenses | Revenus` : la valeur nulle est l'option « Tout ». */
  protected readonly typeSegments = computed(() => {
    this.translate.currentLang();
    return [
      { label: this.translate.instant('transaction.filters.typeAll'), value: null },
      { label: this.translate.instant('transaction.filters.typeExpense'), value: 'expense' },
      { label: this.translate.instant('transaction.filters.typeIncome'), value: 'income' },
    ];
  });

  /** Un filtre est actif : l'état vide doit dire « aucun résultat », pas « aucune donnée ». */
  protected hasActiveFilters = computed(
    () =>
      !!this.searchValue ||
      !!this.selectedCategoryId ||
      !!this.selectedTransactionType ||
      !!this.selectedPeriod(),
  );

  /** Plage du p-datepicker : [début] ou [début, fin], fin nulle tant qu'on choisit. */
  protected selectedPeriod = signal<Date[] | null>(null);

  onPeriodChange(range: Date[] | null) {
    this.selectedPeriod.set(range);

    const [from, to] = range ?? [];
    this.transactionParams.dateFrom = from ? this.toIsoDate(from) : undefined;
    this.transactionParams.dateTo = to ? this.toIsoDate(to) : undefined;

    // Tant que la fin n'est pas choisie, le p-datepicker n'a qu'une borne : on
    // n'interroge pas le serveur sur une plage à moitié saisie.
    if (range && range.length && !to) return;

    this.transactionParams.pageNumber = 1;
    this.loadTransactions({ first: 0, rows: this.transactionParams.pageSize });
  }

  /** L'API attend une DateOnly : date locale en yyyy-MM-dd, jamais un toISOString
   *  qui basculerait d'un jour selon le fuseau. */
  private toIsoDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  ngOnInit() {
    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
    this.loadCategories();
    this.configureDebounce();

    // Ouverture depuis l'action rapide du dashboard (§ QuickActions) : le
    // paramètre est retiré aussitôt, sinon un rechargement ou un retour arrière
    // rouvrirait le dialogue tout seul. `Location.replaceState` plutôt que
    // `Router.navigate` : ce dernier déclenche une seconde navigation pendant que
    // l'API View Transitions anime encore la première (`withViewTransitions()`,
    // app.config.ts), ce qui produit un `AbortError` bruyant en console.
    if (this.route.snapshot.queryParamMap.has('new')) {
      this.openNew();
      this.location.replaceState(this.location.path().split('?')[0]);
    }
  }

  private configureDebounce() {
    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((searchTerm) => {
        this.transactionParams.search = searchTerm;

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

    this.transactionParams.pageNumber = 1;

    this.loadTransactions({
      first: 0,
      rows: this.transactionParams.pageSize,
    } as TableLazyLoadEvent);
  }

  onTypeChange(type: string | null) {
    this.selectedTransactionType = type;
    this.transactionParams.transactionType = type ?? undefined;

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
    this.selectedPeriod.set(null);

    this.transactionParams.search = undefined;
    this.transactionParams.categoryId = undefined;
    this.transactionParams.transactionType = undefined;
    this.transactionParams.dateFrom = undefined;
    this.transactionParams.dateTo = undefined;
    this.transactionParams.sortDirection = 'desc';
    this.transactionParams.pageNumber = 1;

    // `dt.reset()` remet l'affichage à zéro (tri, icônes, première page) et émet lui-même
    // un `onLazyLoad` puisque la table est en mode lazy — inutile de rappeler
    // `loadTransactions` en plus, ça doublait la requête réseau à chaque réinitialisation.
    this.dt.reset();
  }

  loadTransactions(event: TableLazyLoadEvent) {
    const first = event.first ?? 0;
    const rows = event.rows ?? this.transactionParams.pageSize;

    this.transactionParams.pageNumber = Math.floor(first / rows) + 1;
    this.transactionParams.pageSize = rows;

    // Tous les chemins de chargement passent ici : c'est le seul endroit qui écrit
    // la pagination, donc le seul qui puisse désynchroniser les deux paginateurs.
    this.paging.set({ first, rows });

    // Sort. `sortOrder` n'a de sens que rattaché à une colonne triée : `dt.reset()`
    // émet lui-même un `onLazyLoad` avec `sortField: null` mais `sortOrder: 1` (valeur
    // par défaut de PrimeNG), qui écraserait sinon la direction voulue par l'appelant.
    if (typeof event.sortField === 'string') {
      this.transactionParams.sortBy = event.sortField;
      if (event.sortOrder !== undefined && event.sortOrder !== null) {
        this.transactionParams.sortDirection = event.sortOrder === 1 ? 'asc' : 'desc';
      }
    } else {
      this.transactionParams.sortBy = undefined;
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

  private loadCategories() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
    });
  }

  // La transaction sélectionnée est le seul état transmis : le dialogue possède son
  // formulaire et se remplit lui-même à l'ouverture.
  openNew() {
    this.selectedTransaction.set(null);
    this.errors.set({});
    this.transactionDialog = true;
  }

  editTransaction(transaction: Transaction) {
    this.selectedTransaction.set(transaction);
    this.errors.set({});
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
          next: (deletedCount) => {
            this.selectedTransactions.set([]);

            // Hors de la dernière page, une ligne de la page suivante doit remonter :
            // impossible à deviner côté client, un recomptage serveur est la seule
            // option correcte. Sur la dernière page, rien ne remonte derrière, donc une
            // mise à jour locale suffit et évite un aller-retour réseau ; `deletedCount`
            // (et non selectedTransactions.length) protège `totalRecords` d'une dérive
            // si le serveur a silencieusement ignoré un id déjà supprimé ailleurs.
            if (!this.isOnLastPage()) {
              this.reloadCurrentPage();
            } else {
              const selectedIds = new Set(selectedTransactions.map((t) => t.id));
              const pageWillEmpty = this.transactions().length === selectedTransactions.length;
              this.transactions.update((transactions) =>
                transactions.filter((t) => !selectedIds.has(t.id)),
              );
              this.totalRecords.update((count) => count - deletedCount);
              if (pageWillEmpty && this.transactionParams.pageNumber > 1) {
                this.transactionParams.pageNumber--;
                this.reloadCurrentPage();
              }
            }

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
            this.selectedTransaction.set(null);

            // Hors de la dernière page, la ligne suivante doit remonter depuis le
            // serveur : un splice local laisserait la page affichée avec une rangée de
            // moins qu'une page pleine. Sur la dernière page, rien ne remonte derrière,
            // une mise à jour locale suffit et évite l'aller-retour réseau.
            if (!this.isOnLastPage()) {
              this.reloadCurrentPage();
            } else {
              // Testé avant la mutation : est-ce la dernière ligne de cette page ?
              const emptiesPage = this.transactions().length === 1;
              this.transactions.update((transactions) =>
                transactions.filter((t) => t.id !== transaction.id),
              );
              this.totalRecords.update((count) => count - 1);
              if (emptiesPage && this.transactionParams.pageNumber > 1) {
                this.transactionParams.pageNumber--;
                this.reloadCurrentPage();
              }
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

  /**
   * Le dialogue remonte la saisie ; la traduction vers le contrat de l'API se fait ici,
   * avec le même `toIsoDate()` que le filtre de période — la règle ne vit qu'à un endroit.
   */
  submit(value: TransactionFormValue) {
    const transactionData: CreateTransactionDto = {
      label: value.label,
      note: value.note || null,
      type: value.type,
      categoryId: value.categoryId,
      amount: value.amount,
      date: this.toIsoDate(value.date),
    };

    if (this.selectedTransaction()) {
      this.updateTransaction(transactionData);
      return;
    }
    this.addNewTransaction(transactionData);
  }

  private addNewTransaction(transactionData: CreateTransactionDto) {
    this.transactionService.addNewTransaction(transactionData).subscribe({
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

  private updateTransaction(transactionData: UpdateTransactionDto) {
    const selected = this.selectedTransaction();
    if (!selected) return;

    this.transactionService.updateTransaction(selected.id, transactionData).subscribe({
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
    this.errors.set({});
  }

  private reloadCurrentPage() {
    const page = this.transactionParams.pageNumber;
    const pageSize = this.transactionParams.pageSize;
    this.loadTransactions({ first: (page - 1) * pageSize, rows: pageSize });
  }

  /** Dernière page : aucune ligne suivante à faire remonter, une suppression peut donc
   *  se contenter d'une mise à jour locale plutôt que d'un recomptage serveur. */
  private isOnLastPage(): boolean {
    const { pageNumber, pageSize } = this.transactionParams;
    return pageNumber === Math.ceil(this.totalRecords() / pageSize);
  }
}
