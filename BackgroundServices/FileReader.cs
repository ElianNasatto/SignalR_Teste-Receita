using AplicationSignalR.Models;
using FastMember;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Compression;
using WebApplication3.Hubs;
using WebApplication3.Hubs.Clients;

namespace WebApplication3.BackgroundServices
{
    public class FileReader : BackgroundService
    {
        public FileReader(IHubContext<LogHub, ILogHub> hub, IConfiguration configuration)
        {
            Hub = hub;
            this.configuration = configuration;
        }
        private static IHubContext<LogHub, ILogHub> Hub { get; set; }
        public IConfiguration configuration { get; set; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {

                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                
                await Hub.Clients.All.EnviarLog($"Seviço de processamento esta ativo.");

                while (true)
                {
                    try
                    {
                        #region UnzipFiles
                        foreach (var path in Directory.GetFiles("C:\\timoneiro\\ZippedFiles"))
                        {
                            FileInfo fileInfo1 = new FileInfo(path);
                            await Hub.Clients.All.EnviarLog($"Descompactando arquivo;Nome - {fileInfo1.Name};Tamanho - {fileInfo1.Length};");

                            if (fileInfo1.Extension == ".zip")
                            {
                                System.IO.Directory.CreateDirectory("C://Timoneiro//ArquivosDesconpactados");
                                ZipFile.ExtractToDirectory(path, "C://Timoneiro//ArquivosDesconpactados", true);
                                FileInfo fileExtrated = new FileInfo(Directory.GetFiles("C://timoneiro//ArquivosDesconpactados").First());

                                File.Delete(path);
                                fileInfo1 = new FileInfo(fileExtrated.FullName);
                            }
                        }
                        #endregion

                        foreach (var path in Directory.GetFiles("C://Timoneiro//ArquivosDesconpactados"))
                        {
                            FileInfo fileInfo1 = new FileInfo(path);
                            if (fileInfo1.Length == 0)
                                return;

                            await Hub.Clients.All.EnviarLog($"Iniciando processamento de arquivo;Nome - {fileInfo1.Name};Tamanho - {fileInfo1.Length};");

                            int QtdLinhasProcessar = CountLines(fileInfo1.FullName);

                            DataTable dataTable = new DataTable();
                            foreach (string coluna in (typeof(ContribuinteReceitaFederalModel).GetProperties().Where(x => x.Name != "id")).Select(x => x.Name).ToArray())
                                dataTable.Columns.Add(coluna.ToLower());

                            using (var connection = new SqlConnection(configuration.GetValue<string>("CONEXAO_BANCO")))
                            {
                                connection.Open();
                                using (var sqlBulkCopy = new SqlBulkCopy(connection))
                                {
                                    sqlBulkCopy.BulkCopyTimeout = TimeSpan.FromMinutes(5).Seconds;
                                    sqlBulkCopy.BatchSize = (int)(QtdLinhasProcessar / 10);
                                    sqlBulkCopy.EnableStreaming = true;
                                    sqlBulkCopy.DestinationTableName = "dbo.contribuintereceita";

                                    //Case sensitive
                                    var colunasClasse = (typeof(ContribuinteReceitaFederalModel).GetProperties().Where(x => x.Name != "id")).Select(x => x.Name).ToArray();
                                    for (int i = 0; i < colunasClasse.Length; i++)
                                        sqlBulkCopy.ColumnMappings.Add(colunasClasse[i], colunasClasse[i].ToLower());

                                    using (var reader = ObjectReader.Create(ParseFile(fileInfo1.FullName, QtdLinhasProcessar)))
                                        sqlBulkCopy.WriteToServer(reader);

                                    await Hub.Clients.All.SendProgressUpdate("", 100);
                                }

                            }

                            File.Delete(fileInfo1.FullName);

                        }

                        await Hub.Clients.All.EnviarLog("Nenhum arquivo disponivel para processamento");
                        Task.Delay(TimeSpan.FromMinutes(1)).Wait();
                    }
                    catch (Exception ex)
                    {
                        Hub.Clients.All.EnviarLog(ex.Message).Wait();
                        Task.Delay(TimeSpan.FromMinutes(1)).Wait();

                    }
                }
            });

        }

        private static string RetornaCampoStringFormated(string[] campos, int index)
        {
            if (campos.Length >= index)
            {
                if (string.IsNullOrEmpty(campos[index]) || string.IsNullOrEmpty(campos[index].Replace("\"", "")))
                    return "0";

                return campos[index].Replace("\"", "");
            }
            else
            {
                return "0";
            }
        }
        private static IEnumerable<ContribuinteReceitaFederalModel> ParseFile(string path, int lineCount)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                int QtdLinhasProcessadas = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                Stopwatch stopwatchLog = Stopwatch.StartNew();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (stopwatchLog.Elapsed >= TimeSpan.FromSeconds(2))
                    {
                        stopwatchLog.Restart();
                        decimal percentage = (decimal)QtdLinhasProcessadas * 100 / lineCount;
                        Hub.Clients.All.SendProgressUpdate($"Processado {QtdLinhasProcessadas.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))} linhas em {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}", percentage);
                    }
                    QtdLinhasProcessadas++;

                    string[] campos = line.Split("\";");
                    int valor = 0;
                    yield return new ContribuinteReceitaFederalModel()
                    {
                        Cnpj_Basico = RetornaCampoStringFormated(campos, 0),
                        Razao_Social = RetornaCampoStringFormated(campos, 1),
                        Natureza_Juridica = int.TryParse(RetornaCampoStringFormated(campos, 2), out valor) ? valor : 0,
                        Qualificacao_Responsavel = int.TryParse(RetornaCampoStringFormated(campos, 3), out valor) ? valor : 0,
                        Capital_Social = decimal.TryParse(RetornaCampoStringFormated(campos, 4), out decimal valorDecimal) ? valorDecimal : 0,
                        Porte_Empresa = int.TryParse(RetornaCampoStringFormated(campos, 5), out valor) ? valor : 0,
                        Ente_Federativo_Responsavel = RetornaCampoStringFormated(campos, 6)
                    };
                }
            }
        }

        private int CountLines(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                int lineCount = 0;
                while (reader.ReadLine() != null)
                    lineCount++;

                return lineCount;
            }
        }
    }

}

