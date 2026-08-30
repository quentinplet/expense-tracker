export type Budget = {
  id: string;
  /** "2026-08" */
  month: string;
  /** Null pour un budget global — tous les champs Category* le sont alors aussi. */
  categoryId: string | null;
  categoryName: string | null;
  categoryTranslationKey?: string | null;
  categoryIcon: string | null;
  categoryColor: string | null;
  amountLimit: number;
  autoRenew: boolean;
  /** True si autoRenew est actif ET qu'un budget existe déjà le mois suivant pour la
   *  même cible : la duplication automatique n'aura alors aucun effet. */
  autoRenewConflict: boolean;
  spent: number;
  /** amountLimit - spent, peut être négatif. */
  remaining: number;
};

export type CreateBudgetDto = {
  /** Null == budget global. Ignoré par le serveur au PUT (immuable après création). */
  categoryId: string | null;
  month: string;
  amountLimit: number;
  autoRenew: boolean;
};

export type UpdateBudgetDto = CreateBudgetDto;