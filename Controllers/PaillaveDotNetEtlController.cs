using AplicationSignalR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Paillave.Etl.Core;
using Paillave.Etl.FileSystem;
using Paillave.Etl.TextFile;
using Paillave.Etl.Zip;
using Paillave.Etl.SqlServer;
using Microsoft.AspNetCore.SignalR;
using WebApplication3.Hubs.Clients;
using WebApplication3.Hubs;

namespace AplicationSignalR.Controllers
{
    public class PaillaveDotNetEtlController : Controller
    {
        public PaillaveDotNetEtlController(IHubContext<LogHub, ILogHub> hub, IConfiguration configuration)
        {
            Hub = hub;
            this.configuration = configuration;
        }

        public IActionResult Index()
        {
            Thread thread = new Thread(() =>
            {
                Main(configuration);
            });

            thread.Start();
            return View();
        }
        private static IHubContext<LogHub, ILogHub> Hub { get; set; }
        private IConfiguration configuration { get; set; }
        static void Main(IConfiguration configuration)
        {
            var processRunner = StreamProcessRunner.Create<string>(DefineProcess);

            //processRunner.DebugNodeStream += (sender, e) =>
            //{
            //    Hub.Clients.All.EnviarLog(e.NodeName).Wait();
            //    //Console.WriteLine(e.NodeName);
            //};

            using (var cnx = new SqlConnection(configuration.GetValue<string>("CONEXAO_BANCO")))
            {
                cnx.Open();
                var executionOptions = new ExecutionOptions<string> { Resolver = new SimpleDependencyResolver().Register(cnx), TraceProcessDefinition = DefineTraceProcess };
                var res = processRunner.ExecuteAsync("posso por qualquer coisa aqui?", executionOptions).Result;
                Hub.Clients.All.EnviarLog(res.Failed ? "Failed" : "Succeeded").Wait();
            }


        }

        private static void DefineTraceProcess(IStream<TraceEvent> traceStream, ISingleStream<string> contentStream)
        {
            traceStream
              .Where("keep only summary of node and errors", i => i.Content is CounterSummaryStreamTraceContent || i.Content is UnhandledExceptionStreamTraceContent)
              .Select("create log entry", i => new
              {
                  DateTime = i.DateTime,
                  ExecutionId = i.ExecutionId,
                  EventType = i.Content switch
                  {
                      CounterSummaryStreamTraceContent => "EndOfNode",
                      UnhandledExceptionStreamTraceContent => "Error",
                      _ => "Unknown"
                  },
                  Message = i.Content switch
                  {
                      CounterSummaryStreamTraceContent counterSummary => $"{i.NodeName}: {counterSummary.Counter}",
                      UnhandledExceptionStreamTraceContent unhandledException => $"{i.NodeName}({i.NodeTypeName}): [{unhandledException.Level.ToString()}] {unhandledException.Message}",
                      _ => "Unknown"
                  }
              }).Do("mostrando erro", i => Hub.Clients.All.EnviarLog(i.Message));

        }
        private static void DefineProcess(ISingleStream<string> contextStream)
        {
            contextStream.CrossApplyFolderFiles("list all required files", i => "C:\\Users\\Elian\\Downloads\\Empresas0", "*.zip", true)
                .CrossApplyZipFiles("extract files from zip", "*.EMPRECSV")
                .CrossApplyTextFile("parse file", FlatFileDefinition.Create(i => new ContribuinteReceitaFederalModel
                {
                    CnpjBasico = i.ToColumn("cnpj_basico"),
                    RazaoSocial = i.ToColumn("razao_social"),
                    NaturezaJuridica = i.ToNumberColumn<int>("natureza_juridica", "."),
                    QualificacaoResponsavel = i.ToNumberColumn<int>("qualificacao_responsavel", "."),
                    CapitalSocial = i.ToNumberColumn<decimal>("capital_social", "."), // O "m" indica que é um literal decimal em C#
                    PorteEmpresa = i.ToNumberColumn<int>("porte_empresa", "."),
                    EnteFederativoResponsavel = i.ToColumn("ente_federativo_responsavel"),
                })
                .IsColumnSeparated(';'))
                //.Distinct("exclude duplicates", i => i.CnpjBasico)
                .SqlServerSave("save in DB", o => o.ToTable("dbo.ContribuinteReceita").DoNotSave(p => p.id));
                //.Do("display ids on console", i => Hub.Clients.All.EnviarLog(i.id.ToString()));
        }

    }
}