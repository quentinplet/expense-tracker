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
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { Select, SelectChangeEvent } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { Transaction, TransactionType } from '@/types/transaction';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { AmountInput } from '@/shared/components/amount-input/amount-input';
import { LanguageService } from '@/core/services/language-service';
import { CategoryFormDialog, CategoryFormValue } from '@/features/categories/category-form-dialog/category-form-dialog';

/** Valeur factice utilisée comme id d'option pour la ligne « Ajouter une
 *  catégorie » du sélecteur — jamais envoyée au serveur : interceptée dans
 *  `onCategorySelectChange` avant que la soumission ne puisse la voir. */
const NEW_CATEGORY_OPTION_ID = '__new-category__';

/**
 * Ce que le dialogue remonte : la saisie telle que le formulaire la porte, `date` en
 * `Date`. La traduction vers le contrat de l'API appartient au parent, qui possède déjà
 * `toIsoDate()` pour le filtre de période.
 */
export type TransactionFormValue = {
  label: string;
  note: string;
  type: TransactionType;
  categoryId: string;
  amount: number;
  date: Date;
};

@Component({
  selector: 'app-transaction-modal-form',
  imports: [
    Dialog,
    DatePicker,
    Button,
    Select,
    Textarea,
    InputTextModule,
    ReactiveFormsModule,
    TranslatePipe,
    CategoryBadge,
    AmountInput,
    CategoryFormDialog,
  ],
  templateUrl: './transaction-modal-form.html',
  styleUrl: './transaction-modal-form.scss',
  // Injecté pour construire les options du select : optionLabel prend un nom de
  // champ, pas un pipe, donc les libellés se calculent en TypeScript.
  providers: [CategoryNamePipe],
})
export class TransactionModalForm implements OnChanges {
  visible = model.required<boolean>();
  categories = input.required<Categorie[]>();
  errors = input.required<Record<string, string[]>>();

  /** La transaction à éditer, ou `null` pour une création. */
  transaction = input<Transaction | null>(null);

  close = output<void>();
  save = output<TransactionFormValue>();

  /** Remonte au parent, seul propriétaire de `categories` (§ pattern déjà en
   *  place : ce dialogue ne fait que le lire) — le parent l'ajoute à sa
   *  propre liste, exactement comme `Categories.createCategory()`. */
  categoryCreated = output<Categorie>();

