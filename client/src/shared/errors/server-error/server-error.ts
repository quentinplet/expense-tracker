import { ApiError } from '@/types/error';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';

@Component({
  selector: 'app-server-error',
  imports: [Button],
  templateUrl: './server-error.html',
  styleUrl: './server-error.scss',
})
export class ServerError {
  protected error: ApiError;
  private router = inject(Router);
  protected showDetails = false;

  constructor() {
    const navigation = this.router.getCurrentNavigation();
    this.error = navigation?.extras.state?.['error'];
  }

  detailsToggle() {
    this.showDetails = !this.showDetails;
  }

  goToDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
