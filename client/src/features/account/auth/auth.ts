import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { RippleModule } from 'primeng/ripple';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AccountService } from '../../../core/services/account-service';
import { MessageService } from 'primeng/api';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-auth',
  imports: [
    ButtonModule,
    InputTextModule,
    PasswordModule,
    ReactiveFormsModule,
    RouterModule,
    RippleModule,
    TranslatePipe,
  ],
  templateUrl: './auth.html',
  styleUrl: './auth.scss',
})
export class Auth {
  mode = signal<'login' | 'register'>('login');

  submiting = signal(false);
  /** N'affiche une erreur de validation client qu'après une tentative de
   *  soumission — même convention que category-form-dialog.ts/settings.ts,
   *  sinon les champs passent en rouge dès le premier `blur`. */
  submitted = signal(false);
  errors = signal<Record<string, string[]>>({});
  /** Un email déjà pris (409) ou une erreur Identity non couverte par les
   *  Data Annotations arrive en texte brut (chaîne ou tableau), pas dans la
   *  forme `ValidationProblemDetails` que `errors` sait afficher — voir le
   *  même correctif dans settings.ts pour le détail. */
  serverError = signal<string | null>(null);

  /** Même token que les autres formulaires (settings/category/transaction) :
   *  Tailwind ne peut pas battre le thème PrimeNG injecté hors layer. */
  protected readonly fieldTokens = { paddingY: '0.9rem', paddingX: '0.9rem' };

  private accountService = inject(AccountService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);
  private messageService = inject(MessageService);
  private fb = inject(FormBuilder);

  constructor() {
    const isLogin = this.route.snapshot.data['isLogin'] ?? true;
    this.mode.set(isLogin ? 'login' : 'register');

    // Les erreurs de validation client se recalculent déjà seules via les
    // signaux (submitted() && control.invalid). Celles renvoyées par le
    // serveur (errors/serverError) ne le font pas : sans ça, un email déjà
    // pris resterait affiché en rouge même après correction du champ.
    this.loginForm.controls.email.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.serverError.set(null));
    this.loginForm.controls.password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.serverError.set(null));

    this.registerForm.controls.firstName.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearFieldError('FirstName'));
    this.registerForm.controls.lastName.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearFieldError('LastName'));
    this.registerForm.controls.email.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.clearFieldError('Email');
      this.serverError.set(null);
    });
    this.registerForm.controls.password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearFieldError('Password'));
  }

  loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  registerForm = this.fb.nonNullable.group(
    {
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  switchMode(mode: 'login' | 'register') {
    this.mode.set(mode);
    this.submitted.set(false);
    this.errors.set({});
    this.serverError.set(null);
    void this.router.navigateByUrl(mode === 'login' ? '/login' : '/register');
  }

  submit() {
    this.submitted.set(true);
    this.errors.set({});
    this.serverError.set(null);

    if (this.mode() === 'login') {
      if (this.loginForm.invalid) {
        this.loginForm.markAllAsTouched();
        return;
      }
      const { email, password } = this.loginForm.getRawValue();
      this.login(email, password);
      return;
    }

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    const { firstName, lastName, email, password } = this.registerForm.getRawValue();
    this.register(firstName, lastName, email, password);
  }

  private login(email: string, password: string) {
    this.submiting.set(true);

    this.accountService.login({ email, password }).subscribe({
      next: () => {
        this.submiting.set(false);
        void this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.submiting.set(false);
        // Message générique volontaire (§11 : ne pas confirmer si c'est
        // l'email ou le mot de passe qui est faux) — affiché en toast ET en
        // ligne, comme les autres erreurs serveur de cette page.
        this.serverError.set(this.translate.instant('auth.login.failed'));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('auth.login.failed'),
        });
      },
    });
  }

  private register(firstName: string, lastName: string, email: string, password: string) {
    this.submiting.set(true);

    this.accountService.register({ firstName, lastName, email, password }).subscribe({
      next: () => {
        this.submiting.set(false);
        void this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.submiting.set(false);
        this.errors.set(error?.error?.errors ?? {});
        this.serverError.set(this.extractServerMessage(error));
      },
    });
  }

  private clearFieldError(field: string) {
    if (!(field in this.errors())) return;
    const rest = { ...this.errors() };
    delete rest[field];
    this.errors.set(rest);
  }

  private extractServerMessage(error: unknown): string | null {
    const body = (error as { error?: unknown } | undefined)?.error;
    if (Array.isArray(body)) return (body[0] as string) ?? null;
    if (typeof body === 'string') return body;
    return null;
  }
}
