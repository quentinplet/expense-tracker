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

export type DailyPoint = {
  /** "2026-08-14" */
  date: string;
  expenses: number;
  income: number;
};

export type DashboardResponse = {
  /** `YYYY-MM` du mois affiché. */
  month: string;
  totals: MonthTotals;
  /** Σ revenus − Σ dépenses sur tout l'historique. Ce n'est pas un solde. */
  cumulativeNet: number;
  /** Répartition du mois affiché ; `breakdownYear` couvre les douze derniers mois
   *  glissants. Les deux voyagent ensemble pour que la bascule du donut soit
   *  instantanée. */
  breakdown: CategoryBreakdown[];
  breakdownYear: CategoryBreakdown[];
  yearExpenses: number;
  /** Un point par jour du mois affiché ; `trend` porte la vue mensuelle sur tout
   *  l'historique. La courbe choisit l'une ou l'autre selon sa portée. */
  dailyTrend: DailyPoint[];
  trend: MonthlyPoint[];
  recentTransactions: Transaction[];
};
