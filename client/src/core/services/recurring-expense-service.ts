import { environment } from '@/environments/environment';
import {
  CreateRecurringExpenseDto,
  RecurringExpense,
  UpdateRecurringExpenseDto,
} from '@/types/recurring-expense';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RecurringExpenseService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** Déjà triées par NextDueDate croissant côté serveur. */
  getRecurringExpenses(): Observable<RecurringExpense[]> {
    return this.http.get<RecurringExpense[]>(`${this.baseUrl}recurring-expenses`);
  }

  createRecurringExpense(expense: CreateRecurringExpenseDto): Observable<RecurringExpense> {
    return this.http.post<RecurringExpense>(`${this.baseUrl}recurring-expenses`, expense);
  }

  updateRecurringExpense(
    id: string,
    expense: UpdateRecurringExpenseDto,
  ): Observable<RecurringExpense> {
    return this.http.put<RecurringExpense>(`${this.baseUrl}recurring-expenses/${id}`, expense);
  }

  deleteRecurringExpense(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}recurring-expenses/${id}`);
  }
}
