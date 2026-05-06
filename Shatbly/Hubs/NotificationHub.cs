using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shatbly.Hubs
{

[Authorize]
public class NotificationHub : Hub
{
}

}
