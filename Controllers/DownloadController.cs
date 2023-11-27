using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication3.Hubs;
using WebApplication3.Hubs.Clients;
using static System.Net.Mime.MediaTypeNames;

namespace AplicationSignalR.Controllers
{
    public class DownloadController : Controller
    {
        private IHubContext<LogHub, ILogHub> Hub { get; set; }
        public DownloadController(IHubContext<LogHub, ILogHub> hub)
        {
            Hub = hub;
        }
        public ActionResult Index()
        {
            return View();
        }

        private void startDownload(string url)
        {
            Thread thread = new Thread(() =>
            {
                try
                {

                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadDataCompleted += new DownloadDataCompletedEventHandler((object sender, DownloadDataCompletedEventArgs e) =>
                    {
                        if (e.Error == null)
                            System.IO.File.Delete(@$"C:\\Timoneiro\\UploadTemp\\{url.Split("/")[5]}");
                        else
                            System.IO.File.Move(@$"C:\\Timoneiro\\UploadTemp\\{url.Split("/")[5]}", @$"C:\\Timoneiro\\ZippedFiles\\{url.Split("/")[5]}");

                    });
                    client.DownloadFileAsync(new Uri(url), @$"C:\\Timoneiro\\UploadTemp\\{url.Split("/")[5]}");
                }
                catch (Exception ex)
                {
                    Hub.Clients.All.EnviarLog(ex.Message);
                }

            });
            thread.Start();
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            decimal bytesIn = decimal.Parse(e.BytesReceived.ToString());
            decimal totalBytes = decimal.Parse(e.TotalBytesToReceive.ToString());
            decimal percentage = (decimal)bytesIn * 100 / totalBytes;

            string[] sufixos = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int lugarTotal = Convert.ToInt32(Math.Floor(Math.Log(e.TotalBytesToReceive, 1024)));
            double tamanhoArredondadoTotal = Math.Round(e.TotalBytesToReceive / Math.Pow(1024, lugarTotal), 2);

            int lugarDonwloaded = Convert.ToInt32(Math.Floor(Math.Log(e.BytesReceived, 1024)));
            double tamanhoDonwloaded = Math.Round(e.BytesReceived / Math.Pow(1024, lugarDonwloaded), 2);

            Hub.Clients.All.SendProgressUpdate($"{tamanhoDonwloaded} {sufixos[lugarDonwloaded]} of {tamanhoArredondadoTotal} {sufixos[lugarTotal]}", percentage);
        }

        public ActionResult Details(string url, string acao)
        {
            ViewBag.Mensagem = "Link inválido";
            ViewBag.Url = url;
            HttpClient client = new HttpClient();

            try
            {
                Uri uri = new Uri(url);
                if (url.Split("/").Last().Contains(".zip"))
                {
                    string[] segments = uri.Segments;
                    string newUrl = url.TrimEnd(segments[segments.Length - 1].ToCharArray());
                    uri = new Uri(newUrl);
                }
                byte[] fileBytes = client.GetByteArrayAsync(uri.AbsoluteUri).Result;

                string html = Encoding.UTF8.GetString(fileBytes);
                List<dynamic> listaArquivos = new List<dynamic>();
                if (!string.IsNullOrEmpty(html))
                {
                    Regex rx = new Regex(@"([A-Z])\w+\.zip");
                    MatchCollection matches = rx.Matches(html);
                    foreach (Match match in matches)
                    {
                        GroupCollection groups = match.Groups;
                        listaArquivos.Add(new { Nome = groups[0].Value, Url = $"{url}/{groups[0].Value}" });
                    }
                    ViewBag.ListaArquivos = listaArquivos.Distinct();
                    ViewBag.UrlValida = listaArquivos.Any();
                    ViewBag.Mensagem = listaArquivos.Any() ? null : ViewBag.Mensagem;
                }

                if (!string.IsNullOrEmpty(acao))
                {
                    startDownload(url);
                }
            }
            catch (Exception)
            {
            }

            return View("Index");
        }

        // POST: DownloadController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DownloadController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: DownloadController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DownloadController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DownloadController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
