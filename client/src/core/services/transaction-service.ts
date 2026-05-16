import { environment } from '@/environments/environment';
import { PaginatedResult } from '@/types/pagination';
import {
  CreateTransactionDto,
  Transaction,
  TransactionParams,
  UpdateTransactionDto,
} from '@/types/transaction';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getTransactions(params: TransactionParams): Observable<PaginatedResult<Transaction>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.categoryId) {
      httpParams = httpParams.set('categoryId', params.categoryId.toString());
    }
    if (params.transactionType) {
      httpParams = httpParams.set('transactionType', params.transactionType);
    }
    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }
    if (params.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }

    if (params.sortDirection) {
      httpParams = httpParams.set('sortDirection', params.sortDirection);
    }

    return this.http.get<PaginatedResult<Transaction>>(`${this.baseUrl}transactions`, {
      params: httpParams,
    });
  }

  addNewTransaction(transactionData: CreateTransactionDto): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.baseUrl}transactions`, transactionData);
  }

  updateTransaction(id: string, transactionData: UpdateTransactionDto): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.baseUrl}transactions/${id}`, transactionData);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}transactions/${id}`);
  }

  deleteTransactions(transactions: Transaction[]): Observable<void> {
    const ids = transactions.map((t) => t.id);
    return this.http.request<void>('delete', `${this.baseUrl}transactions`, {
      body: { ids },
    });
  }
}
