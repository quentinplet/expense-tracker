import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { LoginCreds, User } from '../../types/user';
import { tap } from 'rxjs/internal/operators/tap';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  currentUser = signal<User | null>(null);
  private baseUrl = environment.apiUrl + 'account';

  login(creds: LoginCreds) {
    return this.http
      .post<User>(this.baseUrl + '/login', creds)
      .pipe(tap((currentUser) => this.currentUser.set(currentUser)));
  }

  refreshToken() {
    return this.http.post<User>(
      this.baseUrl + 'account/refresh-token',
      {},
      { withCredentials: true },
    );
  }

  startTokenRefreshInterval() {
    setInterval(
      () => {
        this.http
          .post<User>(this.baseUrl + 'account/refresh-token', {}, { withCredentials: true })
          .subscribe({
            next: (user) => {
              if (user) {
                this.setCurrentUser(user);
              }
            },
            error: (err) => {
              console.log('Token refresh failed:', err);
              this.logout();
            },
          });
      },
      5 * 60 * 1000,
    ); // Refresh every 5 minutes
  }

  logout() {
    this.http.post(this.baseUrl + 'account/logout', {}, { withCredentials: true }).subscribe({
      next: () => {
        localStorage.removeItem('filters');
        this.currentUser.set(null);
      },
    });
  }

  setCurrentUser(user: User) {
    user.roles = this.getRolesFromToken(user);
    this.currentUser.set(user);
  }

  private getRolesFromToken(user: User): string[] {
    const payload = user.token.split('.')[1];
    const decoded = atob(payload);
    const jsonPayload = JSON.parse(decoded);
    return Array.isArray(jsonPayload.role) ? jsonPayload.role : [jsonPayload.role];
  }
}
