import { Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { RouterLink } from '@angular/router';

import { AccountService } from '../../core/services/account-service';

@Component({
  selector: 'app-home',
  imports: [ButtonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected accountService = inject(AccountService);
}
