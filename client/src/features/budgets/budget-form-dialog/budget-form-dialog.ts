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
import { distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Categorie } from '@/types/categorie';
import { Budget } from '@/types/budget';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { AmountInput } from '@/shared/components/amount-input/amount-input';
import { BudgetService } from '@/core/services/budget-service';
import { MonthKey, monthKeyToDate, toMonthKey } from '@/features/dashboard/month';

/** Valeur bornée du select : ne coïncide avec aucun Guid de catégorie, et n'est
 *  jamais `null` — `null` reste réservé à « rien encore choisi » pour que
 *  `Validators.required` continue de fonctionner (voir onSubmit). */
const GLOBAL_OPTION_ID = '__global__';

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

  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);
  private budgetService = inject(BudgetService);

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
   *  (§ liveTakenSelections). */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    const taken = new Set(this.liveTakenSelections() ?? this.takenSelections());

    const options: { id: string; label: string; icon?: string; color?: string }[] = [];
    if (!taken.has(null)) {
      options.push({ id: GLOBAL_OPTION_ID, label: this.translate.instant('budgets.form.global') });
    }
    options.push(
      ...this.categories()
        .filter((c) => !taken.has(c.id))
        .map((c) => ({
          id: c.id,
          label: this.categoryNamePipe.transform(c),
          icon: c.icon ?? undefined,
          color: c.color ?? undefined,
        })),
    );
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