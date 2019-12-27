using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class SellersController : Controller
    {
        // GET: Sellers

        EbaysiteEntities db = new EbaysiteEntities();

        public ActionResult Index()
        {
            return View(db.Seller);
        }
    }
}