import {
  Component,
  computed,
  inject,
  input,
  OnChanges,
  output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { ImportConfirmRow, ImportPreview, ImportPreviewRow } from '@/types/import';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { LanguageService } from '@/core/services/language-service';
import { CategoryFormDialog, CategoryFormValue } from '@/features/categories/category-form-dialog/category-form-dialog';

type WorkingRow = {
  source: ImportPreviewRow;
  selected: boolean;
  label: string;
  categoryId: string | null;
};

/** Même sentinelle et même logique que `TransactionModalForm` : une ligne
 *  factice en fin de select, jamais une vraie catégorie. */
const NEW_CATEGORY_OPTION_ID = '__new-category__';

/**
 * Tableau éditable, pas une liste en lecture seule (§ Key Gotchas frontend, doc
 * feature Import CSV). Une ligne décochée n'est simplement pas envoyée à la
 * confirmation ; catégorie et libellé restent modifiables directement ici.
 */
@Component({
  selector: 'app-import-preview-table',
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    Button,
    Checkbox,
    Select,
    InputTextModule,
    Tag,
    TranslatePipe,
    CategoryBadge,
    CategoryFormDialog,
  ],
  templateUrl: './preview-table.html',
  providers: [CategoryNamePipe],
})
export class PreviewTable implements OnChanges {
  preview = input.required<ImportPreview>();
  categories = input.required<Categorie[]>();
  loading = input(false);

  confirm = output<ImportConfirmRow[]>();
  back = output<void>();

