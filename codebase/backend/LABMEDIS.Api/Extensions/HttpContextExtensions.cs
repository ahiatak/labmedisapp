using UAParser;

namespace LABMEDIS.Api.Extensions;

/// <summary>
/// Request-context helpers used by every controller's mandatory log line
/// (Principle IV/VII of the constitution: "... IP: {IP} UserAgent: {UA}").
/// </summary>
public static class HttpContextExtensions
{
    private static readonly Parser UaParser = Parser.GetDefault();

    public static string GetIp(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static string GetUserAgentName(this HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var clientInfo = UaParser.Parse(userAgent);
        return $"{clientInfo.UA.Family} {clientInfo.UA.Major} / {clientInfo.OS.Family}";
    }

    public static string GetRequestData(this HttpContext context) =>
        $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
}
