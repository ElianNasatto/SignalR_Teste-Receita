using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Controllers
{
    public class LogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
