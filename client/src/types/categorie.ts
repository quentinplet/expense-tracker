export type Categorie = {
  id: string;
  name: string;
  enabled: boolean;
  type: 'Income' | 'Expense';
};

export type CreateCategorieDto = Omit<Categorie, 'id'>;
export type UpdateCategorieDto = Partial<CreateCategorieDto>;
