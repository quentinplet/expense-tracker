import { TransactionType } from './transaction';

export type Categorie = {
  id: string;
  name: string;
  enabled: boolean;
  type: TransactionType;
  translationKey?: string | null;
  icon?: string | null;
  color?: string | null;
  isSystem: boolean;
};

export type CreateCategorieDto = Omit<Categorie, 'id' | 'isSystem' | 'translationKey'>;
export type UpdateCategorieDto = Partial<CreateCategorieDto>;
