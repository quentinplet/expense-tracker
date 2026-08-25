import { Transaction } from './transaction';

export type MonthTotals = {
  expenses: number;
  income: number;
  net: number;
  previousExpenses: number;
  previousIncome: number;
  previousNet: number;
};

export type CategoryBreakdown = {
  categoryId: string;
  name: string;
  translationKey?: string | null;
  color?: string | null;
  icon?: string | null;
  total: number;
  /** Part du total des dépenses du mois, entre 0 et 1. */
  share: number;
};

export type MonthlyPoint = {
  /** "2026-08" */
  month: string;
  expenses: number;
  income: number;
};

export type DashboardResponse = {
  month: string;
  totals: MonthTotals;
  breakdown: CategoryBreakdown[];
  trend: MonthlyPoint[];
  recentTransactions: Transaction[];
};