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
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { Frequency, RecurringExpense } from '@/types/recurring-expense';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { AmountInput } from '@/shared/components/amount-input/amount-input';
import { LanguageService } from '@/core/services/language-service';

/**
 * Ce que le dialogue remonte : la saisie telle que le formulaire la porte,
 * `nextDueDate` en `Date` — la conversion vers le contrat de l'API (chaîne ISO)
 * appartient au parent, même découpage que TransactionFormValue.
 */
export type RecurringFormValue = {
  label: string;
  amount: number;
  type: TransactionType;
  frequency: Frequency;
  categoryId: string;
  nextDueDate: Date;
  active: boolean;
};

const FREQUENCIES: Frequency[] = ['Daily', 'Weekly', 'Monthly', 'Yearly'];

@Component({
  selector: 'app-recurring-form-dialog',
  imports: [
    Dialog,
    DatePicker,
    Button,
    Select,
    ToggleSwitch,
    InputTextModule,
    ReactiveFormsModule,
    TranslatePipe,
    CategoryBadge,
    AmountInput,
  ],
  templateUrl: './recurring-form-dialog.html',
  styleUrl: './recurring-form-dialog.scss',
  // Injecté pour construire les options du select : optionLabel prend un nom de
  // champ, pas un pipe, donc les libellés se calculent en TypeScript.
  providers: [CategoryNamePipe],
})
export class RecurringFormDialog implements OnChanges {
  visible = model.required<boolean>();
  categories = input.required<Categorie[]>();
  errors = input.required<Record<string, string[]>>();

  /** La charge à éditer, ou `null` pour une création. */
  expense = input<RecurringExpense | null>(null);

  close = output<void>();
  save = output<RecurringFormValue>();

  private fb = inject(FormBuilder);
  private languageService = inject(LanguageService);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);

  private amountInput = viewChild<AmountInput>('amountInput');

  /**
   * Montant nullable pour que le champ s'ouvre vide — même contournement
   * `p-inputnumber` que transaction-modal-form.ts (§2 : `0` + une frappe donnerait
   * un montant à dix fois trop).
   */
  protected form = this.fb.group({
    label: this.fb.nonNullable.control('', Validators.required),
    type: this.fb.nonNullable.control<TransactionType>('Expense', Validators.required),
    amount: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    frequency: this.fb.nonNullable.control<Frequency>('Monthly', Validators.required),
    categoryId: this.fb.nonNullable.control('', Validators.required),
    nextDueDate: this.fb.nonNullable.control(new Date(), Validators.required),
    active: this.fb.nonNullable.control(true),
  });

  protected isEditing = computed(() => this.expense() !== null);

  /** Le p-inputnumber et le p-datepicker prennent une locale explicite, sinon en-US. */
  protected locale = computed(() => this.languageService.current());
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /**
   * Miroir signal du contrôle `type` — filtre les catégories du select. Le
   * contrôle lui-même est figé (lecture seule) une fois en édition : Type est
   * immuable après création (§ Key Gotchas backend), seul CategoryId reste
   * modifiable.
   */
  protected activeType = toSignal(this.form.controls.type.valueChanges, {
    initialValue: this.form.controls.type.value,
  });

  /** N'affiche une erreur qu'après une tentative de soumission — même raison que
   *  transaction-modal-form.ts : `touched` seul se déclenche sur un simple blur. */
  private submitted = signal(false);
  protected showErrors = this.submitted.asReadonly();

  protected readonly fieldTokens = { paddingY: '0.9rem' };
  protected readonly datePickerTokens = { '--p-inputtext-padding-y': '0.9rem' };

  protected frequencyOptions = computed(() => {
    this.translate.currentLang();
    return FREQUENCIES.map((frequency) => ({
      value: frequency,
      label: this.translate.instant(`recurring.frequency.${frequency.toLowerCase()}`),
    }));
  });

  /** Options du select, restreintes au sens de la charge (§ activeType) et
   *  libellées par le pipe — même règle que transaction-modal-form.ts. */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    return this.categories()
      .filter((categorie) => categorie.type === this.activeType())
      .map((categorie) => ({
        id: categorie.id,
        label: this.categoryNamePipe.transform(categorie),
        color: categorie.color,
        icon: categorie.icon,
      }));
  });

  /** Réinitialise à chaque ouverture, même pattern que transaction-modal-form.ts
   *  (`ngOnChanges` clé sur `visible`, pas un `effect()`). */
  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible()) {
      this.resetFrom(this.expense());
    }
  }

  private resetFrom(expense: RecurringExpense | null) {
    this.submitted.set(false);

    if (!expense) {
      this.form.reset({
        label: '',
        type: 'Expense',
        amount: null,
        frequency: 'Monthly',
        categoryId: '',
        nextDueDate: new Date(),
        active: true,
      });
      return;
    }

    this.form.reset({
      label: expense.label,
      type: expense.type,
      amount: expense.amount,
      frequency: expense.frequency,
      categoryId: expense.categoryId,
      nextDueDate: new Date(expense.nextDueDate),
      active: expense.active,
    });
  }

  selectType(type: TransactionType) {
    if (this.isEditing() || this.form.controls.type.value === type) return;

    // La catégorie retenue appartient à l'autre sens : on la réinitialise.
    this.form.patchValue({ type, categoryId: '' });
  }

  onShow() {
    setTimeout(() => this.amountInput()?.focus());
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

    const { label, type, amount, frequency, categoryId, nextDueDate, active } =
      this.form.getRawValue();

    this.save.emit({
      label,
      type,
      frequency,
      categoryId,
      nextDueDate,
      active,
      // `required` + `min(0.01)` : on n'arrive ici que par un formulaire valide.
      amount: amount ?? 0,
    });
  }
}
