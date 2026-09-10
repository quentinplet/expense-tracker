import {
  Component,
  computed,
  inject,
  input,
  model,
  OnChanges,
  output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { combineLatest, distinctUntilChanged, map, of, startWith, switchMap } from 'rxjs';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { Select, SelectChangeEvent } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { Budget } from '@/types/budget';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { AmountInput } from '@/shared/components/amount-input/amount-input';
import { BudgetService } from '@/core/services/budget-service';
import { MonthKey, monthKeyToDate, shiftMonth, toMonthKey } from '@/features/dashboard/month';
import { CategoryFormDialog, CategoryFormValue } from '@/features/categories/category-form-dialog/category-form-dialog';

/** Valeur bornée du select : ne coïncide avec aucun Guid de catégorie, et n'est
 *  jamais `null` — `null` reste réservé à « rien encore choisi » pour que
 *  `Validators.required` continue de fonctionner (voir onSubmit). */
const GLOBAL_OPTION_ID = '__global__';

/** Même sentinelle et même logique que `TransactionModalForm` : une ligne
 *  factice en fin de select, jamais une vraie catégorie. */
const NEW_CATEGORY_OPTION_ID = '__new-category__';

export type BudgetFormValue = {
  /** Null == budget global. */
  categoryId: string | null;
  amountLimit: number;
  autoRenew: boolean;
  /** Ignoré par le parent en édition (Month est immuable, §Key Gotchas backend) —
   *  n'a d'effet qu'à la création. */
  month: MonthKey;
};

@Component({
  selector: 'app-budget-form-dialog',
  imports: [
    Dialog,
    Button,
    Select,
    DatePicker,
    ToggleSwitch,
    ReactiveFormsModule,
    TranslatePipe,
    CategoryBadge,
    AmountInput,
    CategoryFormDialog,
  ],
  templateUrl: './budget-form-dialog.html',
  styleUrl: './budget-form-dialog.scss',
  // Injecté pour construire les options du select et le libellé d'en-tête en
  // lecture seule — même raison que transaction-modal-form.ts.
  providers: [CategoryNamePipe],
})
export class BudgetFormDialog implements OnChanges {
  visible = model.required<boolean>();
  categories = input.required<Categorie[]>();
  errors = input.required<Record<string, string[]>>();

  /** Le mois affiché sur l'écran — pré-remplit le champ mois à l'ouverture d'une
   *  création, modifiable ensuite (§ Planifier un budget futur). */
  month = input.required<MonthKey>();

  /** Le budget à éditer, ou `null` pour une création. */
  budget = input<Budget | null>(null);

  /** Présélectionne « Budget global » à l'ouverture d'une création — depuis le
   *  bouton `+` de la carte placeholder du budget global (§ budgets.ts). Sans
   *  effet en édition, ni si l'option est déjà exclue par takenSelections. */
  presetGlobal = input(false);

  /** CategoryId (ou `null` pour le global) déjà budgétés ce mois-ci — exclus du
   *  select de création pour ne pas remplir un formulaire qui finira en 400. */
  takenSelections = input<(string | null)[]>([]);

  close = output<void>();
  save = output<BudgetFormValue>();

  /** Remonte au parent, seul propriétaire de `categories` — même pattern que
   *  `TransactionModalForm.categoryCreated`. */
  categoryCreated = output<Categorie>();

  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);
  private budgetService = inject(BudgetService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);

  protected categoryFormVisible = signal(false);
  protected categoryFormErrors = signal<Record<string, string[]>>({});

  protected form = this.fb.group({
    categoryId: this.fb.nonNullable.control('', Validators.required),
    amountLimit: this.fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
    autoRenew: this.fb.nonNullable.control(false),
    month: this.fb.nonNullable.control<Date>(new Date(), Validators.required),
  });

  protected isEditing = computed(() => this.budget() !== null);

  /**
   * `takenSelections` vient des budgets du mois affiché sur la page — dès que le champ
   * mois du formulaire s'en écarte, ce filtre porterait sur le mauvais mois. Plutôt que
   * de laisser passer une catégorie déjà budgétée sur le mois choisi (le backend la
   * refuserait alors au submit, en anglais, via l'intercepteur générique), on recharge
   * les budgets du nouveau mois à la volée — un flux HTTP est le bon usage de RxJS ici
   * (coding-standards.md), pas un signal. `null` tant qu'on reste sur le mois de la page :
   * pas d'appel réseau superflu, `categoryOptions` retombe alors sur `takenSelections()`.
   */
  private liveTakenSelections = toSignal(
    this.form.controls.month.valueChanges.pipe(
      map((date) => toMonthKey(date)),
      distinctUntilChanged(),
      switchMap((monthKey) =>
        monthKey === this.month()
          ? of(null)
          : this.budgetService
              .getBudgets(monthKey)
              .pipe(map((budgets) => budgets.map((b) => b.categoryId))),
      ),
    ),
    { initialValue: null },
  );

  /**
   * Catégories déjà budgétées le mois SUIVANT celui du formulaire — sert à prévenir
   * dans le dialog lui-même que la duplication automatique n'aura aucun effet
   * (§ BudgetService.GetBudgetsForMonthAsync côté backend, § budget-card.html côté
   * carte). Ne part en requête que si `autoRenew` est actif : c'est la seule
   * situation où la question se pose, pas la peine d'interroger l'API sinon.
   */
  private nextMonthTaken = toSignal(
    combineLatest([
      this.form.controls.month.valueChanges.pipe(
        startWith(this.form.controls.month.value),
        map((date) => toMonthKey(date)),
        distinctUntilChanged(),
      ),
      this.form.controls.autoRenew.valueChanges.pipe(
        startWith(this.form.controls.autoRenew.value),
        distinctUntilChanged(),
      ),
    ]).pipe(
      switchMap(([monthKey, autoRenew]) =>
        autoRenew
          ? this.budgetService
              .getBudgets(shiftMonth(monthKey, 1))
              .pipe(map((budgets) => budgets.map((b) => b.categoryId)))
          : of(null),
      ),
    ),
    { initialValue: null },
  );

  /** Miroir signal de `categoryId` — même raison que `liveTakenSelections` (la valeur
   *  d'un FormControl n'est pas un signal). */
  private selectedCategoryId = toSignal(this.form.controls.categoryId.valueChanges, {
    initialValue: this.form.controls.categoryId.value,
  });

  /** True quand la duplication automatique, une fois activée, n'aura concrètement
   *  aucun effet : un budget existe déjà le mois suivant pour la cible choisie. */
  protected autoRenewConflict = computed(() => {
    const taken = this.nextMonthTaken();
    if (taken === null) return false;

    const categoryId = this.selectedCategoryId();
    if (!categoryId) return false;

    return new Set(taken).has(categoryId === GLOBAL_OPTION_ID ? null : categoryId);
  });

  /** N'affiche une erreur qu'après une tentative de soumission — même raison que
   *  transaction-modal-form.ts : `touched` seul se déclenche sur un simple blur. */
  private submitted = signal(false);
  protected showErrors = this.submitted.asReadonly();

  /** Mêmes tokens que les autres champs de dialogue (§ transaction-modal-form.ts) :
   *  Tailwind ne peut pas battre le thème PrimeNG injecté hors layer. */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  /** Le p-date-picker n'expose pas de token de padding — même contournement que
   *  transaction-modal-form.ts, la variable héritée depuis l'hôte. */
  protected readonly datePickerTokens = { '--p-inputtext-padding-y': '0.9rem' };

  /** Libellé de l'en-tête en édition : catégorie ou « Budget global », lecture seule. */
  protected editingCategoryLabel = computed(() => {
    const budget = this.budget();
    if (!budget) return '';
    if (budget.categoryId === null) return this.translate.instant('budgets.form.global');
    return this.categoryNamePipe.transform({
      name: budget.categoryName!,
      translationKey: budget.categoryTranslationKey,
    });
  });

  protected editingIcon = computed(() => (this.budget()?.categoryId === null ? 'pi-wallet' : this.budget()?.categoryIcon));
  protected editingColor = computed(() =>
    this.budget()?.categoryId === null ? 'var(--p-primary-500)' : this.budget()?.categoryColor,
  );

  /** Options du select de création : le global en tête, puis les catégories de
   *  dépense — l'un et l'autre exclus s'ils sont déjà budgétés sur le mois choisi
   *  (§ liveTakenSelections). La dernière ligne est factice, voir
   *  `onCategorySelectChange`. */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    const taken = new Set(this.liveTakenSelections() ?? this.takenSelections());

    const options: { id: string; label: string; icon?: string; color?: string; isAction: boolean }[] = [];
    if (!taken.has(null)) {
      options.push({
        id: GLOBAL_OPTION_ID,
        label: this.translate.instant('budgets.form.global'),
        isAction: false,
      });
    }
    options.push(
      ...this.categories()
        .filter((c) => !taken.has(c.id))
        .map((c) => ({
          id: c.id,
          label: this.categoryNamePipe.transform(c),
          icon: c.icon ?? undefined,
          color: c.color ?? undefined,
          isAction: false,
        })),
    );
    options.push({
      id: NEW_CATEGORY_OPTION_ID,
      label: this.translate.instant('transaction.form.addCategory'),
      isAction: true,
    });
    return options;
  });

  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible()) {
      this.resetFrom(this.budget());
    }
  }

  private resetFrom(budget: Budget | null) {
    this.submitted.set(false);

    if (!budget) {
      // Le global a pu être créé entre-temps (un autre onglet) depuis que le
      // bouton `+` a été affiché : présélectionner une option qui n'est plus
      // dans categoryOptions() laisserait le select sans libellé visible.
      const globalStillAvailable = !this.takenSelections().includes(null);
      this.form.reset({
        categoryId: this.presetGlobal() && globalStillAvailable ? GLOBAL_OPTION_ID : '',
        amountLimit: 0,
        autoRenew: false,
        month: monthKeyToDate(this.month()),
      });
      return;
    }

    this.form.reset({
      categoryId: budget.categoryId ?? GLOBAL_OPTION_ID,
      amountLimit: budget.amountLimit,
      autoRenew: budget.autoRenew,
      month: monthKeyToDate(budget.month),
    });
  }

  onClose() {
    this.close.emit();
  }

  /** L'option « Ajouter une catégorie » ne doit jamais rester sélectionnée — même
   *  logique que `TransactionModalForm.onCategorySelectChange`. */
  onCategorySelectChange(event: SelectChangeEvent) {
    if (event.value !== NEW_CATEGORY_OPTION_ID) return;

    this.form.controls.categoryId.setValue('');
    this.categoryFormErrors.set({});
    this.categoryFormVisible.set(true);
  }

  onCategoryFormClose() {
    this.categoryFormVisible.set(false);
  }

  onCategoryFormSave(value: CategoryFormValue) {
    this.categorieService.createCategory(value).subscribe({
      next: (created) => {
        // Émis avant `setValue` : le parent met à jour `expenseCategories` de façon
        // synchrone, donc `categoryOptions()` porte déjà la nouvelle catégorie au
        // moment où le select doit résoudre l'id sélectionné. Les budgets ne
        // portent que sur des catégories de dépense — le parent ignore le reste.
        this.categoryCreated.emit(created);
        if (created.type === 'Expense') {
          this.form.controls.categoryId.setValue(created.id);
        }
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

  onSubmit() {
    if (this.form.invalid) {
      this.submitted.set(true);
      this.form.markAllAsTouched();
      return;
    }

    const { categoryId, amountLimit, autoRenew, month } = this.form.getRawValue();

    this.save.emit({
      categoryId: categoryId === GLOBAL_OPTION_ID ? null : categoryId,
      // `required` + `min(0.01)` : on n'arrive ici que par un formulaire valide.
      amountLimit: amountLimit ?? 0,
      autoRenew,
      month: toMonthKey(month),
    });
  }
}