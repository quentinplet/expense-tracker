import { environment } from '@/environments/environment';
import {
  CategoryDeletedDto,
  Categorie,
  CreateCategorieDto,
  UpdateCategorieDto,
} from '@/types/categorie';
import { TransactionType } from '@/types/transaction';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class CategorieService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getCategories(type?: TransactionType): Observable<Categorie[]> {
    const params = type ? new HttpParams().set('type', type) : undefined;
    return this.http.get<Categorie[]>(`${this.baseUrl}categories`, { params });
  }

  createCategory(categoryData: CreateCategorieDto): Observable<Categorie> {
    return this.http.post<Categorie>(`${this.baseUrl}categories`, categoryData);
  }

  updateCategory(id: string, categoryData: UpdateCategorieDto): Observable<Categorie> {
    return this.http.put<Categorie>(`${this.baseUrl}categories/${id}`, categoryData);
  }

  deleteCategory(id: string): Observable<CategoryDeletedDto> {
    return this.http.delete<CategoryDeletedDto>(`${this.baseUrl}categories/${id}`);
  }
}