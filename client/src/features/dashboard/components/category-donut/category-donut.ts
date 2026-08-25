import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { UIChart } from 'primeng/chart';
import { LanguageService } from '@/core/services/language-service';
import { CategoryBreakdown } from '@/types/dashboard';

/** Au-delà, la légende devient plus haute que le graphique. */
const MAX_SLICES = 6;
const OTHER_COLOR = '#6b7280';

type Slice = {
  key: string;
  label: string;
  color: string;
  total: number;
  share: number;
};

@Component({
  selector: 'app-category-donut',
  imports: [UIChart, CurrencyPipe, DecimalPipe, TranslatePipe],
  templateUrl: './category-donut.html',
  styleUrl: './category-donut.scss',
})
export class CategoryDonut {
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);

  breakdown = input.required<CategoryBreakdown[]>();
  monthExpenses = input.required<number>();

  protected locale = computed(() => this.languageService.current());

  /**
   * Les catégories arrivent triées par montant décroissant. On garde les
   * premières et on agrège la queue, sinon vingt catégories produisent une
   * légende illisible.
   */
  protected slices = computed<Slice[]>(() => {
    // Lecture de la langue : les libellés doivent suivre un changement à chaud.
    this.translate.currentLang();

    const items = this.breakdown();
    const head = items.slice(0, MAX_SLICES);
    const tail = items.slice(MAX_SLICES);

    const slices: Slice[] = head.map((c) => ({
      key: c.categoryId,
      label: this.displayName(c),
      color: c.color || OTHER_COLOR,
      total: c.total,
      share: c.share,
    }));

    if (tail.length) {
      slices.push({
        key: 'other',
        label: this.translate.instant('dashboard.breakdown.other'),
        color: OTHER_COLOR,
        total: tail.reduce((sum, c) => sum + c.total, 0),
        share: tail.reduce((sum, c) => sum + c.share, 0),
      });
    }

    return slices;
  });

  /**
   * Le graphique ne porte que les arcs : la légende est du HTML à côté. Chart.js
   * ne connaît ni le thème ni la langue, donc tout ce qui est texte reste hors
   * du canvas et suit Angular naturellement.
   */
  protected chartData = computed(() => {
    const slices = this.slices();
    return {
      labels: slices.map((s) => s.label),
      datasets: [
        {
          data: slices.map((s) => s.total),
          backgroundColor: slices.map((s) => s.color),
          borderWidth: 0,
          hoverOffset: 4,
        },
      ],
    };
  });

  protected chartOptions = {
    cutout: '68%',
    plugins: { legend: { display: false }, tooltip: { enabled: false } },
    maintainAspectRatio: false,
  };

  /** La règle du projet, en un seul endroit : jamais traduire un libellé saisi. */
  private displayName(category: CategoryBreakdown): string {
    if (!category.translationKey) return category.name;

    const translated = this.translate.instant(category.translationKey);
    return translated === category.translationKey ? category.name : translated;
  }
}