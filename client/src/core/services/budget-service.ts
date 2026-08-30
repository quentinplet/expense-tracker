import { environment } from '@/environments/environment';
import { Budget, CreateBudgetDto, UpdateBudgetDto } from '@/types/budget';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class BudgetService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** @param month `YYYY-MM`. Le serveur ne devine jamais le mois courant. */
  getBudgets(month: string): Observable<Budget[]> {
    return this.http.get<Budget[]>(`${this.baseUrl}budgets`, {
      params: new HttpParams().set('month', month),
    });
  }

  createBudget(budget: CreateBudgetDto): Observable<Budget> {
    return this.http.post<Budget>(`${this.baseUrl}budgets`, budget);
  }

  updateBudget(id: string, budget: UpdateBudgetDto): Observable<Budget> {
    return this.http.put<Budget>(`${this.baseUrl}budgets/${id}`, budget);
  }

  deleteBudget(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}budgets/${id}`);
  }
}