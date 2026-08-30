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
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Categorie } from '@/types/categorie';
import { Budget } from '@/types/budget';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { LanguageService } from '@/core/services/language-service';

/** Valeur bornée du select : ne coïncide avec aucun Guid de catégorie, et n'est
 *  jamais `null` — `null` reste réservé à « rien encore choisi » pour que
 *  `Validators.required` continue de fonctionner (voir onSubmit). */
const GLOBAL_OPTION_ID = '__global__';

export type BudgetFormValue = {
  /** Null == budget global. */
  categoryId: string | null;
  amountLimit: number;
  autoRenew: boolean;
};

@Component({
  selector: 'app-budget-form-dialog',
  imports: [
    Dialog,
    Button,
    Select,
    InputNumber,
    ToggleSwitch,
    ReactiveFormsModule,
    TranslatePipe,
    CategoryBadge,
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

  /** Le mois affiché sur l'écran — un budget créé ici lui appartient toujours. */
  month = input.required<string>();

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
  private languageService = inject(LanguageService);

  protected locale = computed(() => this.languageService.current());

  protected form = this.fb.group({
    categoryId: this.fb.nonNullable.control('', Validators.required),
    amountLimit: this.fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
    autoRenew: this.fb.nonNullable.control(false),
  });

  protected isEditing = computed(() => this.budget() !== null);

  /** N'affiche une erreur qu'après une tentative de soumission — même raison que
   *  transaction-modal-form.ts : `touched` seul se déclenche sur un simple blur. */
  private submitted = signal(false);
  protected showErrors = this.submitted.asReadonly();

  /** Mêmes tokens que les autres champs de dialogue (§ transaction-modal-form.ts) :
   *  Tailwind ne peut pas battre le thème PrimeNG injecté hors layer. */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  /** Séparateur décimal de la locale active : `,` en français, `.` en anglais. */
  private decimalChar = computed(() =>
    new Intl.NumberFormat(this.locale(), { minimumFractionDigits: 1 })
      .format(1.1)
      .replace(/\d/g, ''),
  );

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
   *  dépense — l'un et l'autre exclus s'ils sont déjà budgétés ce mois-ci. */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    const taken = new Set(this.takenSelections());

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
      });
      return;
    }

    this.form.reset({
      categoryId: budget.categoryId ?? GLOBAL_OPTION_ID,
      amountLimit: budget.amountLimit,
      autoRenew: budget.autoRenew,
    });
  }

  /** Même contournement que transaction-modal-form.ts : PrimeNG traduit la touche
   *  décimale du pavé numérique mais pas le point de la rangée principale. */
  onAmountKeydown(event: KeyboardEvent) {
    const decimal = this.decimalChar();
    if (event.key !== '.' || decimal === '.') return;

    event.preventDefault();
    const replay = new KeyboardEvent('keypress', { bubbles: true, cancelable: true });
    Object.defineProperty(replay, 'which', { get: () => decimal.charCodeAt(0) });
    Object.defineProperty(replay, 'keyCode', { get: () => decimal.charCodeAt(0) });
    (event.target as HTMLInputElement).dispatchEvent(replay);
  }

  /**
   * Le champ démarre à 0 € plutôt que vide (demande du 30/08). `p-inputnumber`
   * insère sinon les chiffres tapés dans le « 0,00 € » affiché au lieu de le
   * remplacer — même piège que documenté sur le montant de transaction, qu'un
   * défaut `null` avait évité là-bas. Ici on sélectionne tout au focus, pour que
   * la première frappe remplace le zéro plutôt que de s'y insérer.
   */
  onAmountFocus(event: Event) {
    (event.target as HTMLInputElement).select();
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

    const { categoryId, amountLimit, autoRenew } = this.form.getRawValue();

    this.save.emit({
      categoryId: categoryId === GLOBAL_OPTION_ID ? null : categoryId,
      // `required` + `min(0.01)` : on n'arrive ici que par un formulaire valide.
      amountLimit: amountLimit ?? 0,
      autoRenew,
    });
  }
}