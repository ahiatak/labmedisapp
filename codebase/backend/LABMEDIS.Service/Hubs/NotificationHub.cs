using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LABMEDIS.Service.Hubs;

/// <summary>
/// Real-time notification hub (US12 — contracts/notifications.md, FR-076/FR-077). Every
/// connection joins one SignalR group per role and per permission it carries, so
/// NotificationService can target an event at "Role:Direction" or "Permission:Stock.Read"
/// without tracking connection ids itself. Redis backplane is configured in Program.cs
/// (Principle IX — zero polling, multi-instance scale-out).
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        foreach (var roleClaim in Context.User?.FindAll(System.Security.Claims.ClaimTypes.Role) ?? [])
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Role:{roleClaim.Value}");
        }

        foreach (var permissionClaim in Context.User?.FindAll("permission") ?? [])
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Permission:{permissionClaim.Value}");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "All");
        await base.OnConnectedAsync();
    }
}
