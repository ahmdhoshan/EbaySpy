using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            EbaysiteEntities db = new EbaysiteEntities();
            ViewBag.Title = "Ebay Spy";
            try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            ViewData["import1"] = Request.Url.AbsoluteUri + "Control/Every24HoursProductsSpy";
            return View(db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name));
        }


        public ActionResult AdminPanell()
        {
            EbaysiteEntities db = new EbaysiteEntities();
            ViewBag.Title = "Admin Panel";
            return View(db.Setting.FirstOrDefault());
        }




        public ActionResult Prices()
        {
            ViewBag.Title = "Prices";
            return View();
        }


        public ActionResult ContactUs()
        {
            ViewBag.Title = "Contact Us";
            return View();
        }


    }
}
