import { AccountService } from '@/core/services/account-service';
import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { BusyService } from '@/core/services/busy-service';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageSwitch } from '@/shared/language-switch/language-switch';
import { ThemeService } from '@/core/services/theme-service';
import { NotificationBell } from '@/shared/components/notification-bell/notification-bell';

@Component({
  selector: 'app-navbar',
  imports: [MenuModule, ProgressSpinnerModule, TranslatePipe, LanguageSwitch, NotificationBell],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  protected accountService = inject(AccountService);
  protected busyService = inject(BusyService);
  protected themeService = inject(ThemeService);
  private translate = inject(TranslateService);
  private router = inject(Router);

  protected initials = computed(() => {
    const user = this.accountService.currentUser();
    if (!user) return '';
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  });

  /**
   * Reconstruit les libellés à chaque changement de langue : un MenuItem PrimeNG
   * est un objet figé, il ne se retraduit pas tout seul.
   */
  protected menuItems = computed<MenuItem[]>(() => {
    this.translate.currentLang();
    return [
      {
        label: this.translate.instant('profileMenu.profile'),
        icon: 'pi pi-user',
        command: () => this.router.navigateByUrl('/settings'),
      },
      { separator: true },
      {
        label: this.translate.instant('profileMenu.logout'),
        icon: 'pi pi-sign-out',
        command: () => this.accountService.logout(),
      },
    ];
  });
}