  /** Remonte au parent, seul propriétaire de `categories` — même pattern que
   *  `TransactionModalForm.categoryCreated`. */
  categoryCreated = output<Categorie>();

  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);
  private languageService = inject(LanguageService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);

  protected locale = computed(() => this.languageService.current());
  protected rows = signal<WorkingRow[]>([]);

  protected readonly newCategoryOptionId = NEW_CATEGORY_OPTION_ID;
  protected categoryFormVisible = signal(false);
  protected categoryFormErrors = signal<Record<string, string[]>>({});
  protected categoryFormInitialType = signal<TransactionType>('Expense');

  /** Ligne d'où « Ajouter une catégorie » a été ouvert — pour y appliquer la
   *  catégorie créée à la fermeture du dialogue. */
  private pendingRowIndex: number | null = null;

  /**
   * Reconstruit à chaque nouvelle prévisualisation (nouveau fichier, ou mapping
   * corrigé) — jamais sur un changement de `categories`, qui ne justifie pas de
   * perdre les cases décochées ou les libellés déjà édités par l'utilisateur.
   */
  ngOnChanges(changes: SimpleChanges) {
    if (!changes['preview']) return;

    this.rows.set(
      this.preview().rows.map((source) => ({
        source,
        // Doublon décoché par défaut, conflit recurring coché par défaut (§ Key
        // Gotchas frontend) : un doublon est presque certainement à exclure, un
        // conflit recurring n'est qu'une alerte qui laisse la décision à l'utilisateur.
        selected: source.isValid && !source.isDuplicate,
        label: source.suggestedLabel,
        categoryId: this.defaultCategoryId(source.type),
      })),
    );
  }

  private defaultCategoryId(type: TransactionType | null): string | null {
    if (!type) return null;
    return this.categories().find((c) => c.type === type && c.isLocked)?.id ?? null;
  }

  /**
   * `p-table` sans `rowTrackBy` compare l'objet ligne par identité (le défaut
   * PrimeNG, `(index, item) => item`) — or `updateLabel`/`updateCategory`/
   * `toggleRow` remplacent `rows()` par un nouveau tableau à chaque frappe,
   * donc chaque ligne change d'identité. Angular détruisait et recréait le
   * `<input>` du libellé à chaque caractère tapé, perdant le focus après la
   * première touche. `rowNumber` (le numéro de ligne du CSV source) est
   * stable pour la durée de la prévisualisation.
   */
  protected trackByRowNumber = (_: number, row: WorkingRow) => row.source.rowNumber;

  protected selectedCount = computed(() => this.rows().filter((r) => r.selected).length);
  protected canConfirm = computed(() => this.selectedCount() > 0 && !this.loading());

  protected allValidSelected = computed(() => {
    const valid = this.rows().filter((r) => r.source.isValid);
    return valid.length > 0 && valid.every((r) => r.selected);
  });

  protected categoryOptionsFor(type: TransactionType | null) {
    this.translate.currentLang();
    const options = this.categories()
      .filter((c) => c.type === type)
      .map((c) => ({
        id: c.id,
        label: this.categoryNamePipe.transform(c),
        color: c.color as string | null,
        icon: c.icon as string | null,
        isAction: false,
      }));

    options.push({
      id: NEW_CATEGORY_OPTION_ID,
      label: this.translate.instant('transaction.form.addCategory'),
      color: null,
      icon: null,
      isAction: true,
    });

    return options;
  }

  protected toggleSelectAll(checked: boolean) {
    this.rows.update((rows) => rows.map((r) => (r.source.isValid ? { ...r, selected: checked } : r)));
  }

  protected toggleRow(index: number, selected: boolean) {
    this.rows.update((rows) => rows.map((r, i) => (i === index ? { ...r, selected } : r)));
  }

  protected updateLabel(index: number, label: string) {
    this.rows.update((rows) => rows.map((r, i) => (i === index ? { ...r, label } : r)));
  }

  protected updateCategory(index: number, categoryId: string) {
    this.rows.update((rows) => rows.map((r, i) => (i === index ? { ...r, categoryId } : r)));
  }

  /**
   * PrimeNG met à jour l'affichage du select de façon optimiste dès le clic,
   * avant même que `[ngModel]` ne repousse quoi que ce soit — donc ne rien
   * faire laisserait le select affiché sur « Ajouter une catégorie » tant que
   * `row.categoryId` ne change pas *réellement* (Angular ne rappelle
   * `writeValue` que si la valeur liée diffère de la précédente). Vide plutôt
   * que restaurer l'ancienne sélection, même logique que
   * `TransactionModalForm.onCategorySelectChange` : si l'utilisateur annule,
   * il ne perd rien qu'un reclic.
   */
  protected onCategorySelectChange(index: number, categoryId: string) {
    if (categoryId !== NEW_CATEGORY_OPTION_ID) {
      this.updateCategory(index, categoryId);
      return;
    }

    this.updateCategory(index, '');
    this.pendingRowIndex = index;
    this.categoryFormInitialType.set(this.rows()[index].source.type ?? 'Expense');
    this.categoryFormErrors.set({});
    this.categoryFormVisible.set(true);
  }

  protected onCategoryFormClose() {
    this.pendingRowIndex = null;
    this.categoryFormVisible.set(false);
  }

  protected onCategoryFormSave(value: CategoryFormValue) {
    this.categorieService.createCategory(value).subscribe({
      next: (created) => {
        // Émis avant `updateCategory` : le parent met à jour `categories` de
        // façon synchrone, donc `categoryOptionsFor()` porte déjà la nouvelle
        // catégorie au moment où le select doit résoudre l'id sélectionné.
        this.categoryCreated.emit(created);
        if (this.pendingRowIndex !== null) {
          this.updateCategory(this.pendingRowIndex, created.id);
        }
        this.pendingRowIndex = null;
        this.categoryFormVisible.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('category.form.created'),
          life: 3000,
        });
      },
      error: (error) => {
        this.categoryFormErrors.set(error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('category.form.createFailed'),
        });
      },
    });
  }

  onConfirm() {
    const rows: ImportConfirmRow[] = this.rows()
      .filter((r) => r.selected && r.source.isValid)
      .map((r) => ({
        date: r.source.date!,
        amount: r.source.amount!,
        type: r.source.type as TransactionType,
        label: r.label.trim() || r.source.suggestedLabel,
        rawLabelForHash: r.source.rawLabel,
        note: null,
        categoryId: r.categoryId,
      }));

    this.confirm.emit(rows);
  }
}
