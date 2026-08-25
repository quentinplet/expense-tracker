import { AccountService } from '@/core/services/account-service';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { StatsWidget } from './stats-widget/stats-widget';
import { DashboardService } from '@/core/services/dashboard-service';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { DataWidget, WidgetSeverity } from '@/types/dashboard';

@Component({
  selector: 'app-dashboard',
  imports: [StatsWidget, Select, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private dashboardService = inject(DashboardService);

  protected monthOptions = Array.from({ length: 12 }, (_, i) => {
    const date = new Date();
    date.setMonth(date.getMonth() + -i);
    return {
      label: date.toLocaleString('default', { month: 'long', year: 'numeric' }),
      month: date.getMonth() + 1,
      year: date.getFullYear(),
    };
  });

  protected selectedPeriod = signal(this.monthOptions[0]);

  protected dashboardData = toSignal(
    toObservable(this.selectedPeriod).pipe(
      switchMap(({ month, year }) => this.dashboardService.getDashboardData({ month, year })),
    ),
  );

  protected dataWidgets: () => DataWidget[] = computed(() => {
    const data = this.dashboardData();

    if (!data) return [];

    return [
      {
        title: 'Total Expenses',
        value: data.totalExpenses,
        icon: 'pi pi-arrow-down',
        severity: 'danger',
        currency: true,
      },
      {
        title: 'Total Income',
        value: data.totalIncome,
        icon: 'pi pi-arrow-up',
        severity: 'success',
        currency: true,
      },
      {
        title: 'Balance',
        value: data.balance,
        icon: 'pi pi-wallet',
        severity: 'contrast',
        currency: true,
      },
      {
        title: 'Transactions',
        value: data.numberOfTransactions,
        icon: 'pi pi-list',
        severity: 'primary',
      },
    ];
  });

  constructor() {
    effect(() => {
      console.log('Dashboard data updated:', this.dashboardData());
    });
  }
}
