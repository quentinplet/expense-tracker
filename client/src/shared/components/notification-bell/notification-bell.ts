import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PopoverModule } from 'primeng/popover';
import { NotificationService } from '@/core/services/notification-service';
import { LanguageService } from '@/core/services/language-service';
import { CategoryNamePipe } from '@/shared/pipes/category-name-pipe';
import { Notification } from '@/types/notification';

/**
 * Cloche de la topbar : panneau déroulant (p-popover) listant les
 * notifications récentes, badge de compteur non lues. Polling démarré/arrêté
 * sur le cycle de vie du composant — il ne vit que dans MainLayout, monté
 * uniquement derrière authGuard (même principe que
 * AccountService.startTokenRefreshInterval, mais scopé au composant plutôt
 * qu'au login : plus simple, pas de couplage supplémentaire avec AccountService).
 */
@Component({
  selector: 'app-notification-bell',
  // CategoryNamePipe est utilisé impérativement (this.categoryNamePipe.transform(...)
  // dans textFor()), jamais comme pipe de template ici — providers, pas imports.
  imports: [PopoverModule, TranslatePipe, CurrencyPipe, DatePipe],
  providers: [CategoryNamePipe],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.scss',
})
export class NotificationBell implements OnInit, OnDestroy {
  protected notificationService = inject(NotificationService);
  private languageService = inject(LanguageService);
  private translate = inject(TranslateService);
  private categoryNamePipe = inject(CategoryNamePipe);

  protected locale = computed(() => this.languageService.current());

  /** Plafonné à l'affichage : le badge montre "9+", jamais un nombre à trois
   *  chiffres qui ferait éclater la pastille. */
  protected badgeCount = computed(() => {
    const count = this.notificationService.unreadCount();
    return count > 9 ? '9+' : String(count);
  });

  ngOnInit(): void {
    this.notificationService.startPolling();
  }

  ngOnDestroy(): void {
    this.notificationService.stopPolling();
  }

  /** Rafraîchit à l'ouverture, pas seulement au tick périodique — pour ne pas
   *  montrer un compteur obsolète si l'utilisateur ouvre juste avant un tick. */
  onOpen(): void {
    this.notificationService.refresh();
  }

  onSelect(notification: Notification): void {
    if (!notification.isRead) this.notificationService.markAsRead(notification.id);
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead();
  }

  /** stopPropagation : le bouton d'effacement est imbriqué dans le bouton de la
   *  ligne (§ notification-bell.html) — sans ça, l'effacement déclencherait aussi
   *  onSelect() juste au-dessus (marquer comme lu + naviguer). */
  onDismiss(notification: Notification, event: Event): void {
    event.stopPropagation();
    this.notificationService.delete(notification.id);
  }

  /**
   * Résolution de la clé i18n par Type (et par budgetIsGlobal pour les
   * seuils), en un seul endroit — même règle que CategoryNamePipe pour les
   * catégories (§10).
   */
  protected textFor(n: Notification): string {
    if (n.type === 'RecurringTransactionGenerated') {
      return this.translate.instant('notifications.recurringGenerated', {
        label: n.transactionLabel,
      });
    }

    if (n.budgetIsGlobal) {
      return this.translate.instant(`notifications.budgetGlobalThreshold${n.thresholdPercent}`);
    }

    const category = this.categoryNamePipe.transform({
      name: n.budgetCategoryName ?? '',
      translationKey: n.budgetCategoryTranslationKey,
    });
    return this.translate.instant(`notifications.budgetCategoryThreshold${n.thresholdPercent}`, {
      category,
    });
  }

  protected iconFor(n: Notification): string {
    return n.type === 'RecurringTransactionGenerated' ? 'pi-sync' : 'pi-chart-pie';
  }
}
