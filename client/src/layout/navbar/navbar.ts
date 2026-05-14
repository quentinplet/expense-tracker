import { AccountService } from '@/core/services/account-service';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { BusyService } from '@/core/services/busy-service';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-navbar',
  imports: [ButtonModule, AvatarModule, MenuModule, RouterLink, ProgressSpinnerModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  accountService = inject(AccountService);
  busyService = inject(BusyService);
  router = inject(Router);

  menuItems: MenuItem[] = [
    {
      label: 'Profile',
      icon: 'pi pi-user',
      command: () => this.router.navigateByUrl('/profile'),
    },
    {
      separator: true,
    },
    {
      label: 'Logout',
      icon: 'pi pi-sign-out',
      command: () => this.accountService.logout(),
    },
  ];
}
