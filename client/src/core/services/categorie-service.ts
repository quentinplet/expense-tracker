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
import { map, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CategorieService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** « Other » (`isLocked`) toujours en dernier, quel que soit l'écran qui affiche la
   *  liste — repli permanent, il n'a pas sa place mélangé aux catégories du dessus. Tri
   *  stable : ne change rien à l'ordre relatif du reste. */
  getCategories(type?: TransactionType): Observable<Categorie[]> {
    const params = type ? new HttpParams().set('type', type) : undefined;
    return this.http
      .get<Categorie[]>(`${this.baseUrl}categories`, { params })
      .pipe(map((categories) => [...categories].sort((a, b) => Number(a.isLocked) - Number(b.isLocked))));
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