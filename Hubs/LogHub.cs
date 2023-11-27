using Microsoft.AspNetCore.SignalR;
using System.Security;
using System.Xml;
using WebApplication3.BackgroundServices;
using WebApplication3.Hubs.Clients;

namespace WebApplication3.Hubs
{

    public class LogHub : Hub<ILogHub>
    {
        public string EnviarLog(string message)
        {
            return message;
        }
        public List<string> CarregarLogs()
        {
            return new List<string>();
        }


    }
}
