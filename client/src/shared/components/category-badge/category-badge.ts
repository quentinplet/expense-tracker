import { Component, input } from '@angular/core';

/**
 * Icône de catégorie sur fond teinté à 15% de sa couleur — pattern repris tel
 * quel des lignes de `transactions.html` et du `p-select` de
 * `transaction-modal-form.html` (§18 : couleur en style inline, jamais en
 * classe générée). Extrait au 3e usage réel (Categories list), suivant la
 * règle des trois (§7).
 */
@Component({
  selector: 'app-category-badge',
  imports: [],
  templateUrl: './category-badge.html',
})
export class CategoryBadge {
  icon = input<string | null | undefined>(null);
  color = input<string | null | undefined>(null);
  size = input<'sm' | 'md'>('md');
}