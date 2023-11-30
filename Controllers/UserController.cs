using DapperExtensions;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace AplicationSignalR.Controllers
{
    public class UserController : Controller
    {
        private IDbConnection dbConnection;

        public UserController(IDbConnection _dbConnection)
        {
            this.dbConnection = _dbConnection;
            dbConnection.Open();
        }
        protected override void Dispose(bool disposing)
        {
            dbConnection.Dispose();
        }

        // GET: UserController
        public ActionResult Index()
        {
            ViewData.Model = dbConnection.GetList<Models.Usuarios>() ?? new List<Models.Usuarios>();
            return View();
        }

        // GET: UserController/Details/5
        public ActionResult Details(int id)
        {
            ViewData.Model = dbConnection.Get<Models.Usuarios>(id);

            return View();
        }

        // GET: UserController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.Usuarios user)
        {
            try
            {
                ViewData.Model = dbConnection.Insert<Models.Usuarios>(user);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/Edit/5
        public ActionResult Edit(int id)
        {
            ViewData.Model = dbConnection.Get<Models.Usuarios>(id);
            return View();
        }

        //public ActionResult Edit(int id, Models.User user)
        // POST: UserController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Models.Usuarios user)
        {
            try
            {
                ViewData.Model = dbConnection.Update<Models.Usuarios>(user);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/Delete/5
        public ActionResult Delete(int id)
        {
            ViewData.Model = dbConnection.Get<Models.Usuarios>(id);

            return View();
        }

        // POST: UserController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Models.Usuarios user)
        {
            try
            {
                ViewData.Model = dbConnection.Delete<Models.Usuarios>(user);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
