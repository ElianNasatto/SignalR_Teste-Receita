using AplicationSignalR.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace AplicationSignalR.Hubs
{
    public class ChatHub : Hub
    {
        //public override async Task OnConnectedAsync()
        //{
        //    await Clients.All.SendAsync("ReceiveSystemMessage", $"{Context.UserIdentifier} joined.");
        //    await base.OnConnectedAsync();
        //}

        public async Task Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            await Clients.All.SendAsync("broadcastMessage", name, message);
        }
    }
}
