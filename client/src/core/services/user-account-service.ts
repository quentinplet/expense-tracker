import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@/environments/environment';
import {
  ChangeEmailDto,
  ChangeNameDto,
  ChangePasswordDto,
  DeleteAccountDto,
  User,
} from '@/types/user';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class UserAccountService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  changeName(dto: ChangeNameDto): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}users/me/name`, dto);
  }

  changePassword(dto: ChangePasswordDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}users/me/password`, dto);
  }

  changeEmail(dto: ChangeEmailDto): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}users/me/email`, dto);
  }

  deleteAccount(dto: DeleteAccountDto): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}users/me`, { body: dto });
  }
}
