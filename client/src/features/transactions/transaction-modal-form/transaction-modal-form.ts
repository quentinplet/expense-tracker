import { Component, computed, inject, input, model, output } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { RadioButton } from 'primeng/radiobutton';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Button } from 'primeng/button';
import { Categorie } from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { TranslatePipe } from '@ngx-translate/core';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { LanguageService } from '@/core/services/language-service';

@Component({
  selector: 'app-transaction-modal-form',
  imports: [
    Dialog,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    RadioButton,
    InputNumber,
    DatePicker,
    Button,
    InputTextModule,
    ReactiveFormsModule,
    TranslatePipe,
    CategoryNamePipe,
  ],
  templateUrl: './transaction-modal-form.html',
  styleUrl: './transaction-modal-form.scss',
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

  /** Le p-inputnumber et le p-datepicker prennent une locale explicite, sinon en-US. */
  protected locale = computed(() => this.languageService.current());

  /** `dd/mm/yy` en français, `mm/dd/yy` en anglais — le format du p-datepicker
   *  n'est pas déductible de la locale, il faut le lui donner. */
  protected dateFormat = computed(() => (this.locale() === 'fr' ? 'dd/mm/yy' : 'mm/dd/yy'));

  /// L'onglet actif reflète le sens de la transaction en cours d'édition,
  /// pour qu'un revenu ne s'ouvre pas sur l'onglet Expense.
  activeTab = computed<TransactionType>(() => this.form().get('type')?.value ?? 'Expense');

  onTypeTabChange(value: string | number | undefined) {
    if (value === undefined) return;
    const type = value as TransactionType;
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
