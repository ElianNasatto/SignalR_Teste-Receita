using AplicationSignalR.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace AplicationSignalR.Hubs
{
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("broadcastMessage", Context.User.Identity.Name, "Entrou no bate papo.");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("broadcastMessage", $"{Context.User.Identity.Name} não esta mais entre nós.");
            await base.OnConnectedAsync();
        }
        public async Task Send(string message)
        {
            // Call the broadcastMessage method to update clients.
            await Clients.All.SendAsync("broadcastMessage", Context.User.Identity.Name, message);
        }
    }
}
