using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PropertyOS.Api.Hubs;

/// <summary>
/// Real-time in-app notification channel. Authenticated via JWT (browsers pass the token
/// as ?access_token=… — wired in Program.cs OnMessageReceived). Server-side dispatch
/// targets a specific recipient with IHubContext&lt;NotificationsHub&gt;.Clients.User(userId):
/// SignalR's default IUserIdProvider keys connections by ClaimTypes.NameIdentifier, which
/// JwtTokenGenerator emits as the user id. The hub itself is intentionally receive-only —
/// clients never invoke server methods; notification state changes go through the REST API.
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
}
