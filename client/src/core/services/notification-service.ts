import { environment } from '@/environments/environment';
import { NotificationsResponse } from '@/types/notification';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { withSkipLoading } from '@/core/interceptors/loading-interceptor';

const POLL_INTERVAL_MS = 60_000;
const RECENT_TAKE = 20;

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  unreadCount = signal(0);
  notifications = signal<NotificationsResponse['items']>([]);

  private pollHandle?: ReturnType<typeof setInterval>;

  /** Idempotent : un composant qui se réinitialise (changement de route dans le
   *  layout) ne doit pas empiler un second minuteur. */
  startPolling(): void {
    if (this.pollHandle) return;
    this.refresh();
    this.pollHandle = setInterval(() => this.refresh(), POLL_INTERVAL_MS);
  }

  stopPolling(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
    this.pollHandle = undefined;
  }

  refresh(): void {
    this.http
      .get<NotificationsResponse>(`${this.baseUrl}notifications`, {
        params: new HttpParams().set('take', RECENT_TAKE),
        context: withSkipLoading(),
      })
      .subscribe({
        next: (res) => {
          this.unreadCount.set(res.unreadCount);
          this.notifications.set(res.items);
        },
      });
  }

  markAsRead(id: string): void {
    this.http
      .put<void>(`${this.baseUrl}notifications/${id}/read`, {}, { context: withSkipLoading() })
      .subscribe({
        next: () => this.refresh(),
      });
  }

  markAllAsRead(): void {
    this.http
      .put<void>(`${this.baseUrl}notifications/read-all`, {}, { context: withSkipLoading() })
      .subscribe({
        next: () => this.refresh(),
      });
  }

  delete(id: string): void {
    this.http
      .delete<void>(`${this.baseUrl}notifications/${id}`, { context: withSkipLoading() })
      .subscribe({
        next: () => this.refresh(),
      });
  }
}