  private fb = inject(FormBuilder);
  private languageService = inject(LanguageService);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);

  private amountInput = viewChild<AmountInput>('amountInput');

  protected readonly newCategoryOptionId = NEW_CATEGORY_OPTION_ID;
  protected categoryFormVisible = signal(false);
  protected categoryFormErrors = signal<Record<string, string[]>>({});

  /**
   * Démarre à 0, pas `null` : un champ vide affiche « 0,00 € » en placeholder, grisé
   * comme un texte d'exemple — trompeur alors que le montant est bien à saisir. Le
   * `(onFocus)` de `AmountInput` (sélection du texte, déjà en place pour Budgets)
   * couvre le risque qui justifiait `null` à l'origine : sans lui, `p-inputnumber`
   * insère les chiffres frappés dans le zéro affiché (« 68 » → 680,00 €). Il reste
   * toujours positif, le sens étant porté par `type` (§3.C).
   */
  protected form = this.fb.group({
    label: this.fb.nonNullable.control('', Validators.required),
    note: this.fb.nonNullable.control(''),
    type: this.fb.nonNullable.control<TransactionType>('Expense', Validators.required),
    categoryId: this.fb.nonNullable.control('', Validators.required),
    amount: this.fb.control<number | null>(0, [Validators.required, Validators.min(0.01)]),
    date: this.fb.nonNullable.control(new Date(), Validators.required),
  });

  protected isEditing = computed(() => this.transaction() !== null);

  /** Le p-inputnumber et le p-datepicker prennent une locale explicite, sinon en-US. */
  protected locale = computed(() => this.languageService.current());

  /** Le format du p-datepicker ne se déduit pas de la locale, il faut le lui donner. */
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /**
   * Miroir signal du contrôle `type`, pour teinter le switch et filtrer les catégories.
   * Un `computed()` ne conviendrait pas : la valeur d'un FormControl n'est pas un signal,
   * rien ne l'invaliderait.
   */
  protected activeType = toSignal(this.form.controls.type.valueChanges, {
    initialValue: this.form.controls.type.value,
  });

  /**
   * Une erreur ne s'affiche qu'une fois la soumission tentée. `touched` ne suffit pas :
   * cliquer sur le switch fait perdre le focus au montant, ce qui suffisait à déclencher
   * « Le montant doit être supérieur à 0. » sans que rien n'ait été validé.
   */
  private submitted = signal(false);
  protected showErrors = this.submitted.asReadonly();

  /**
   * Champs plus hauts via les tokens de PrimeNG plutôt qu'un utilitaire Tailwind :
   * Tailwind 4 émet ses utilitaires dans `@layer utilities` et PrimeNG injecte son thème
   * hors layer, or une règle sans layer bat toujours une règle en layer. `h-12` restait
   * donc sans effet. En alimentant `var(--p-*-padding-y)`, plus besoin d'`!important`.
   *
   * Le montant est exclu : il porte sa propre taille.
   */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  /**
   * Le p-date-picker n'expose pas de token de padding — son champ est un `.p-inputtext`
   * ordinaire. La variable posée sur l'hôte descend par héritage CSS jusqu'à l'input.
   */
  protected readonly datePickerTokens = { '--p-inputtext-padding-y': '0.9rem' };

  /**
   * Réinitialise à chaque ouverture.
   *
   * `ngOnChanges` et non un `effect()` : Angular déconseille les effects pour propager
   * de l'état, et ce hook s'exécute avant que la vue ne soit rendue, là où un effect
   * part de façon asynchrone dans le cycle.
   *
   * Clé sur `visible` et non sur `transaction` : deux créations consécutives passent
   * toutes deux `null`, ce qui ne produit aucun changement. Et le composant n'est jamais
   * recréé, le p-dialog possédant sa visibilité, donc `ngOnInit` ne suffirait pas.
   */
  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible()) {
      this.resetFrom(this.transaction());
    }
  }

  private resetFrom(transaction: Transaction | null) {
    this.submitted.set(false);

    if (!transaction) {
      // Chaque champ explicité, plutôt que de compter sur le rappel implicite d'Angular
      // à la valeur de construction pour les contrôles omis : ce rappel est correct ici
      // (chaque contrôle a été construit vide), mais silencieux pour qui ne le sait pas —
      // un futur contrôle construit avec une valeur non vide s'y ferait piéger.
      this.form.reset({
        label: '',
        note: '',
        type: 'Expense',
        categoryId: '',
        amount: 0,
        date: new Date(),
      });
      return;
    }

    this.form.reset({
      label: transaction.label,
      note: transaction.note ?? '',
      type: transaction.type,
      categoryId: transaction.categoryId,
      amount: transaction.amount,
      date: new Date(transaction.date),
    });
  }

  /**
   * Options du select, restreintes au sens courant et libellées par le pipe — la règle
   * « clé de traduction si système, nom sinon » ne vit qu'à un endroit (§10).
   *
   * La dernière ligne est une option factice, pas une vraie catégorie : elle ouvre le
   * dialogue de création par-dessus celui-ci plutôt que de sélectionner quoi que ce
   * soit (voir `onCategorySelectChange`).
   */
  protected categoryOptions = computed(() => {
    this.translate.currentLang();
    const options = this.categories()
      .filter((categorie) => categorie.type === this.activeType())
      .map((categorie) => ({
        id: categorie.id,
        label: this.categoryNamePipe.transform(categorie),
        color: categorie.color as string | null,
        icon: categorie.icon as string | null,
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
  });

  selectType(type: TransactionType) {
    if (this.form.controls.type.value === type) return;

    // La catégorie retenue appartient à l'autre sens : on la réinitialise.
    this.form.patchValue({ type, categoryId: '' });
  }

  /**
   * L'option « Ajouter une catégorie » ne doit jamais rester sélectionnée — on la
   * remplace par une chaîne vide (comme au premier chargement) plutôt que de
   * restaurer la sélection précédente, pour rester simple : si l'utilisateur
   * annule le dialogue de création, il ne perd rien qu'un reclic.
   */
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
        // Émis avant `setValue` : le parent met à jour `categories` de façon
        // synchrone, donc `categoryOptions()` porte déjà la nouvelle catégorie
        // au moment où le select doit résoudre l'id sélectionné.
        this.categoryCreated.emit(created);
        this.form.controls.categoryId.setValue(created.id);
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

  /**
   * Le focus va au montant, le champ que l'on vient saisir (§2). Le p-dialog le poserait
   * sinon sur le premier élément focusable depuis un `setTimeout` calé sur la durée de
   * transition — donc après le nôtre — et ce blur marquait le montant `touched`. D'où
   * `[focusOnShow]="false"` sur le dialogue.
   */
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

    const { label, note, type, categoryId, amount, date } = this.form.getRawValue();

    this.save.emit({
      label,
      note,
      type,
      categoryId,
      date,
      // `required` + `min(0.01)` : on n'arrive ici que par un formulaire valide.
      amount: amount ?? 0,
    });
  }
}
