import { environment } from '@/environments/environment';
import { PaginatedResult } from '@/types/pagination';
import { Transaction } from '@/types/transaction';
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
}
