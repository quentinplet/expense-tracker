import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { ProgressBar } from 'primeng/progressbar';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '@/core/services/language-service';
import { Budget } from '@/types/budget';
import { CategoryBadge } from '@/shared/components/category-badge/category-badge';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';

type BudgetStatus = 'normal' | 'warning' | 'over';

/** Couleur neutre du budget global, indépendante de toute catégorie (§UI budget global). */
const GLOBAL_ACCENT = 'var(--p-primary-500)';

@Component({
  selector: 'app-budget-card',
  imports: [Button, ProgressBar, TranslatePipe, CategoryBadge, CategoryNamePipe, CurrencyPipe, DecimalPipe],
  templateUrl: './budget-card.html',
  styleUrl: './budget-card.scss',
})
export class BudgetCard {
  budget = input.required<Budget>();

  edit = output<Budget>();
  delete = output<Budget>();

  private languageService = inject(LanguageService);
  protected locale = computed(() => this.languageService.current());

  protected isGlobal = computed(() => this.budget().categoryId === null);

  /** Le budget global sans ligne réelle en base — `id` vide, un plafond à 0 en
   *  attendant que l'utilisateur en définisse un (voir budgets.ts). Pas de bouton
   *  de suppression sur cette carte : rien à supprimer tant qu'elle n'existe pas. */
  protected isPlaceholder = computed(() => this.budget().id === '');

  /**
   * Peut dépasser 100 : c'est justement ce que la carte doit montrer sur un budget
   * dépassé. `amountLimit` est normalement toujours > 0 (validé côté serveur), sauf
   * pour le placeholder du budget global — d'où la garde, plutôt qu'une division
   * par zéro produisant `NaN`.
   */
  protected percentage = computed(() => {
    const { spent, amountLimit } = this.budget();
    return amountLimit > 0 ? (spent / amountLimit) * 100 : 0;
  });

  /** La largeur de la barre, elle, se plafonne à 100 — rien au-delà du conteneur. */
  protected barValue = computed(() => Math.min(100, this.percentage()));

  protected status = computed<BudgetStatus>(() => {
    const pct = this.percentage();
    if (pct >= 100) return 'over';
    if (pct >= 90) return 'warning';
    return 'normal';
  });

  protected badgeColor = computed(() =>
    this.isGlobal() ? GLOBAL_ACCENT : (this.budget().categoryColor ?? 'var(--p-surface-400)'),
  );

  /**
   * Une seule teinte, interpolée en continu entre vert (0 %) et rouge (100 % et
   * au-delà) selon la proximité du plafond — pas un dégradé fixe affiché sur toute
   * la barre : plus le pourcentage grimpe, plus la couleur elle-même vire au rouge.
   * `color-mix` fait l'interpolation directement en CSS, même recette que le badge
   * de catégorie (§18). Indépendant de la catégorie : la barre porte la sévérité
   * (dépensé vs plafond), le badge porte l'identité.
   */
  protected barColor = computed(() => {
    const ratio = Math.min(100, Math.max(0, this.percentage()));
    return `color-mix(in oklch, var(--p-red-500) ${ratio}%, var(--p-green-500))`;
  });
}