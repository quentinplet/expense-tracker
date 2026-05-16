import { environment } from '@/environments/environment';
import { PaginatedResult } from '@/types/pagination';
import { CreateTransactionDto, Transaction, UpdateTransactionDto } from '@/types/transaction';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getTransactions(
    pageNumber: number = 1,
    pageSize: number = 10,
  ): Observable<PaginatedResult<Transaction>> {
    return this.http.get<PaginatedResult<Transaction>>(`${this.baseUrl}transactions`, {
      params: {
        pageNumber: pageNumber.toString(),
        pageSize: pageSize.toString(),
      },
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
