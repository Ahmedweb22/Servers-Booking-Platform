using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shtbly.Hubs
{

[Authorize]
public class NotificationHub : Hub
{
}

}
