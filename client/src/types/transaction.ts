export type TransactionType = 'Expense' | 'Income';

export type Transaction = {
  id: string;
  amount: number;
  type: TransactionType;
  date: string;
  label: string;
  note?: string | null;
  categoryName: string;
  categoryTranslationKey?: string | null;
  categoryColor?: string | null;
  categoryIcon?: string | null;
  categoryId: string;
};

export type CreateTransactionDto = {
  amount: number;
  type: TransactionType;
  date: string;
  label: string;
  note?: string | null;
  categoryId: string;
};

export type UpdateTransactionDto = CreateTransactionDto;

export class TransactionParams {
  pageNumber = 1;
  pageSize = 10;

  categoryId?: string;
  transactionType?: string;
  /** Bornes incluses, au format yyyy-MM-dd attendu par la DateOnly de l'API. */
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  sortBy?: string;
  sortDirection: 'asc' | 'desc' = 'desc';
}
