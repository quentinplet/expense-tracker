import { WidgetSeverity } from '@/types/dashboard';
import { CurrencyPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-stats-widget',
  imports: [CurrencyPipe],
  templateUrl: './stats-widget.html',
  styleUrl: './stats-widget.scss',
})
export class StatsWidget {
  title = input<string>();
  value = input<number>();
  icon = input<string>();
  currency = input<boolean>();

  severity = input<WidgetSeverity>('primary');

  protected severityClasses: Record<WidgetSeverity, string> = {
    success: 'bg-green-100 dark:bg-green-400/10 text-green-500',
    danger: 'bg-red-100 dark:bg-red-400/10 text-red-500',
    warning: 'bg-yellow-100 dark:bg-yellow-400/10 text-yellow-500',
    primary: 'bg-blue-100 dark:bg-blue-400/10 text-blue-500',
    contrast: 'bg-surface-100 dark:bg-surface-700 text-surface-900 dark:text-surface-0',
  };

  // Les classes ont été mises à jour pour intégrer un dégradé subtil (Light et Dark mode)
  protected cardClasses: Record<WidgetSeverity, string> = {
    success:
      'bg-gradient-to-br from-green-100 to-green-300 dark:from-green-900/30 dark:to-green-950/10',
    danger: 'bg-gradient-to-br from-red-100 to-red-300 dark:from-red-900/30 dark:to-red-950/10',
    warning:
      'bg-gradient-to-br from-yellow-100 to-yellow-300 dark:from-yellow-900/30 dark:to-yellow-950/10',
    primary:
      'bg-gradient-to-br from-blue-100 to-blue-300 dark:from-blue-900/30 dark:to-blue-950/10',
    contrast:
      'bg-gradient-to-br from-surface-100 to-surface-300 dark:from-surface-700 dark:to-surface-900',
  };

  protected iconContainerClass = computed(() => this.severityClasses[this.severity()]);
  protected cardClass = computed(() => this.cardClasses[this.severity()]);
}
