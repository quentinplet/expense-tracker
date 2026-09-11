import { Component, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { UIChart } from 'primeng/chart';
import { LanguageService } from '@/core/services/language-service';
import { ThemeService } from '@/core/services/theme-service';
import { DailyPoint, MonthlyPoint } from '@/types/dashboard';
import { dayKeyToDate, monthKeyToDate, Scope } from '../../month';

const INCOME_COLOR = '#22c55e';
const EXPENSE_COLOR = '#ef4444';

/** Même convention que ThemeService/LanguageService : `app.<nom>`. */
const SCOPE_STORAGE_KEY = 'app.dashboardTrendScope';

/** Le tableau `trend` couvre déjà tout l'historique borné (12 à 120 mois) : la
 *  portée « Année » se contente d'en garder les douze derniers points. */
const ROLLING_YEAR_MONTHS = 12;

function readStoredScope(fallback: Scope): Scope {
  try {
    const stored = localStorage.getItem(SCOPE_STORAGE_KEY);
    if (stored === 'month' || stored === 'all') return stored;
  } catch {
    // Stockage indisponible (navigation privée…) : on retombe sur le défaut.
  }
  return fallback;
}

@Component({
  selector: 'app-trend-chart',
  imports: [UIChart, TranslatePipe],
  templateUrl: './trend-chart.html',
  styleUrl: './trend-chart.scss',
})
export class TrendChart {
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);
  private themeService = inject(ThemeService);

  /** Un point par jour du mois affiché. */
  dailyTrend = input.required<DailyPoint[]>();

  /** Un point par mois, sur tout l'historique jusqu'au mois affiché. */
  trend = input.required<MonthlyPoint[]>();

  /**
   * Réglage local au widget, et non porté par l'URL : c'est un confort de lecture,
   * pas un état qu'on partage ou sur lequel on revient avec le bouton précédent.
   * Persisté en localStorage (par navigateur, pas par lien) pour survivre à un
   * rechargement — voir `setScope`.
   *
   * La portée change la granularité, pas seulement la fenêtre : « Mois » montre les
   * jours du mois affiché, « Année » les douze derniers mois glissants jusqu'au
   * mois courant — sans se laisser tronquer quand le picker recule.
   */
  protected scope = signal<Scope>(readStoredScope('month'));

  protected setScope(value: Scope): void {
    this.scope.set(value);
    try {
      localStorage.setItem(SCOPE_STORAGE_KEY, value);
    } catch {
      // Stockage indisponible : le choix vaut pour la session en cours.
    }
  }

  /** Étiquette et valeurs, la granularité étant déjà résolue. */
  protected points = computed<{ label: Date; expenses: number; income: number }[]>(() =>
    this.scope() === 'all'
      ? this.trend()
          .slice(-ROLLING_YEAR_MONTHS)
          .map((p) => ({ ...p, label: monthKeyToDate(p.month) }))
      : this.dailyTrend().map((p) => ({ ...p, label: dayKeyToDate(p.date) })),
  );

  /** Les étiquettes sont dans le canvas : elles ne se retraduisent pas
   *  toutes seules, il faut reconstruire les données au changement de langue. */
  protected chartData = computed(() => {
    const locale = this.languageService.current();
    const points = this.points();
    const format: Intl.DateTimeFormatOptions =
      this.scope() === 'all' ? { month: 'short' } : { day: 'numeric', month: 'short' };

    return {
      labels: points.map((p) => p.label.toLocaleDateString(locale, format)),
      datasets: [
        {
          label: this.translate.instant('dashboard.trend.income'),
          data: points.map((p) => p.income),
          borderColor: INCOME_COLOR,
          backgroundColor: 'rgba(34, 197, 94, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 0,
          pointHoverRadius: 4,
          borderWidth: 2,
        },
        {
          label: this.translate.instant('dashboard.trend.expenses'),
          data: points.map((p) => p.expenses),
          borderColor: EXPENSE_COLOR,
          backgroundColor: 'rgba(239, 68, 68, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 0,
          pointHoverRadius: 4,
          borderWidth: 2,
        },
      ],
    };
  });

  /**
   * Chart.js ignore le thème : ses axes et ses grilles gardent la couleur figée
   * au moment du rendu. On relit donc les variables CSS à chaque bascule, sinon
   * le graphique conserve du texte clair sur fond clair.
   */
  protected chartOptions = computed(() => {
    this.themeService.current();

    const styles = getComputedStyle(document.documentElement);
    const textColor = styles.getPropertyValue('--p-text-muted-color').trim() || '#94a3b8';
    const gridColor = styles.getPropertyValue('--p-content-border-color').trim() || '#334155';

    return {
      maintainAspectRatio: false,
      interaction: { mode: 'index' as const, intersect: false },
      plugins: { legend: { display: false } },
      scales: {
        x: {
          // Trente-et-un jours, ou dix ans de mois : sans plafond les étiquettes
          // se chevauchent jusqu'à devenir illisibles.
          ticks: { color: textColor, autoSkip: true, maxTicksLimit: 8, maxRotation: 0 },
          grid: { display: false },
          border: { color: gridColor },
        },
        y: {
          ticks: { color: textColor },
          grid: { color: gridColor },
          border: { display: false },
        },
      },
    };
  });

  protected readonly incomeColor = INCOME_COLOR;
  protected readonly expenseColor = EXPENSE_COLOR;
}
