export type Transaction = {
  id: string;
  amount: number;
  date: Date;
  description: string;
  categoryName: string;
  categoryId: number;
  type?: 'income' | 'expense';
};

export type CreateTransactionDto = Omit<Transaction, 'id' | 'type' | 'categoryName'> & {
  categoryId: number;
};
export type UpdateTransactionDto = Partial<CreateTransactionDto>;

export class TransactionParams {
  pageNumber = 1;
  pageSize = 10;
}
