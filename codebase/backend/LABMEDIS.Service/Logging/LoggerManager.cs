using NLog;

namespace LABMEDIS.Service.Logging;

/// <summary>
/// NLog-backed implementation of <see cref="ILoggerManager"/>. This is the only logging
/// entry point used across the API/Service/Core layers (Principle IV).
/// </summary>
public sealed class LoggerManager : ILoggerManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void LogInfo(string message) => Logger.Info(message);

    public void LogWarn(string message) => Logger.Warn(message);

    public void LogError(Exception ex, string message) => Logger.Error(ex, message);

    public void LogDebug(string message) => Logger.Debug(message);
}
