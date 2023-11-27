namespace WebApplication3.Hubs.Clients
{
    /// <summary>
    /// Interface IHub serve pra evitar de digitar erroneamente o metodo que os clientes (navegador web) espera receber
    /// Herdar essa interface no hub;
    /// https://learn.microsoft.com/pt-br/aspnet/core/signalr/hubs?view=aspnetcore-8.0
    /// </summary>
    public interface ILogHub
    {
        public Task EnviarLog(string message);
        public Task EnviarLog(List<string> message);
        public Task CarregarLog(List<string> message);
        
        public Task SendProgressUpdate(string message, decimal percentComplete);
    }
}
