export type NotificationType = 'RecurringTransactionGenerated' | 'BudgetThresholdReached';

export type Notification = {
  id: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;

  /** Renseignés si type === 'RecurringTransactionGenerated'. */
  transactionId?: string | null;
  transactionLabel?: string | null;
  transactionAmount?: number | null;

  /** Renseignés si type === 'BudgetThresholdReached'. categoryName/categoryTranslationKey
   *  restent null pour un budget global (isGlobal === true), même convention que Budget. */
  budgetId?: string | null;
  budgetCategoryName?: string | null;
  budgetCategoryTranslationKey?: string | null;
  budgetIsGlobal?: boolean | null;
  /** "2026-08" */
  budgetMonth?: string | null;
  thresholdPercent?: number | null;
};

export type NotificationsResponse = {
  unreadCount: number;
  items: Notification[];
};
