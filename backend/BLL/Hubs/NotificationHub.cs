using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BLL.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
