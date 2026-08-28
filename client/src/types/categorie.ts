import { TransactionType } from './transaction';

export type Categorie = {
  id: string;
  name: string;
  enabled: boolean;
  type: TransactionType;
  translationKey?: string | null;
  icon?: string | null;
  color?: string | null;
  isLocked: boolean;
};

export type CreateCategorieDto = {
  name: string;
  icon: string;
  color: string;
  type: TransactionType;
};

export type UpdateCategorieDto = CreateCategorieDto;

export type CategoryDeletedDto = {
  reassignedTransactionCount: number;
};