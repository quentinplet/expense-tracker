import {
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  model,
  OnInit,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { LanguageService } from '@/core/services/language-service';

@Component({
  selector: 'app-transaction-modal-form',
  imports: [
    Dialog,
    InputNumber,
    DatePicker,
    Button,
    Select,
    Textarea,
    InputTextModule,
    ReactiveFormsModule,
    TranslatePipe,
  ],
  templateUrl: './transaction-modal-form.html',
  styleUrl: './transaction-modal-form.scss',
  // Injecté pour construire les options du select : optionLabel prend un nom de
  // champ, pas un pipe, donc les libellés se calculent en TypeScript.
  providers: [CategoryNamePipe],
})
export class TransactionModalForm implements OnInit {
  visible = model.required<boolean>();
  form = input.required<FormGroup>();
  categories = input.required<Categorie[]>();
  errors = input.required<Record<string, string[]>>();
  isEditing = input.required<boolean>();

  close = output<void>();
  save = output<void>();

  private amountInput = viewChild<InputNumber>('amountInput');

  private destroyRef = inject(DestroyRef);
  private languageService = inject(LanguageService);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);

  /** Le p-inputnumber et le p-datepicker prennent une locale explicite, sinon en-US. */
  protected locale = computed(() => this.languageService.current());

  /** `dd/mm/yy` en français, `mm/dd/yy` en anglais — le format du p-datepicker
   *  n'est pas déductible de la locale, il faut le lui donner. */
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /**
   * Le sens de la transaction en cours, pour qu'un revenu ne s'ouvre pas sur « Dépense ».
   *
   * Miroir signal du contrôle `type`, et pas un `computed()` qui lirait
   * `form().get('type')?.value` : la valeur d'un FormControl n'est pas un signal et
   * `form()` ne change jamais d'instance, donc un tel computed n'était jamais
   * invalidé — il restait figé sur « Expense » et le switch ne faisait rien à l'écran.
   *
   * Alimenté par `valueChanges` (cf. ngOnInit) et non à l'ouverture du dialogue :
   * `onShow` n'arrive qu'une fois l'animation d'entrée terminée, on voyait donc le
   * switch s'afficher sur « Dépense » puis basculer sur « Revenu » à l'édition.
   */
  private currentType = signal<TransactionType>('Expense');
  activeType = this.currentType.asReadonly();

  /**
   * Une erreur ne s'affiche qu'une fois la soumission tentée. `touched` seul ne suffit
   * pas : cliquer sur le switch fait perdre le focus au montant, ce qui suffisait à
   * déclencher « Le montant doit être supérieur à 0. » sans que l'utilisateur ait
   * rien validé.
   */
  private submitted = signal(false);
  protected showErrors = computed(() => this.submitted());

  /**
   * Champs un peu plus hauts que la valeur par défaut de PrimeNG, via SES tokens
   * plutôt qu'un utilitaire Tailwind.
   *
   * Tailwind 4 émet ses utilitaires dans `@layer utilities` et PrimeNG injecte son
   * thème hors layer : une règle sans layer bat toujours une règle en layer, quelle
   * que soit la spécificité. Un `h-12` n'avait donc aucun effet et il fallait
   * `h-12!`. Ici on ne combat plus la règle de PrimeNG, on l'alimente — sa propre
   * déclaration lit `var(--p-*-padding-y)`, donc plus besoin d'`!important`.
   *
   * Le montant est exclu : il porte sa propre taille.
   */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  /**
   * Le p-date-picker n'expose pas de token de padding : son champ est un `.p-inputtext`
   * ordinaire. On pose donc la variable de l'inputtext sur l'hôte, d'où elle descend
   * par héritage CSS jusqu'à l'input interne.
   */
  protected readonly datePickerTokens = { '--p-inputtext-padding-y': '0.9rem' };

  /** Séparateur décimal de la locale active : `,` en français, `.` en anglais. */
  private decimalChar = computed(() =>
    new Intl.NumberFormat(this.locale(), { minimumFractionDigits: 1 })
      .format(1.1)
      .replace(/\d/g, ''),
  );

  /**
   * PrimeNG traduit la touche décimale du PAVÉ numérique vers le séparateur de la
   * locale (`event.code === 'NumpadDecimal'`), mais pas le point de la rangée
   * principale : en français il était purement avalé — `4` `.` `5` donnait `45,00 €`.
   * Impossible de saisir des centimes au clavier d'un portable.
   *
   * On rejoue donc la frappe avec la virgule. `onInputKeyPress` lit
   * `event.which || event.keyCode`, d'où les deux propriétés redéfinies : le
   * constructeur de KeyboardEvent ignore ces champs hérités.
   */
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
   * Options du select de catégorie, restreintes au sens courant et libellées par le
   * pipe — la règle « clé de traduction si système, nom sinon » ne vit qu'à un endroit.
   */
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

  ngOnInit() {
    const type = this.form().get('type');
    this.currentType.set(type?.value ?? 'Expense');

    // Le parent patche `type` avant d'ouvrir le dialogue : en suivant le contrôle,
    // le switch est déjà au bon endroit à la première image, sans bascule visible.
    type?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value: TransactionType) => this.currentType.set(value));
  }

  selectType(type: TransactionType) {
    const form = this.form();
    if (form.get('type')?.value === type) return;

    form.patchValue({ type });
    // La catégorie retenue appartient à l'autre sens : on la réinitialise.
    form.patchValue({ categoryId: '' });
  }

  /**
   * Le focus va au montant, le champ que l'on vient saisir (§2).
   *
   * `[autofocus]` sur le p-inputnumber ne tenait pas : le p-dialog pose le focus sur
   * le premier élément focusable — le segment « Dépense » — depuis un `setTimeout`
   * calé sur la durée de transition, donc APRÈS le nôtre. Il nous le reprenait, et
   * ce blur marquait le montant `touched` : « Le montant doit être supérieur à 0. »
   * s'affichait avant toute frappe. D'où `[focusOnShow]="false"` sur le dialogue.
   */
  onShow() {
    this.focusAmount();
  }

  private focusAmount() {
    setTimeout(() => this.amountInput()?.input?.nativeElement.focus());
  }

  onClose() {
    // Désarmé à la FERMETURE, pas dans onShow : celui-ci n'arrive qu'une fois
    // l'animation d'entrée terminée, donc le dialogue se repeignait d'abord avec les
    // erreurs de la session précédente avant de les effacer — d'où le flash.
    this.submitted.set(false);
    this.close.emit();
  }

  onSubmit() {
    if (this.form().invalid) {
      this.submitted.set(true);
      this.form().markAllAsTouched();
      return;
    }
    this.save.emit();
  }
}
