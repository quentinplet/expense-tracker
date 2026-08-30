using API.Interfaces;

namespace API.Jobs;

/// Duplique chaque budget AutoRenew du mois courant vers le mois suivant, une fois
/// par jour — pas seulement au changement de mois, pour rattraper le cas où l'API
/// était arrêtée le jour J. Pas de Hangfire ici : un seul job, un seul process, un
/// `BackgroundService` natif suffit et n'ajoute aucune dépendance.
///
/// L'idempotence ne repose pas sur une table de suivi séparée : la table Budgets
/// elle-même est l'état persisté qu'on interroge avant d'insérer
/// (BudgetService.RenewBudgetsAsync vérifie l'absence de la ligne du mois suivant
/// avant toute écriture), donc rejouer ce job après un redémarrage ne duplique rien.
public class BudgetAutoRenewJob(
    IServiceScopeFactory scopeFactory, ILogger<BudgetAutoRenewJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        // Premier passage immédiat au démarrage : couvre le jour où l'API était
        // arrêtée, sans attendre le prochain tick de 24h.
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var budgetService = scope.ServiceProvider.GetRequiredService<IBudgetService>();

            var created = await budgetService.RenewBudgetsAsync();
            if (created > 0)
                logger.LogInformation("BudgetAutoRenewJob duplicated {Count} budget(s).", created);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "BudgetAutoRenewJob failed.");
        }
    }
}