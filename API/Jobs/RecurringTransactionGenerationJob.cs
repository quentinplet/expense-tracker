using API.Interfaces;

namespace API.Jobs;

/// Génère la transaction due de chaque transaction récurrente active, une fois par
/// jour — pas seulement à l'échéance elle-même, pour rattraper le cas où l'API
/// était arrêtée le jour J. Même famille que BudgetAutoRenewJob : un
/// BackgroundService natif, pas de Hangfire.
///
/// L'idempotence ne repose pas sur une table de suivi séparée : la table
/// Transactions elle-même (via RecurringTransactionId) est l'état persisté qu'on
/// interroge avant d'insérer (RecurringTransactionService.GenerateDueTransactionsAsync),
/// donc rejouer ce job après un redémarrage ne duplique rien.
public class RecurringTransactionGenerationJob(
    IServiceScopeFactory scopeFactory, ILogger<RecurringTransactionGenerationJob> logger) : BackgroundService
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
            var recurringTransactionService = scope.ServiceProvider.GetRequiredService<IRecurringTransactionService>();

            var generated = await recurringTransactionService.GenerateDueTransactionsAsync();
            if (generated > 0)
                logger.LogInformation("RecurringTransactionGenerationJob generated {Count} transaction(s).", generated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RecurringTransactionGenerationJob failed.");
        }
    }
}
