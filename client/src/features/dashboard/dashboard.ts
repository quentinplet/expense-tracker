import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DashboardService } from '@/core/services/dashboard-service';
import { DashboardResponse } from '@/types/dashboard';
import { MonthNavigator } from './components/month-navigator/month-navigator';
import { KpiCards } from './components/kpi-cards/kpi-cards';
import { CategoryDonut } from './components/category-donut/category-donut';
import { TrendChart } from './components/trend-chart/trend-chart';
import { RecentTransactions } from './components/recent-transactions/recent-transactions';
import { currentMonthKey, isValidMonthKey, MonthKey, shiftMonth } from './month';

@Component({
  selector: 'app-dashboard',
  imports: [
    TranslatePipe,
    MonthNavigator,
    KpiCards,
    CategoryDonut,
    TrendChart,
    RecentTransactions,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private dashboardService = inject(DashboardService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected loading = signal(false);
  protected failed = signal(false);

  /**
   * Le mois affiché vit dans l'URL : rafraîchir, partager un lien ou revenir en
   * arrière se comportent alors comme l'utilisateur s'y attend.
   */
  private month = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => params.get('month')),
      map((month) => (isValidMonthKey(month) ? month : currentMonthKey())),
    ),
    { initialValue: currentMonthKey() },
  );

  protected currentMonth = computed<MonthKey>(() => this.month());

  protected data = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => params.get('month')),
      map((month) => (isValidMonthKey(month) ? month : currentMonthKey())),
      tap(() => {
        this.loading.set(true);
        this.failed.set(false);
      }),
      switchMap((month) =>
        this.dashboardService.getDashboardData(month).pipe(
          catchError(() => {
            this.failed.set(true);
            return of(null);
          }),
        ),
      ),
      tap(() => this.loading.set(false)),
    ),
    { initialValue: null as DashboardResponse | null },
  );

  /**
   * Deux vides différents : aucune donnée nulle part appelle un message
   * d'accueil, un mois vide alors qu'il y a de l'historique ailleurs appelle
   * seulement « rien enregistré en août ». Montrer l'accueil à quelqu'un qui a
   * simplement changé de mois se lit comme une perte de données.
   */
  protected monthIsEmpty = computed(() => {
    const data = this.data();
    if (!data) return false;
    return data.totals.income === 0 && data.totals.expenses === 0;
  });

  protected hasNoHistory = computed(() => {
    const data = this.data();
    if (!data) return false;
    return this.monthIsEmpty() && data.trend.every((p) => p.income === 0 && p.expenses === 0);
  });

  goToPreviousMonth() {
    this.navigateTo(shiftMonth(this.currentMonth(), -1));
  }

  goToNextMonth() {
    this.navigateTo(shiftMonth(this.currentMonth(), 1));
  }

  private navigateTo(month: MonthKey) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { month },
      queryParamsHandling: 'merge',
    });
  }
}