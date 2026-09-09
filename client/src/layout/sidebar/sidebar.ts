import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AccountService } from '@/core/services/account-service';

type NavItem = {
  labelKey: string;
  icon: string;
  route: string;
  /** Les écrans non encore livrés restent visibles mais inertes : un lien vers
   *  une route inexistante renvoie sur la page 404, ce qui ressemble à une panne. */
  available: boolean;
};

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, MenuModule],
  templateUrl: './sidebar.html',
})
export class SidebarComponent {
  protected accountService = inject(AccountService);
  private translate = inject(TranslateService);
  private router = inject(Router);

  protected readonly menuItems: NavItem[] = [
    { labelKey: 'nav.dashboard', icon: 'pi pi-th-large', route: '/dashboard', available: true },
    { labelKey: 'nav.transactions', icon: 'pi pi-list', route: '/transactions', available: true },
    { labelKey: 'nav.categories', icon: 'pi pi-tags', route: '/categories', available: true },
    { labelKey: 'nav.budgets', icon: 'pi pi-chart-pie', route: '/budgets', available: true },
    { labelKey: 'nav.recurring', icon: 'pi pi-sync', route: '/recurring', available: true },
    { labelKey: 'nav.reports', icon: 'pi pi-chart-bar', route: '/reports', available: false },
  ];

  protected initials = computed(() => {
    const user = this.accountService.currentUser();
    if (!user) return '';
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  });

  /**
   * Même menu que la navbar, et mêmes clés i18n : c'est la même action rendue à
   * deux endroits. Reconstruit à chaque changement de langue, un MenuItem
   * PrimeNG étant un objet figé qui ne se retraduit pas tout seul.
   */
  protected profileMenuItems = computed<MenuItem[]>(() => {
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
