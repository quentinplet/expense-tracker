import { Component, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { RippleModule } from 'primeng/ripple';
import { AccountService } from '../../../core/services/account-service';

@Component({
  selector: 'app-auth',
  imports: [
    ButtonModule,
    CheckboxModule,
    InputTextModule,
    PasswordModule,
    ReactiveFormsModule,
    RouterModule,
    RippleModule,
  ],
  templateUrl: './auth.html',
  styleUrl: './auth.scss',
})
export class Auth {
  isLogin = input(true);
  cancel = output<void>();

  mode = signal<'login' | 'register'>('login');

  submiting = signal(false);

  authService = inject(AccountService);
  router = inject(Router);

  constructor() {
    this.mode.set(this.isLogin() ? 'login' : 'register');
  }

  form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  submit() {
    if (this.form.invalid) return;

    const { email, password } = this.form.getRawValue();

    this.submiting.set(true);

    if (this.mode() === 'login') {
      this.login(email, password);
    }
  }

  private login(email: string, password: string) {
    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.submiting.set(false);
        void this.router.navigate(['/']);
      },
      error: (err) => {
        this.submiting.set(false);
        console.log('Login failed:', err);
      },
    });
  }
}
