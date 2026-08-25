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

@Component({
  selector: 'app-navbar',
  imports: [MenuModule, ProgressSpinnerModule, TranslatePipe, LanguageSwitch],
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
    const name = this.accountService.currentUser()?.userName ?? '';
    return name.slice(0, 2).toUpperCase();
  });

  /**
   * Reconstruit les libellés à chaque changement de langue : un MenuItem PrimeNG
   * est un objet figé, il ne se retraduit pas tout seul.
   */
  protected menuItems = computed<MenuItem[]>(() => {
    this.translate.currentLang();
    return [
      {
        label: this.translate.instant('topbar.profile'),
        icon: 'pi pi-user',
        command: () => this.router.navigateByUrl('/profile'),
      },
      { separator: true },
      {
        label: this.translate.instant('topbar.logout'),
        icon: 'pi pi-sign-out',
        command: () => this.accountService.logout(),
      },
    ];
  });
}