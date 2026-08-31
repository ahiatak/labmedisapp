using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;

namespace LABMEDIS.Service.Jobs;

/// <summary>
/// Daily Hangfire job (US4/US12 — FR-043/FR-076): transitions any "Libéré" lot whose
/// ExpiryDate has passed to "Périmé", emits `lot:expiringSoon` for lots crossing their
/// category's alert threshold (J-30/60/90/120), and `quarantine:prolonged` for lots stuck in
/// quarantine beyond StockLotService.QuarantineProlongedThresholdDays.
/// </summary>
public class ExpiryAlertJob(IStockLotService stockLotService, ILoggerManager logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInfo("ExpiryAlertJob | Début du contrôle quotidien des péremptions.");

        var (expiredCount, alertCount) = await stockLotService.ProcessExpiryAlertsAsync(cancellationToken);

        logger.LogInfo($"ExpiryAlertJob | Terminé — {expiredCount} lot(s) transitionné(s) vers Périmé, {alertCount} lot(s) en alerte de péremption proche.");
    }
}
