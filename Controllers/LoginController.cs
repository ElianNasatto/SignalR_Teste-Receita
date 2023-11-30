using AplicationSignalR.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using WebApplication3.Controllers;
using DapperExtensions;
using DapperExtensions.Predicate;

namespace AplicationSignalR.Controllers
{
    [Authorize]
    public class LoginController : Controller
    {
        private IDbConnection dbConnection;

        public LoginController(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }
        protected override void Dispose(bool disposing)
        {
            dbConnection.Dispose();
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public ActionResult Logout()
        {
            HttpContext.SignOutAsync();
            //Vi que precisa implementar um blacklist com o uso do jwt
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(string Nome)
        {
            try
            {
                if (string.IsNullOrEmpty(Nome))
                    return RedirectToAction(nameof(Index));

                var predicate = Predicates.Field<Usuarios>(f => f.Nome, Operator.Like, $"%{Nome}%");
                IEnumerable<Usuarios> users = dbConnection.GetListAutoMap<Usuarios>(predicate);
                if (users != null && users.Count() > 0)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, users.First().Nome),
                        new Claim(ClaimTypes.Sid, users.First().Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        AllowRefresh = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                        IsPersistent = true,
                    };

                   await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction(nameof(HomeController.Index),"Home");
                }
                else
                {
                    ViewBag.mensagem = $"Usuário/senha incorretos.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception)
            {
                ViewBag.mensagem = $"Usuário/senha incorretos.";
                return RedirectToAction(nameof(Index));
            }
        }

        [AllowAnonymous]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.Usuarios user)
        {
            try
            {
                ViewData.Model = dbConnection.Insert<Usuarios>(user);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
