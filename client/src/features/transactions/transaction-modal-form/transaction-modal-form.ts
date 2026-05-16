import { Component, computed, input, model, output } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { RadioButton } from 'primeng/radiobutton';
import { InputNumber } from 'primeng/inputnumber';
import { DatePicker } from 'primeng/datepicker';
import { Button } from 'primeng/button';
import { Categorie } from '@/types/categorie';
import { Transaction } from '@/types/transaction';
import { Form, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';

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

  expenseCategories = computed(() => this.categories().filter((cat) => cat.type === 'Expense'));
  incomeCategories = computed(() => this.categories().filter((cat) => cat.type === 'Income'));

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
