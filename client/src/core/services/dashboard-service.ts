import { environment } from '@/environments/environment';
import { DashboardParams, DashboardResponse } from '@/types/dashboard';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getDashboardData(params: DashboardParams) {
    let httpParams = new HttpParams()
      .set('month', params.month.toString())
      .set('year', params.year.toString());
    return this.http.get<DashboardResponse>(`${this.baseUrl}dashboard`, { params: httpParams });
  }
}
