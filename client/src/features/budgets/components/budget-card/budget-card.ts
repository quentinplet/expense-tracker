import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { ProgressBar } from 'primeng/progressbar';
import { TooltipModule } from 'primeng/tooltip';
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
  imports: [
    Button,
    ProgressBar,
    TooltipModule,
    TranslatePipe,
    CategoryBadge,
    CategoryNamePipe,
    CurrencyPipe,
    DecimalPipe,
  ],
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
   * Plafonné à 100 — au-delà, le nombre ne raconte plus rien d'utile (un budget à
   * 1 € dépensé de 50 € donnerait « 5000 % », sans commune mesure avec un budget à
   * 0 € dépensé qui, lui, tombe pile sur 100 %). Le vrai dépassement reste lisible
   * via les montants en euros juste en dessous ; ce nombre-ci n'est qu'une jauge.
   * `amountLimit` peut valoir 0 sur un vrai budget (pas seulement le placeholder
   * du budget global) — sans dépense, 0 % ; avec la moindre dépense contre un
   * plafond nul, 100 % (dépassé d'office, rien à diviser).
   */
  protected percentage = computed(() => {
    const { spent, amountLimit } = this.budget();
    if (amountLimit <= 0) return spent > 0 ? 100 : 0;
    return Math.min(100, (spent / amountLimit) * 100);
  });

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
  protected barColor = computed(
    () => `color-mix(in oklch, var(--p-red-500) ${this.percentage()}%, var(--p-green-500))`,
  );
}