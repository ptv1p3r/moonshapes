using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Moonshapes.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Aplicação de teste para a Moonshapes 2018.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Pedro Tiago de Jesus Estevanez Roldan";

            return View();
        }
    }
}