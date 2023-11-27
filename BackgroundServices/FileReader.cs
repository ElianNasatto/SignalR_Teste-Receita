using Microsoft.AspNetCore.SignalR;
using System.IO.Compression;
using System.Threading;
using WebApplication3.Hubs;
using WebApplication3.Hubs.Clients;

namespace WebApplication3.BackgroundServices
{
    public class FileReader : BackgroundService
    {
        public IHubContext<LogHub, ILogHub> Hub { get; set; }
        public FileReader(IHubContext<LogHub, ILogHub> hub)
        {
            Hub = hub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var path in Directory.GetFiles("C:\\timoneiro\\ZippedFiles"))
                        {
                            FileInfo fileInfo1 = new FileInfo(path);
                            await Hub.Clients.All.EnviarLog($"Iniciando processamento do arquivo;Nome - {fileInfo1.Name};Tamanho - {fileInfo1.Length};");

                            if (fileInfo1.Extension == ".zip")
                            {
                                System.IO.Directory.CreateDirectory("C://Timoneiro//ArquivosDesconpactados");
                                ZipFile.ExtractToDirectory(path, "C://Timoneiro//ArquivosDesconpactados", true);
                                FileInfo fileExtrated = new FileInfo(Directory.GetFiles("C://timoneiro//ArquivosDesconpactados").First());

                                //File.Delete(path);
                                fileInfo1 = new FileInfo(fileExtrated.FullName);
                            }

                            int QtdLinhasProcessadas = 0;
                            int QtdLinhasProcessar = CountLines(fileInfo1.FullName);

                            string[] sufixos = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                            if (fileInfo1.Length == 0)
                                return;

                            int lugar = Convert.ToInt32(Math.Floor(Math.Log(fileInfo1.Length, 1024)));
                            double tamanhoArredondado = Math.Round(fileInfo1.Length / Math.Pow(1024, lugar), 2);

                            string msg = $"Arquivo - {fileInfo1.Name} - Tamanho - {tamanhoArredondado} {sufixos[lugar]}";


                            Parallel.ForEach(File.ReadLines(fileInfo1.FullName, System.Text.Encoding.GetEncoding("utf-8")), new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (line) =>
                            {
                                Interlocked.Increment(ref QtdLinhasProcessadas);

                                if ((QtdLinhasProcessadas % 50) == 0)
                                {
                                    decimal percentage = (decimal)QtdLinhasProcessadas * 100 / QtdLinhasProcessar;
                                    await Hub.Clients.All.SendProgressUpdate(msg, percentage);
                                }

                                if ((QtdLinhasProcessadas % 1000) == 0)
                                    Hub.Clients.All.EnviarLog("1000 linhas processadas").Wait();

                            });

                            File.Delete(fileInfo1.FullName);

                        }

                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                    catch (Exception ex)
                    {
                        await Hub.Clients.All.EnviarLog(ex.Message);
                        await Task.Delay(TimeSpan.FromMinutes(1));

                    }
                }
            });

        }

        private int CountLines(string filePath)
        {
            int lineCount = 0;

            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.ReadLine() != null)
                {
                    lineCount++;
                }
            }

            return lineCount;
        }
    }

}

