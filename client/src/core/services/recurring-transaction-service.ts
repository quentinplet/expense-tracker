import { environment } from '@/environments/environment';
import {
  CreateRecurringTransactionDto,
  RecurringTransaction,
  UpdateRecurringTransactionDto,
} from '@/types/recurring-transaction';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RecurringTransactionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** Déjà triées par NextDueDate croissant côté serveur. */
  getRecurringTransactions(): Observable<RecurringTransaction[]> {
    return this.http.get<RecurringTransaction[]>(`${this.baseUrl}recurring-transactions`);
  }

  createRecurringTransaction(
    recurringTransaction: CreateRecurringTransactionDto,
  ): Observable<RecurringTransaction> {
    return this.http.post<RecurringTransaction>(
      `${this.baseUrl}recurring-transactions`,
      recurringTransaction,
    );
  }

  updateRecurringTransaction(
    id: string,
    recurringTransaction: UpdateRecurringTransactionDto,
  ): Observable<RecurringTransaction> {
    return this.http.put<RecurringTransaction>(
      `${this.baseUrl}recurring-transactions/${id}`,
      recurringTransaction,
    );
  }

  deleteRecurringTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}recurring-transactions/${id}`);
  }
}
