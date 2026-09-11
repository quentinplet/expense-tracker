import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountService } from '@/core/services/account-service';
import { DashboardService } from '@/core/services/dashboard-service';
import { BudgetService } from '@/core/services/budget-service';
import { RecurringTransactionService } from '@/core/services/recurring-transaction-service';
import { LanguageService } from '@/core/services/language-service';
import { DashboardResponse } from '@/types/dashboard';
import { Budget } from '@/types/budget';
import { RecurringTransaction } from '@/types/recurring-transaction';
import { PeriodSelector } from './components/period-selector/period-selector';
import { KpiCards } from './components/kpi-cards/kpi-cards';
import { CategoryDonut } from './components/category-donut/category-donut';
import { TrendChart } from './components/trend-chart/trend-chart';
import { RecentTransactions } from './components/recent-transactions/recent-transactions';
import { QuickActions } from './components/quick-actions/quick-actions';
import { BudgetsSummary } from './components/budgets-summary/budgets-summary';
import { RecurringSummary } from './components/recurring-summary/recurring-summary';
import { currentMonthKey, isValidMonthKey, MonthKey } from './month';

@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe,
    TranslatePipe,
    PeriodSelector,
    KpiCards,
    CategoryDonut,
    TrendChart,
    RecentTransactions,
    QuickActions,
    BudgetsSummary,
    RecurringSummary,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private accountService = inject(AccountService);
  private dashboardService = inject(DashboardService);
  private budgetService = inject(BudgetService);
  private recurringTransactionService = inject(RecurringTransactionService);
  private languageService = inject(LanguageService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected loading = signal(false);
  protected failed = signal(false);

  protected locale = computed(() => this.languageService.current());
  protected today = new Date();

  /** Salutation selon l'heure d'ouverture. Le nom reste vide tant que le profil
   *  n'est pas chargé : « Bonjour,  👋 » vaut mieux qu'un placeholder qui clignote. */
  protected greeting = computed(() => {
    const hour = this.today.getHours();
    const moment = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    return {
      key: `dashboard.greeting.${moment}`,
      name: this.accountService.currentUser()?.firstName ?? '',
    };
  });

  /**
   * Le mois vit dans l'URL : rafraîchir, partager un lien ou revenir en arrière
   * se comportent alors comme l'utilisateur s'y attend. Une valeur invalide retombe
   * sur le mois courant plutôt que de provoquer une erreur.
   *
   * La portée « tout l'historique » n'est délibérément pas ici : elle est propre à
   * chaque widget, et l'encoder dans l'URL reviendrait à traiter un confort de
   * lecture comme un état partageable.
   */
  private month$ = this.route.queryParamMap.pipe(
    map((params) => params.get('month')),
    map((month) => (isValidMonthKey(month) ? month : currentMonthKey())),
  );

  protected currentMonth = toSignal(this.month$, { initialValue: currentMonthKey() });

  protected data = toSignal(
    this.month$.pipe(
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

  /** Suit le mois affiché, comme les KPI/donut/courbe journalière. */
  protected budgets = toSignal(
    this.month$.pipe(
      switchMap((month) =>
        this.budgetService.getBudgets(month).pipe(catchError(() => of([] as Budget[]))),
      ),
    ),
    { initialValue: [] as Budget[] },
  );

  /** Décorrélées de la période affichée, comme les transactions récentes : une
   *  charge récurrente n'appartient pas à un mois précis. */
  protected recurringTransactions = toSignal(
    this.recurringTransactionService
      .getRecurringTransactions()
      .pipe(catchError(() => of([] as RecurringTransaction[]))),
    { initialValue: [] as RecurringTransaction[] },
  );

  setMonth(month: MonthKey) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { month },
      queryParamsHandling: 'merge',
    });
  }
}
