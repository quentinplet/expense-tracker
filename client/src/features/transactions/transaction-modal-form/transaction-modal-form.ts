import { Component, computed, inject, input, model, output } from '@angular/core';
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
export class TransactionModalForm {
  visible = model.required<boolean>();
  form = input.required<FormGroup>();
  categories = input.required<Categorie[]>();
  errors = input.required<Record<string, string[]>>();
  isEditing = input.required<boolean>();

  close = output<void>();
  save = output<void>();

  private languageService = inject(LanguageService);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);

  /** Le p-inputnumber et le p-datepicker prennent une locale explicite, sinon en-US. */
  protected locale = computed(() => this.languageService.current());

  /** `dd/mm/yy` en français, `mm/dd/yy` en anglais — le format du p-datepicker
   *  n'est pas déductible de la locale, il faut le lui donner. */
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /// Le sens de la transaction en cours d'édition, pour qu'un revenu ne s'ouvre
  /// pas sur « Dépense ».
  activeType = computed<TransactionType>(() => this.form().get('type')?.value ?? 'Expense');

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

  selectType(type: TransactionType) {
    const form = this.form();
    if (form.get('type')?.value === type) return;

    form.patchValue({ type });
    // La catégorie retenue appartient à l'autre sens : on la réinitialise.
    form.patchValue({ categoryId: '' });
  }

  onClose() {
    this.close.emit();
  }

  onSubmit() {
    if (this.form().invalid) {
      this.form().markAllAsTouched();
      return;
    }
    this.save.emit();
  }
}
