using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Api.Extensions;

/// <summary>
/// Builds the two mandatory log lines of Principle IV/VII of the constitution, exactly:
/// Info  — "{LastName} {FirstName} ({UserName}) | Début [NomAction] | {Method} {Path} IP: {IP} UserAgent: {UA}"
/// Error — "{LastName} {FirstName} ({UserName}) | Echec [NomAction] : {ExMessage} | IP: {IP}"
/// Centralized here so every controller action produces byte-identical formatting.
/// </summary>
public static class LoggingContextExtensions
{
    private static string FormatUser(CurrentUserContext? user) =>
        user is null ? "Utilisateur inconnu (Anonyme)" : $"{user.LastName} {user.FirstName} ({user.UserName})";

    public static string BuildStartLog(this HttpContext context, CurrentUserContext? user, string actionName) =>
        $"{FormatUser(user)} | Début {actionName} | {context.Request.Method} {context.Request.Path} " +
        $"IP: {context.GetIp()} UserAgent: {context.GetUserAgentName()}";

    public static string BuildErrorLog(this HttpContext context, CurrentUserContext? user, string actionName, string exMessage) =>
        $"{FormatUser(user)} | Echec {actionName} : {exMessage} | IP: {context.GetIp()}";
}
