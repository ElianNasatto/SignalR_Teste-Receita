namespace AplicationSignalR.Hubs.Clients
{
    public interface IChatHub
    {
        public Task ReceiveSystemMessage(string message);
        public Task Send(string name, string message);
    }
}
