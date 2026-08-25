export interface CategorySummary {
  categoryName: string;
  totalAmount: number;
}

export interface BudgetSummary {
  categoryName: string;
  totalBudget: number;
  totalSpent: number;
  remainingBudget: number;
  isExceeded: boolean;
}

export interface DashboardResponse {
  totalIncome: number;
  totalExpenses: number;
  balance: number;
  numberOfTransactions: number;
  expensesByCategory: CategorySummary[];
  budgetSummaries: BudgetSummary[];
}

export interface DashboardParams {
  month: number;
  year: number;
}

export type WidgetSeverity = 'success' | 'danger' | 'primary' | 'warning' | 'contrast';

export type DataWidget = {
  title: string;
  value: number;
  icon: string;
  severity: WidgetSeverity;
  currency?: boolean;
};
