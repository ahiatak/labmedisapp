using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>
/// T154 — notification persistée retrouvée après reconnexion (FR-094). Simulates "offline at
/// emission time" by emitting the event with no SignalR listener connected, then verifies
/// the REST endpoint still returns it (the persistence write happens before the push, so it
/// never depends on anyone being connected to receive it).
/// </summary>
public class NotificationPersistenceTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Notification_EmittedToRoleGroup_IsPersistedAndRetrievableAfterReconnection()
    {
        var email = $"achats-{Guid.NewGuid()}@labmedis.test";
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, email, "ResponsableAchats");

        Guid purchaseOrderId;
        using (var scope = factory.Services.CreateScope())
        {
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            purchaseOrderId = Guid.NewGuid();
            // No SignalR connection is open for this user — this is exactly the "offline at
            // emission time" scenario FR-094 must survive.
            await notificationService.EmitAsync("order:pendingApproval", "Role:ResponsableAchats", new { purchaseOrderId });
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/notifications?unreadOnly=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();

        var found = Assert.Single(notifications!, n => n.EventType == "order:pendingApproval");
        Assert.False(found.IsRead);
        Assert.Contains(purchaseOrderId.ToString(), found.Payload, StringComparison.OrdinalIgnoreCase);

        var markReadResponse = await client.PostAsync($"/api/notifications/{found.Id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var afterReadResponse = await client.GetAsync("/api/notifications?unreadOnly=true");
        var afterRead = await afterReadResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.DoesNotContain(afterRead!, n => n.Id == found.Id);
    }
}
