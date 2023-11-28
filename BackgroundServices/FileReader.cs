using AplicationSignalR.Models;
using CsvHelper;
using CsvHelper.Configuration;
using FastMember;
using Microsoft.AspNetCore.SignalR;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using System.Threading;
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
                        }
                        foreach (var path in Directory.GetFiles("C://Timoneiro//ArquivosDesconpactados"))
                        {
                            FileInfo fileInfo1 = new FileInfo(path);

                            int QtdLinhasProcessadas = 0;
                            int QtdLinhasProcessar = CountLines(fileInfo1.FullName);

                            string[] sufixos = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                            if (fileInfo1.Length == 0)
                                return;

                            int lugar = Convert.ToInt32(Math.Floor(Math.Log(fileInfo1.Length, 1024)));
                            double tamanhoArredondado = Math.Round(fileInfo1.Length / Math.Pow(1024, lugar), 2);

                            string msg = $"Arquivo - {fileInfo1.Name} - Tamanho - {tamanhoArredondado} {sufixos[lugar]}";

                            Mutex mutex = new Mutex();

                            DataTable dataTable = new DataTable();

                            //Case sensitive

                            var colunas = new string[] {
                "cnpj_basico",
                "razao_social",
                "natureza_juridica",
                "qualificacao_responsavel",
                "capital_social",
                "porte_empresa",
                "ente_federativo_responsavel"};

                            foreach (string coluna in colunas)
                                dataTable.Columns.Add(coluna);



                            using (var connection = new SqlConnection(configuration.GetValue<string>("CONEXAO_BANCO")))
                            {
                                connection.Open();
                                using (var sqlBulkCopy = new SqlBulkCopy(connection))
                                {
                                    sqlBulkCopy.BulkCopyTimeout = TimeSpan.FromMinutes(5).Seconds;
                                    sqlBulkCopy.BatchSize = (int)(QtdLinhasProcessar / 10);
                                    sqlBulkCopy.EnableStreaming = true;
                                    sqlBulkCopy.DestinationTableName = "dbo.contribuintereceita";

                                    var colunasClasse = (typeof(ContribuinteReceitaFederalModel).GetProperties().Where(x => x.Name != "id")).Select(x => x.Name).ToArray();
                                    for (int i = 0; i < colunas.Length; i++)
                                        sqlBulkCopy.ColumnMappings.Add(colunasClasse[i], colunas[i]);

                                    using (var reader = ObjectReader.Create(ParseFile(fileInfo1.FullName, QtdLinhasProcessar)))
                                        sqlBulkCopy.WriteToServer(reader);
                                }

                            }

                            //    Parallel.ForEach(File.ReadLines(fileInfo1.FullName, System.Text.Encoding.GetEncoding("utf-8")), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (line) =>
                            //{
                            //    var lineSplited = line.Replace("\"", "").Split(";", StringSplitOptions.RemoveEmptyEntries);
                            //    Interlocked.Increment(ref QtdLinhasProcessadas);

                            //    dataTable.Rows.Add(lineSplited);

                            //    if ((QtdLinhasProcessadas % 50) == 0)
                            //    {
                            //        mutex.WaitOne();
                            //        decimal percentage = (decimal)QtdLinhasProcessadas * 100 / QtdLinhasProcessar;
                            //        await Hub.Clients.All.SendProgressUpdate(msg, percentage);
                            //        using (var connection = new SqlConnection(configuration.GetValue<string>("CONEXAO_BANCO")))
                            //        {

                            //            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);
                            //            sqlBulkCopy.BulkCopyTimeout = 120;
                            //            sqlBulkCopy.DestinationTableName = "dbo.ContribuinteReceita";
                            //            sqlBulkCopy.WriteToServer(dataTable);
                            //            mutex.ReleaseMutex();
                            //        }

                            //    }
                            //});

                            //File.Delete(fileInfo1.FullName);

                        }

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
        static IEnumerable<ContribuinteReceitaFederalModel> ParseFile(string path, int lineCount)
        {
            int QtdLinhasProcessadas = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if ((QtdLinhasProcessadas % 50) == 0)
                    {
                        decimal percentage = (decimal)QtdLinhasProcessadas * 100 / lineCount;
                        Hub.Clients.All.SendProgressUpdate($"Processado - {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}", percentage);
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

        private static ContribuinteReceitaFederalModel RetornaObjeto(string line)
        {
            string[] campos = line.Split("\";");
            int valor = 0;

            return new ContribuinteReceitaFederalModel()
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

