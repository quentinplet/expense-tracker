import { environment } from '@/environments/environment';
import { DashboardResponse } from '@/types/dashboard';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** @param month `YYYY-MM`. Le serveur ne devine jamais le mois courant. */
  getDashboardData(month: string): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>(`${this.baseUrl}dashboard`, {
      params: new HttpParams().set('month', month),
    });
  }
}
