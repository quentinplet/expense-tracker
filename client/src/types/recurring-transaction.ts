import { TransactionType } from './transaction';

export type Frequency = 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export type RecurringTransaction = {
  id: string;
  label: string;
  amount: number;
  type: TransactionType;
  frequency: Frequency;
  /** "2026-08-16" */
  nextDueDate: string;
  active: boolean;
  categoryId: string;
  categoryName: string;
  categoryTranslationKey?: string | null;
  categoryIcon?: string | null;
  categoryColor?: string | null;
};

export type CreateRecurringTransactionDto = {
  label: string;
  amount: number;
  type: TransactionType;
  frequency: Frequency;
  nextDueDate: string;
  active: boolean;
  categoryId: string;
};

export type UpdateRecurringTransactionDto = CreateRecurringTransactionDto;
