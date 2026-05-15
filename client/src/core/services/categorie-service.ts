import { environment } from '@/environments/environment.development';
import { Categorie } from '@/types/categorie';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CategorieService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getCategories() {
    return this.http.get<Categorie[]>(this.baseUrl + 'categories');
  }
}
