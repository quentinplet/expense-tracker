import { Component, computed, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountService } from '@/core/services/account-service';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { signal } from '@angular/core';

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
  imports: [RouterLink, RouterLinkActive, TranslatePipe, CategoryNamePipe],
  templateUrl: './sidebar.html',
})
export class SidebarComponent implements OnInit {
  private categorieService = inject(CategorieService);
  protected accountService = inject(AccountService);

  protected categories = signal<Categorie[]>([]);

  protected readonly menuItems: NavItem[] = [
    { labelKey: 'nav.dashboard', icon: 'pi pi-th-large', route: '/dashboard', available: true },
    { labelKey: 'nav.transactions', icon: 'pi pi-list', route: '/transactions', available: true },
    { labelKey: 'nav.budgets', icon: 'pi pi-chart-pie', route: '/budgets', available: false },
    { labelKey: 'nav.recurring', icon: 'pi pi-sync', route: '/recurring', available: false },
    { labelKey: 'nav.reports', icon: 'pi pi-chart-bar', route: '/reports', available: false },
    { labelKey: 'nav.settings', icon: 'pi pi-cog', route: '/settings', available: false },
  ];

  /** La sidebar montre un aperçu, pas la liste complète : au-delà, elle défile. */
  protected visibleCategories = computed(() => this.categories().slice(0, 6));

  protected initials = computed(() =>
    (this.accountService.currentUser()?.userName ?? '').slice(0, 2).toUpperCase(),
  );

  ngOnInit() {
    this.categorieService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }
}
