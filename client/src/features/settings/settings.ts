import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AccountService } from '@/core/services/account-service';
import { UserAccountService } from '@/core/services/user-account-service';

@Component({
  selector: 'app-settings',
  imports: [ButtonModule, InputTextModule, PasswordModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  protected accountService = inject(AccountService);
  private userAccountService = inject(UserAccountService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  /** Même token que les autres formulaires (category/transaction) : Tailwind
   *  ne peut pas battre le thème PrimeNG injecté hors layer. */
  protected readonly fieldTokens = { paddingY: '0.9rem' };

  /**
   * `ChangePassword`/`ChangeEmail`/`DeleteAccount` renvoient un 400 en texte brut
   * (`BadRequest("...")`ou `BadRequest(IEnumerable<string>)`) pour un mot de passe
   * actuel incorrect ou un nouveau mot de passe trop faible — pas la forme
   * `ValidationProblemDetails` (`{ errors: {...} }`) que `xxxErrors` sait afficher.
   * Sans ça, ce cas précis ne montrait rien sous les champs, seulement le toast
   * générique de l'intercepteur global.
   */
  private extractServerMessage(error: unknown): string | null {
    const body = (error as { error?: unknown } | undefined)?.error;
    if (Array.isArray(body)) return (body[0] as string) ?? null;
    if (typeof body === 'string') return body;
    return null;
  }

  // --- Nom ---
  protected nameForm = this.fb.nonNullable.group({
    firstName: [this.accountService.currentUser()?.firstName ?? '', Validators.required],
    lastName: [this.accountService.currentUser()?.lastName ?? '', Validators.required],
  });
  protected nameErrors = signal<Record<string, string[]>>({});
  protected nameServerError = signal<string | null>(null);
  protected nameSubmitted = signal(false);
  protected nameSaving = signal(false);

  submitName() {
    if (this.nameForm.invalid) {
      this.nameSubmitted.set(true);
      this.nameForm.markAllAsTouched();
      return;
    }

    this.nameErrors.set({});
    this.nameServerError.set(null);
    this.nameSaving.set(true);
    this.userAccountService.changeName(this.nameForm.getRawValue()).subscribe({
      next: (user) => {
        this.accountService.setCurrentUser(user);
        this.nameSaving.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('settings.security.name.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.nameSaving.set(false);
        this.nameErrors.set(error?.error?.errors ?? {});
        this.nameServerError.set(this.extractServerMessage(error));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('settings.security.name.updateFailed'),
        });
      },
    });
  }

  // --- Mot de passe ---
  protected passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
  });
  protected passwordErrors = signal<Record<string, string[]>>({});
  protected passwordServerError = signal<string | null>(null);
  protected passwordSubmitted = signal(false);
  protected passwordSaving = signal(false);

  submitPassword() {
    if (this.passwordForm.invalid) {
      this.passwordSubmitted.set(true);
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.passwordErrors.set({});
    this.passwordServerError.set(null);
    this.passwordSaving.set(true);
    this.userAccountService.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.passwordSaving.set(false);
        this.passwordSubmitted.set(false);
        this.passwordForm.reset({ currentPassword: '', newPassword: '' });
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('settings.security.password.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.passwordSaving.set(false);
        this.passwordErrors.set(error?.error?.errors ?? {});
        this.passwordServerError.set(this.extractServerMessage(error));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('settings.security.password.updateFailed'),
        });
      },
    });
  }

  // --- Email ---
  protected emailForm = this.fb.nonNullable.group({
    newEmail: ['', [Validators.required, Validators.email]],
    currentPassword: ['', Validators.required],
  });
  protected emailErrors = signal<Record<string, string[]>>({});
  protected emailServerError = signal<string | null>(null);
  protected emailSubmitted = signal(false);
  protected emailSaving = signal(false);

  submitEmail() {
    if (this.emailForm.invalid) {
      this.emailSubmitted.set(true);
      this.emailForm.markAllAsTouched();
      return;
    }

    this.emailErrors.set({});
    this.emailServerError.set(null);
    this.emailSaving.set(true);
    this.userAccountService.changeEmail(this.emailForm.getRawValue()).subscribe({
      next: (user) => {
        this.accountService.setCurrentUser(user);
        this.emailSaving.set(false);
        this.emailSubmitted.set(false);
        this.emailForm.reset({ newEmail: '', currentPassword: '' });
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('settings.security.email.updated'),
          life: 3000,
        });
      },
      error: (error) => {
        this.emailSaving.set(false);
        this.emailErrors.set(error?.error?.errors ?? {});
        this.emailServerError.set(this.extractServerMessage(error));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('settings.security.email.updateFailed'),
        });
      },
    });
  }

  // --- Suppression du compte ---
  protected deleteForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
  });
  protected deleteServerError = signal<string | null>(null);
  protected deleteSubmitted = signal(false);
  protected deleteSaving = signal(false);

  confirmDelete() {
    if (this.deleteForm.invalid) {
      this.deleteSubmitted.set(true);
      this.deleteForm.markAllAsTouched();
      return;
    }

    this.confirmationService.confirm({
      message: this.translate.instant('settings.security.danger.confirmMessage'),
      header: this.translate.instant('settings.security.danger.confirmTitle'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deleteAccount(),
    });
  }

  private deleteAccount() {
    this.deleteServerError.set(null);
    this.deleteSaving.set(true);
    this.userAccountService.deleteAccount(this.deleteForm.getRawValue()).subscribe({
      next: () => {
        this.accountService.currentUser.set(null);
        void this.router.navigateByUrl('/login');
      },
      error: (error) => {
        this.deleteSaving.set(false);
        this.deleteServerError.set(this.extractServerMessage(error));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('settings.security.danger.deleteFailed'),
        });
      },
    });
  }
}
