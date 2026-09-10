import { TransactionType } from './transaction';

export type ColumnMapping = {
  dateColumn: string | null;
  labelColumn: string | null;
  amountColumn: string | null;
  debitColumn: string | null;
  creditColumn: string | null;
  dateFormat: string;
  cultureName: string;
};

/** Renseigné seulement au second appel de preview(), après correction manuelle
 *  d'une détection automatique incomplète — voir ImportService. */
export type ColumnMappingOverride = Partial<
  Pick<
    ColumnMapping,
    'dateColumn' | 'labelColumn' | 'amountColumn' | 'debitColumn' | 'creditColumn' | 'dateFormat' | 'cultureName'
  >
>;

export type ImportPreviewRow = {
  rowNumber: number;
  date: string | null;
  amount: number | null;
  type: TransactionType | null;
  rawLabel: string;
  suggestedLabel: string;
  isDuplicate: boolean;
  hasRecurringConflict: boolean;
  conflictingTransactionId: string | null;
  isValid: boolean;
  parseError: string | null;
};

export type ImportPreview = {
  detectedColumns: string[];
  suggestedMapping: ColumnMapping;
  rows: ImportPreviewRow[];
  totalRows: number;
  duplicateCount: number;
  conflictCount: number;
  invalidCount: number;
};

export type ImportConfirmRow = {
  date: string;
  amount: number;
  type: TransactionType;
  label: string;
  rawLabelForHash: string;
  note?: string | null;
  categoryId: string | null;
};

export type ImportConfirmResult = {
  importBatchId: string;
  insertedCount: number;
  skippedDuplicateCount: number;
};

export type ImportBatch = {
  id: string;
  fileName: string;
  importedAt: string;
  rowCount: number;
  status: string;
};
