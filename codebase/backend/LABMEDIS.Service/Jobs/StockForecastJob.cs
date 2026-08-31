using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;

namespace LABMEDIS.Service.Jobs;

/// <summary>Daily Hangfire job (US10 — FR-063/FR-064). Also triggerable manually via POST /api/forecast/run.</summary>
public class StockForecastJob(IForecastService forecastService, ILoggerManager logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInfo("StockForecastJob | Début du calcul quotidien du point de commande.");
        var createdCount = await forecastService.RunCalculationAsync(cancellationToken);
        logger.LogInfo($"StockForecastJob | Terminé — {createdCount} nouvelle(s) suggestion(s) de réapprovisionnement créée(s).");
    }
}
