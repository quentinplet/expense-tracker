import { AccountService } from '@/core/services/account-service';
import { Component, inject } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private accountService = inject(AccountService);
  currentUser = this.accountService.currentUser();

  constructor() {
    console.log('this.currentUser:', this.currentUser);
  }
}
