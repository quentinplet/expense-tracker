import {
  Component,
  computed,
  inject,
  input,
  OnChanges,
  output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TranslatePipe } from '@ngx-translate/core';
import { ColumnMapping, ColumnMappingOverride } from '@/types/import';

type AmountMode = 'signed' | 'debitCredit';

/**
 * Étape de mapping manuel — affichée seulement si la détection automatique du
 * backend n'a pas résolu une colonne obligatoire (§ ImportWizard.needsManualMapping).
 * Repart toujours de `suggested`, pré-remplie par ce que le backend a déjà trouvé.
 */
@Component({
  selector: 'app-import-column-mapper',
  imports: [ReactiveFormsModule, Select, Button, InputTextModule, TranslatePipe],
  templateUrl: './column-mapper.html',
})
export class ColumnMapper implements OnChanges {
  columns = input.required<string[]>();
  suggested = input.required<ColumnMapping>();
  loading = input(false);

  submitMapping = output<ColumnMappingOverride>();
  back = output<void>();

  private fb = inject(FormBuilder);

  protected form = this.fb.nonNullable.group({
    dateColumn: [''],
    labelColumn: [''],
    amountMode: ['signed' as AmountMode],
    amountColumn: [''],
    debitColumn: [''],
    creditColumn: [''],
    dateFormat: ['dd/MM/yyyy'],
    cultureName: ['fr-FR'],
  });

  protected submitted = signal(false);

  protected columnOptions = computed(() => this.columns().map((c) => ({ label: c, value: c })));

  protected amountModeOptions = [
    { labelKey: 'import.mapping.amountModeSigned', value: 'signed' as AmountMode },
    { labelKey: 'import.mapping.amountModeDebitCredit', value: 'debitCredit' as AmountMode },
  ];

  ngOnChanges(changes: SimpleChanges) {
    if (!changes['suggested']) return;

    const s = this.suggested();
    this.form.reset({
      dateColumn: s.dateColumn ?? '',
      labelColumn: s.labelColumn ?? '',
      amountMode: s.debitColumn || s.creditColumn ? 'debitCredit' : 'signed',
      amountColumn: s.amountColumn ?? '',
      debitColumn: s.debitColumn ?? '',
      creditColumn: s.creditColumn ?? '',
      dateFormat: s.dateFormat || 'dd/MM/yyyy',
      cultureName: s.cultureName || 'fr-FR',
    });
    this.submitted.set(false);
  }

  protected get isDebitCredit() {
    return this.form.controls.amountMode.value === 'debitCredit';
  }

  protected get amountIncomplete(): boolean {
    const v = this.form.getRawValue();
    return v.amountMode === 'debitCredit' ? !v.debitColumn && !v.creditColumn : !v.amountColumn;
  }

  onSubmit() {
    this.submitted.set(true);
    const v = this.form.getRawValue();
    if (!v.dateColumn || !v.labelColumn || this.amountIncomplete) return;

    this.submitMapping.emit({
      dateColumn: v.dateColumn,
      labelColumn: v.labelColumn,
      amountColumn: v.amountMode === 'signed' ? v.amountColumn : null,
      debitColumn: v.amountMode === 'debitCredit' ? v.debitColumn : null,
      creditColumn: v.amountMode === 'debitCredit' ? v.creditColumn : null,
      dateFormat: v.dateFormat,
      cultureName: v.cultureName,
    });
  }
}
