namespace LABMEDIS.Service.Logging;

/// <summary>
/// Exclusive logging abstraction for the whole application (Principle IV of the
/// constitution). The standard Microsoft.Extensions.Logging.ILogger&lt;T&gt; is forbidden —
/// every controller and service MUST log through this interface instead.
/// </summary>
public interface ILoggerManager
{
    void LogInfo(string message);

    void LogWarn(string message);

    void LogError(Exception ex, string message);

    void LogDebug(string message);
}
