using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinConsultation(int consultationId)
        {

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"consultation_{consultationId}");
        }
    }
}
