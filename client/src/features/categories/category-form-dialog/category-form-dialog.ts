import { Component, computed, inject, input, model, OnChanges, output, signal, SimpleChanges } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TranslatePipe } from '@ngx-translate/core';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';

/** Palette et icônes contraintes plutôt qu'un colorpicker/upload libres —
 *  §22 Q du gotcha frontend : un hex hors palette casse `color-mix()`
 *  silencieusement, une contrainte côté formulaire évite l'aller-retour
 *  réseau pour rien (le backend valide déjà le format via [RegularExpression]). */
export const CATEGORY_ICONS = [
  'pi-shopping-cart',
  'pi-car',
  'pi-home',
  'pi-heart',
  'pi-ticket',
  'pi-tag',
  'pi-bolt',
  'pi-stopwatch',
  'pi-wallet',
  'pi-briefcase',
  'pi-chart-line',
  'pi-gift',
  'pi-book',
  'pi-globe',
];

export const CATEGORY_COLORS = [
  '#F59E0B',
  '#3B82F6',
  '#8B5CF6',
  '#EF4444',
  '#EC4899',
  '#14B8A6',
  '#F97316',
  '#22C55E',
  '#10B981',
  '#0EA5E9',
  '#6366F1',
  '#A855F7',
  '#6b7280',
  '#84cc16',
];

export type CategoryFormValue = {
  name: string;
  icon: string;
  color: string;
  type: TransactionType;
};

@Component({
  selector: 'app-category-form-dialog',
  imports: [Dialog, Button, InputTextModule, ReactiveFormsModule, TranslatePipe, CategoryBadge],
  templateUrl: './category-form-dialog.html',
  styleUrl: './category-form-dialog.scss',
  // Injecté pour pré-remplir le champ Nom avec le libellé affiché (§10), pas
  // le Name brut — voir resetFrom()/onSubmit() pour la raison.
  providers: [CategoryNamePipe],
})
export class CategoryFormDialog implements OnChanges {
  visible = model.required<boolean>();
  errors = input.required<Record<string, string[]>>();

  /** La catégorie à éditer, ou `null` pour une création. */
  category = input<Categorie | null>(null);

  /** Sens présélectionné à la création — ex. celui de la transaction en cours
   *  de saisie, quand ce dialogue s'ouvre depuis le sélecteur de catégorie
   *  d'un autre formulaire plutôt que depuis l'écran Catégories. */
  initialType = input<TransactionType>('Expense');

  close = output<void>();
  save = output<CategoryFormValue>();

  private fb = inject(FormBuilder);
  private categoryNamePipe = inject(CategoryNamePipe);

  /**
   * Nom brut de la catégorie éditée et libellé affiché au même instant — pour
   * détecter, à la soumission, si le champ Nom a réellement été modifié par
   * l'utilisateur ou seulement pré-rempli. Sans ça, enregistrer sans toucher
   * au Nom renverrait le libellé traduit comme nouveau Name, et le service
   * effacerait TranslationKey en croyant à un renommage (§10 : « on ne
   * traduit jamais un libellé choisi par l'utilisateur » — ce champ pré-rempli
   * n'en est pas un tant qu'il n'a pas été touché).
   */
  private originalName: string | null = null;
  private initialDisplayName: string | null = null;

  protected readonly icons = CATEGORY_ICONS;
  protected readonly colors = CATEGORY_COLORS;

  protected form = this.fb.group({
    name: this.fb.nonNullable.control('', Validators.required),
    icon: this.fb.nonNullable.control(CATEGORY_ICONS[0], Validators.required),
    color: this.fb.nonNullable.control(CATEGORY_COLORS[0], Validators.required),
    type: this.fb.nonNullable.control<TransactionType>('Expense', Validators.required),
  });

  protected isEditing = computed(() => this.category() !== null);

  /** Miroir signal du contrôle `type` — un FormControl n'est pas un signal. */
  protected activeType = toSignal(this.form.controls.type.valueChanges, {
    initialValue: this.form.controls.type.value,
  });

  /** N'affiche une erreur qu'après une tentative de soumission (voir
   *  transaction-modal-form.ts pour la raison : `touched` seul se déclenche
   *  trop tôt, sur un simple blur). */
  private submitted = signal(false);
  protected showErrors = this.submitted.asReadonly();

  /** Mêmes tokens PrimeNG que le dialogue de transaction : Tailwind ne peut
   *  pas battre le thème injecté hors layer (voir transaction-modal-form.ts). */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible()) {
      this.resetFrom(this.category());
    }
  }

  private resetFrom(category: Categorie | null) {
    this.submitted.set(false);
    this.originalName = category?.name ?? null;

    if (!category) {
      this.initialDisplayName = null;
      this.form.reset({
        name: '',
        icon: CATEGORY_ICONS[0],
        color: CATEGORY_COLORS[0],
        type: this.initialType(),
      });
      return;
    }

    // Le champ Nom affiche le libellé résolu (§10), pas le Name brut — sinon
    // une catégorie non renommée ("Food & Groceries") s'ouvrirait en anglais
    // dans un formulaire par ailleurs entièrement en français.
    this.initialDisplayName = this.categoryNamePipe.transform(category);
    this.form.reset({
      name: this.initialDisplayName,
      icon: category.icon ?? CATEGORY_ICONS[0],
      color: category.color ?? CATEGORY_COLORS[0],
      type: category.type,
    });
  }

  selectType(type: TransactionType) {
    this.form.controls.type.setValue(type);
  }

  selectIcon(icon: string) {
    this.form.controls.icon.setValue(icon);
  }

  selectColor(color: string) {
    this.form.controls.color.setValue(color);
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

    const { name, icon, color, type } = this.form.getRawValue();

    // Champ non retouché depuis l'ouverture : on renvoie le Name brut d'origine,
    // pas le libellé traduit affiché — voir le commentaire sur initialDisplayName.
    const submittedName =
      this.isEditing() && name === this.initialDisplayName ? this.originalName! : name;

    this.save.emit({ name: submittedName, icon, color, type });
  }
}
