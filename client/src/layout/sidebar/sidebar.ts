import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
})
export class SidebarComponent {
  menuItems = [
    { label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' },
    { label: 'Transactions', icon: 'pi pi-list', route: '/transactions' },
    { label: 'Categories', icon: 'pi pi-tag', route: '/categories' },
    { label: 'Budgets', icon: 'pi pi-chart-bar', route: '/budgets' },
    { label: 'Plans', icon: 'pi pi-calendar', route: '/plans' },
  ];
}
