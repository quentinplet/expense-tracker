import { Component, computed, forwardRef, inject, input, viewChild } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { LanguageService } from '@/core/services/language-service';

/**
 * Champ montant en euros, `p-inputnumber` habillé du contournement de saisie déjà mis au
 * point sur les dialogues Transaction et Budget — extrait à leur 2e usage réel plutôt
 * qu'au 3e (règle des trois, §7) : la duplication était déjà verbatim et volontairement
 * signalée comme telle dans les deux fichiers d'origine, pas une simple ressemblance de
 * forme. Voir la note laissée dans current-feature.md lors du Quick-Add Dialog pass.
 */
@Component({
  selector: 'app-amount-input',
  imports: [InputNumber, FormsModule],
  templateUrl: './amount-input.html',
  styleUrl: './amount-input.scss',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => AmountInput), multi: true },
  ],
})
export class AmountInput implements ControlValueAccessor {
  inputId = input<string>();
  placeholder = input('');
  invalid = input(false);
  min = input<number | undefined>(undefined);
  inputStyleClass = input('');

  private languageService = inject(LanguageService);

  /** Le p-inputnumber prend une locale explicite, sinon en-US (§10). */
  protected locale = computed(() => this.languageService.current());

  /** Séparateur décimal de la locale active : `,` en français, `.` en anglais. */
  private decimalChar = computed(() =>
    new Intl.NumberFormat(this.locale(), { minimumFractionDigits: 1 })
      .format(1.1)
      .replace(/\d/g, ''),
  );

  protected value: number | null = null;
  protected disabled = false;

  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  private inputNumber = viewChild<InputNumber>('inputNumberRef');

  writeValue(value: number | null): void {
    this.value = value;
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  protected onModelChange(value: number | null) {
    this.value = value;
    this.onChange(value);
  }

  protected onBlur() {
    this.onTouched();
  }

  /**
   * PrimeNG traduit la touche décimale du PAVÉ numérique vers le séparateur de la locale
   * (`event.code === 'NumpadDecimal'`) mais pas le point de la rangée principale : en
   * français il était avalé, `4` `.` `5` donnant `45,00 €`. On rejoue la frappe avec la
   * virgule ; `onInputKeyPress` lit `event.which || event.keyCode`, d'où les propriétés
   * redéfinies — le constructeur de KeyboardEvent ignore ces champs hérités.
   */
  protected onKeydown(event: KeyboardEvent) {
    const decimal = this.decimalChar();
    if (event.key !== '.' || decimal === '.') return;

    event.preventDefault();
    const replay = new KeyboardEvent('keypress', { bubbles: true, cancelable: true });
    Object.defineProperty(replay, 'which', { get: () => decimal.charCodeAt(0) });
    Object.defineProperty(replay, 'keyCode', { get: () => decimal.charCodeAt(0) });
    (event.target as HTMLInputElement).dispatchEvent(replay);
  }

  /**
   * Sélectionne tout le texte au focus, pour qu'une valeur pré-remplie (le budget démarre
   * à 0 €) se remplace dès la première frappe au lieu de s'y insérer. Sans effet sur un
   * champ vide (la transaction démarre à `null`).
   */
  protected onFocus(event: Event) {
    (event.target as HTMLInputElement).select();
  }

  /** Permet au parent de poser le focus à l'ouverture d'un dialogue (§2). */
  focus() {
    this.inputNumber()?.input?.nativeElement.focus();
  }
}