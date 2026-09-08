import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

type QuickAction = {
  labelKey: string;
  icon: string;
  accent: string;
  link: string;
  queryParams?: Record<string, string>;
};

/** Ajouter une transaction redirige plutôt que d'ouvrir un dialogue inline ici : la
 *  modale s'ouvre elle-même sur `/transactions`, via le paramètre `new` lu au chargement
 *  (§ Transactions.ngOnInit) — un seul formulaire de transaction dans l'app, pas deux. */
const ACTIONS: QuickAction[] = [
  {
    labelKey: 'dashboard.quickActions.addTransaction',
    icon: 'pi-plus',
    accent: 'var(--p-primary-color)',
    link: '/transactions',
    queryParams: { new: '1' },
  },
  {
    labelKey: 'dashboard.quickActions.manageBudgets',
    icon: 'pi-wallet',
    accent: '#8b5cf6',
    link: '/budgets',
  },
  {
    labelKey: 'dashboard.quickActions.manageRecurring',
    icon: 'pi-sync',
    accent: '#06b6d4',
    link: '/recurring',
  },
];

@Component({
  selector: 'app-quick-actions',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './quick-actions.html',
  styleUrl: './quick-actions.scss',
})
export class QuickActions {
  protected actions = ACTIONS;
}
