export type Transaction = {
  id: string;
  amount: number;
  date: Date;
  description: string;
  category: string;
  type: 'income' | 'expense';
};

export type CreateTransactionDto = Omit<Transaction, 'id'>;
export type UpdateTransactionDto = Partial<CreateTransactionDto>;

export class TransactionParams {
  pageNumber = 1;
  pageSize = 10;
}
