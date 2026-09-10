import { environment } from '@/environments/environment';
import {
  ColumnMappingOverride,
  ImportBatch,
  ImportConfirmResult,
  ImportConfirmRow,
  ImportPreview,
} from '@/types/import';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ImportService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** N'écrit rien en base — le fichier est ré-envoyé une seconde fois avec `mapping`
   *  seulement si l'utilisateur corrige une détection automatique incomplète. */
  preview(file: File, mapping?: ColumnMappingOverride): Observable<ImportPreview> {
    const formData = new FormData();
    formData.append('file', file);
    if (mapping?.dateColumn) formData.append('dateColumn', mapping.dateColumn);
    if (mapping?.labelColumn) formData.append('labelColumn', mapping.labelColumn);
    if (mapping?.amountColumn) formData.append('amountColumn', mapping.amountColumn);
    if (mapping?.debitColumn) formData.append('debitColumn', mapping.debitColumn);
    if (mapping?.creditColumn) formData.append('creditColumn', mapping.creditColumn);
    if (mapping?.dateFormat) formData.append('dateFormat', mapping.dateFormat);
    if (mapping?.cultureName) formData.append('cultureName', mapping.cultureName);

    return this.http.post<ImportPreview>(`${this.baseUrl}import/csv`, formData);
  }

  /** Le fichier original n'est jamais ré-uploadé ici — seulement les lignes déjà
   *  parsées par preview(), potentiellement éditées côté client. */
  confirm(fileName: string, rows: ImportConfirmRow[]): Observable<ImportConfirmResult> {
    return this.http.post<ImportConfirmResult>(`${this.baseUrl}import/csv/confirm`, { fileName, rows });
  }

  getBatches(): Observable<ImportBatch[]> {
    return this.http.get<ImportBatch[]>(`${this.baseUrl}import/batches`);
  }

  deleteBatch(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}import/batches/${id}`);
  }
}
