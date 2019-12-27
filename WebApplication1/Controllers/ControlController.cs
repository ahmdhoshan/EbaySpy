using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

using System.Web.Mvc;
using System.Web.Http;
using WebApplication1.Models;
using System.Threading;
using OpenQA.Selenium.Chrome;
using System.Text;
using System.Reflection;
using PagedList;

namespace WebApplication1.Controllers
{
    
    public class ControlController : Controller
    {

        #region Spy On Sellers On Database


        private void OnePageSearchThread(object args)
        {

            EbaysiteEntities dc = new EbaysiteEntities();
            ChromeDriver driver = Utilities.DriverMaker();
            Array argArray = new object[2];
            argArray = (Array)args;
            string pageurl = (string)argArray.GetValue(0);
            string sellerid = (string)argArray.GetValue(1);
            //Go to Seller Page
            driver.Url = pageurl;

            //List for Products in the Page
            IList<IWebElement> productsinpage = driver.FindElements(By.ClassName("vip"));

            //Bulid an Array To Putt All The Prdoucts URLS in (To Solve DOM Problem)
            string[] ProductsURLAray = new string[productsinpage.Count];
            //Put The Products URLs in The Array
            for (int i = 0; i < ProductsURLAray.Length; i++)
            {
                ProductsURLAray[i] = productsinpage[i].GetAttribute("href");
            }

            for (int i = 0; i < ProductsURLAray.Length; i++) //Loop For The Products Inside The Page
            {
                driver.Url = ProductsURLAray[i];

                int soldnumber = GetProductSoldNumber(driver);
                //Add The Product To The Database
                Product prod = new Product();
                string prodId = ProductsURLAray[i].Remove(0, 24); // remove ebay url
                prod.soldarray = soldnumber.ToString() + ",";
                prod.Title = driver.FindElement(By.XPath(".//*[@id='itemTitle']")).Text;
                prod.ProductURL = prodId;
                prod.SellerId = sellerid;
                prod.AddedDate = DateTime.Now;

                dc.Product.Add(prod);
                try
                {
                    dc.SaveChanges();
                }
                // check database errors
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            Debug.WriteLine("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                        }
                    }
                }

            }
            driver.Close();
        }





        private void OneSellerPageSpy_Thread(object args)
        {
            EbaysiteEntities dc = new EbaysiteEntities();
            ChromeDriver driver = Utilities.DriverMaker();
            Array argArray = new object[1];
            argArray = (Array)args;
            string sellerid = (string)argArray.GetValue(0);

            string sellerpageurl = "https://www.ebay.com/usr/" + sellerid;

            //Go to Seller Page
            driver.Url = sellerpageurl;

            //Go to "Items For Sale"
            driver.FindElement(By.XPath(".//*[@id='shortcuts']/span[1]/span/a")).Click();

            //get the 0 page url
            string currentpageurl = driver.FindElement(By.XPath(".//*[@id='cbelm']/div[1]/div[2]/a[2]")).GetAttribute("href");

            //Click on "Buy it now!"
            driver.FindElement(By.XPath(".//*[@id='cbelm']/div[1]/div[2]/a[2]")).Click();
            /*  TODO:  MAKE IT A FUNCTION !! */

            //List For Pages URLS
            IList<IWebElement> listofpageurls = driver.FindElements(By.CssSelector("*[class^='pg ']"));


            if(listofpageurls.Count()>0)
            {

            //Bulid an Array To Putt All The Page URLS in (To Solve DOM Problem)
            string[] PageURLAray = new string[listofpageurls.Count];

            //Put The Pages URLs in The Array
            for (int i = 0; i < PageURLAray.Length; i++)
            {
                PageURLAray[i] = listofpageurls[i].GetAttribute("href");
            }
            //Print The Number Of Pages
            Debug.WriteLine("Number Of Pages =" + listofpageurls.Count);

            if (dc.Setting.FirstOrDefault().SpeedUpServer == true)
            {
                for (int j = 0; j < listofpageurls.Count; j++) //Loop For The Pages
                {
                    //Make a Thread To Work On Page Driver!
                    object argss = new object[2] { PageURLAray[j], sellerid };
                    Thread b1 = new Thread(new ParameterizedThreadStart(OnePageSearchThread));
                    b1.Start(argss);
                }
            }

            else
            {
                for (int j = 0; j < PageURLAray.Length; j++) //Loop For The Pages
                {
                    //Make a Thread To Work On Page Driver!
                    object argss = new object[2] { PageURLAray[j], sellerid };
                    OnePageSearchThread(argss);
                }
            }

            }

            else
            {
                //Make a Thread To Work On Page Driver!
                object argss = new object[2] { currentpageurl, sellerid };
                Thread b1 = new Thread(new ParameterizedThreadStart(OnePageSearchThread));
                b1.Start(argss);

            }

            driver.Close();
        }

        #endregion




        #region Scheduled Functions!

        public ActionResult Every24HoursProductsSpy()
        {
            EbaysiteEntities db = new EbaysiteEntities();
            ChromeDriver driver = Utilities.DriverMaker();

            try
            {
                foreach (var slr in db.Seller)
                {
                    //Check If We Are Spy On This Seller!
                    if (slr.Spy == 1)
                    {
                        foreach (var item in slr.Product)
                        {
                            List<string> soldslist = item.soldarray.Split(',').ToList<string>();
                            //Remove The space from list
                            soldslist.RemoveAt(soldslist.Count - 1);
                            int currsoldnum=0;



                            driver.Url = "https://www.ebay.com/itm" + item.ProductURL;

                            //Try 3 Times To get The Sold Number (if there is an exception)
                            for (int i = 0; i < 3; i++)
                            {
                                currsoldnum = GetProductSoldNumber(driver);
                                if (currsoldnum != -1) break;
                            }

                            int finalsoldnumber = currsoldnum;

                            /*
                            //If The Sales In Product Reduced
                            if (Convert.ToInt32(soldslist[soldslist.Count - 1]) > currsoldnum)
                            {
                                if (item.repeatProductSold == null)
                                {
                                    item.repeatProductSold =
                                        Convert.ToInt32(soldslist[soldslist.Count - 1]);
                                    finalsoldnumber = currsoldnum + Convert.ToInt32(soldslist[soldslist.Count - 1]);
                                }
                                else
                                {
                                    finalsoldnumber = currsoldnum + (int)item.repeatProductSold;
                                }
                            }
                            */
                            if (soldslist.Count > 30)
                            {
                                soldslist.RemoveAt(0);
                                soldslist.Add(finalsoldnumber.ToString());
                                item.soldarray = String.Join(",", soldslist);
                                item.soldarray += ",";
                            }
                            else
                            {
                                soldslist.Add(currsoldnum.ToString());
                                item.soldarray += currsoldnum.ToString() + ",";
                            }

                            if (soldslist.Count == 1)
                            {
                                item.Last_7_Days_Sold = item.Last_30_Days_Sold = item.Last_Day_Sold_Number
                                    = Convert.ToInt32(soldslist[0]);
                            }
                            else
                            {

                                item.Last_Day_Sold_Number = Convert.ToInt32(soldslist[soldslist.Count - 1]) - Convert.ToInt32(soldslist[soldslist.Count - 2]);
                                item.Last_30_Days_Sold = Convert.ToInt32(soldslist[soldslist.Count - 1]) - Convert.ToInt32(soldslist[0]);
                                if (soldslist.Count >= 7)
                                {
                                    item.Last_7_Days_Sold = Convert.ToInt32(soldslist[soldslist.Count - 1]) - Convert.ToInt32(soldslist[soldslist.Count - 7]);
                                }
                                else
                                {
                                    item.Last_7_Days_Sold = Convert.ToInt32(soldslist[soldslist.Count - 1]) - Convert.ToInt32(soldslist[0]);
                                }
                            }
                        }


                    }
                }

            }
            catch (Exception) { }
            finally
            {
                try { db.SaveChanges(); }
                catch (Exception) { }

                //Get Total Solds!
                foreach (var slr in db.Seller)
                {
                    //Check If We Are Spy On This Seller!
                    if (slr.Spy == 1)
                    {


                        int? Last30DaysSales = (from od in db.Product
                                                where od.SellerId == slr.SellerId
                                                select od.Last_30_Days_Sold).Sum();
                        int? Last7DaysSales = (from od in db.Product
                                               where od.SellerId == slr.SellerId
                                               select od.Last_7_Days_Sold).Sum();
                        int? Last1DaysSales = (from od in db.Product
                                               where od.SellerId == slr.SellerId
                                               select od.Last_Day_Sold_Number).Sum();


                        slr.TotalSoldsInLastMonth = Last30DaysSales;
                        slr.TotalSoldsInLast7Days = Last7DaysSales;
                        slr.TotalSoldsInLast1Days = Last1DaysSales;
                    }

                }

                try { db.SaveChanges(); }
                catch (Exception) { }



                driver.Close();  
            }

            return View();

        }


        #endregion



        #region Buttons In Views Functions


        /*******    Products : Move To My_Products      */
        [System.Web.Http.HttpPost]
        public ActionResult MoveToMyProducts(int id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            db.Product.FirstOrDefault(x => x.ProductId == id).Status = 1;
            db.SaveChanges();
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }


        /*******    MyProducts : Remove From My_Products      */
        [System.Web.Http.HttpPost]
        public ActionResult RemoveFromMyProducts(int id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            db.Product.FirstOrDefault(x => x.ProductId == id).Status = null;
            db.SaveChanges();
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }





        /*******    Seller List : Spy      */
        [System.Web.Http.HttpPost]
        public ActionResult SpyOneSeller_ViewFunc(string id)
        {
            EbaysiteEntities db = new EbaysiteEntities();

            Users Currentuser = db.Users.FirstOrDefault(x => x.EmailId == Utilities.userEmailId);


           
            if(Currentuser.WorkedUsers.Worked==true)
            {
                TempData["testmsg"] = "You Cant send Much Requests To The Server, Please Try Again Later!";
                return Redirect(Request.UrlReferrer.PathAndQuery);
            }


            //If we spied on this seller on the past and removed the spy do this!
            if ((db.Seller.FirstOrDefault(x => x.SellerId == id)).Spy == 2)
            {
                (db.Seller.FirstOrDefault(x => x.SellerId == id)).Spy = 1;
                db.SaveChanges();
                return Redirect(Request.UrlReferrer.PathAndQuery);
            }
            Utilities.ChangeUserStatus_WorkNow(1);

            object args = new object[1] { id };
            Thread b1 = new Thread(new ParameterizedThreadStart(OneSellerPageSpy_Thread));
            b1.Start(args);

            /* 0: Not spied
             * 1: Spied For the First time
             * 2: Spy Removed
             * */


            (db.Seller.FirstOrDefault(x => x.SellerId == id)).Spy = 1;
            db.SaveChanges();


            Thread b2 = new Thread(new ParameterizedThreadStart(Utilities.Wait));
            b2.Start();


            return Redirect(Request.UrlReferrer.PathAndQuery);
        }



        /*******    Seller List : Unspy      */
        [System.Web.Http.HttpPost]
        public ActionResult UnSpyOneSeller_ViewFunc(string id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            (db.Seller.FirstOrDefault(x => x.SellerId == id)).Spy = 2;
            db.SaveChanges();
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }


        /*******    SellersBlackList : Remove      */

       [System.Web.Http.HttpPost]
        public ActionResult RemoveFromBlackList(string id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            var slr = db.Seller.Where(x => x.SellerId == id).SingleOrDefault();
            if (slr != null)
            {
                slr.BlackList = null;
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }


        /*******    SellersList : Remove      */

        [System.Web.Http.HttpPost]
        public ActionResult MoveToBlackList(string id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            var slr = db.Seller.Where(x => x.SellerId == id).SingleOrDefault();
            if (slr != null)
            {
                slr.BlackList = true;
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }




        /*******    Seller List : Delete      */

        [System.Web.Http.HttpPost]
        public ActionResult Delete(string id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            var slr = db.Seller.Where(x => x.SellerId == id).SingleOrDefault();
            if (slr != null)
            {
                db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).Seller.Remove(slr);
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }





        /*******    Products : Delete      */

        [System.Web.Http.HttpPost]
        public ActionResult DeleteProd(int id)
        {
            EbaysiteEntities db = new EbaysiteEntities();
            var prod = db.Product.Where(x => x.ProductId == id).SingleOrDefault();
            if (prod != null)
            {
                db.Product.Remove(prod);
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.PathAndQuery);
        }



        #endregion




        #region View Functions


        public ActionResult Index()
        {
            return View();
        }



        /****  UsedKeywords *******/

        public ActionResult UsedKeywords(string sortOrder, int? page)
        {
            
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortParm1 = String.IsNullOrEmpty(sortOrder) ? "SortDate" : "";
            ViewBag.SortParmDate = sortOrder == "SortDate" ? "SortDate_desc" : "SortDate";
            EbaysiteEntities db = new EbaysiteEntities();
                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            var query = from keyword in db.Keywords
                        where keyword.Users.Any(c => c.EmailId == User.Identity.Name)
                        select keyword;

            switch (sortOrder)
            {
                case "SortDate":
                    query = query.OrderBy(s => s.UsedDate);
                    break;
                case "SortDate_desc":
                    query = query.OrderByDescending(s => s.UsedDate);
                    break;
                default:
                    query = query.OrderByDescending(s => s.UsedDate);
                    break;
            }


            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(query.ToPagedList(pageNumber, pageSize));
        }






        /****  SellersBlackList *******/

        public ActionResult SellersBlackList(string sortOrder, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortParm1 = String.IsNullOrEmpty(sortOrder) ? "SortRating" : "";
            ViewBag.SortParmRate = sortOrder == "SortRating" ? "SortRating_desc" : "SortRating";
            ViewBag.SortParmDate = sortOrder == "SortDate" ? "SortDate_desc" : "SortDate";
            EbaysiteEntities db = new EbaysiteEntities();
                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            var query = from seler in db.Seller
                        where seler.Users.Any(c => c.EmailId == User.Identity.Name)
                        where seler.BlackList == true
                        select seler;
            switch (sortOrder)
            {
                case "SortRating":
                    query = query.OrderBy(s => s.Rating);
                    break;
                case "SortDate":
                    query = query.OrderBy(s => s.AddedDate);
                    break;
                case "SortRating_desc":
                    query = query.OrderByDescending(s => s.Rating);
                    break;
                case "SortDate_desc":
                    query = query.OrderByDescending(s => s.AddedDate);
                    break;
                default:
                    query = query.OrderByDescending(s => s.AddedDate);
                    break;
            }


            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(query.ToPagedList(pageNumber, pageSize));
        }








        /****  getsellers  *******/

        public ActionResult getsellers()
        {
            ViewBag.Title = "Home Page";

            Utilities.userEmailId = User.Identity.Name;

            EbaysiteEntities db = new EbaysiteEntities();
                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            //Check if the user in "WorkedUsers" Table
            if (db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).WorkedUsers==null)
            {
                WorkedUsers ws = new WorkedUsers();
                ws.UserId = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).UserId;
                ws.Worked = false;
                db.WorkedUsers.Add(ws);
                db.SaveChanges();
            }
   
            return View(db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name));
        }

        /****  Products  *******/
        public ActionResult Products(string sortOrder, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortParm1 = String.IsNullOrEmpty(sortOrder) ? "Sort1" : "";
            ViewBag.SortParm7 = sortOrder == "Sort1" ? "Sort1_desc" : "Sort1";
            ViewBag.SortParm7 = sortOrder == "Sort7" ? "Sort7_desc" : "Sort7";
            ViewBag.SortParm30 = sortOrder == "Sort30" ? "Sort30_desc" : "Sort30";
            ViewBag.SortParmDate = sortOrder == "SortDate" ? "SortDate_desc" : "SortDate";
            EbaysiteEntities db = new EbaysiteEntities();
                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            var query = from seler in db.Seller
                        where seler.Users.Any(c => c.EmailId == User.Identity.Name)
                        where seler.Spy.Equals(1)
                        where seler.BlackList==null
                        select seler;

         
            var query2 = from prdct in db.Product
                         join s in query
                         on prdct.SellerId equals s.SellerId
                         where prdct.Status==null
                         select prdct;
            switch (sortOrder)
            {
                case "Sort7":
                    query2 = query2.OrderBy(s => s.Last_7_Days_Sold);
                    break;
                case "Sort30":
                    query2 = query2.OrderBy(s => s.Last_30_Days_Sold);
                    break;
                case "SortDate":
                    query2 = query2.OrderBy(s => s.AddedDate);
                    break;
                case "SortDate_desc":
                    query2 = query2.OrderByDescending(s => s.AddedDate);
                    break;
                case "Sort7_desc":
                    query2 = query2.OrderByDescending(s => s.Last_7_Days_Sold);
                    break;
                case "Sort30_desc":
                    query2 = query2.OrderByDescending(s => s.Last_30_Days_Sold);
                    break;
                case "Sort1":
                    query2 = query2.OrderBy(s => s.Last_Day_Sold_Number);
                    break;
                case "Sort1_desc":
                    query2 = query2.OrderByDescending(s => s.Last_Day_Sold_Number);
                    break;
                default:
                    query2 = query2.OrderByDescending(s => s.Last_Day_Sold_Number);
                    break;
            }


            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(query2.ToPagedList(pageNumber, pageSize));
        }



        /****  MyProducts  *******/
        public ActionResult MyProducts(string sortOrder, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortParm1 = String.IsNullOrEmpty(sortOrder) ? "Sort1" : "";
            ViewBag.SortParm7 = sortOrder == "Sort1" ? "Sort1_desc" : "Sort1";
            ViewBag.SortParm7 = sortOrder == "Sort7" ? "Sort7_desc" : "Sort7";
            ViewBag.SortParm30 = sortOrder == "Sort30" ? "Sort30_desc" : "Sort30";
            ViewBag.SortParmDate = sortOrder == "SortDate" ? "SortDate_desc" : "SortDate";
            EbaysiteEntities db = new EbaysiteEntities();
                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            var query = from seler in db.Seller
                        where seler.Users.Any(c => c.EmailId == User.Identity.Name)
                        where seler.Spy.Equals(1)
                        where seler.BlackList == null
                        select seler;
            var query2 = from prdct in db.Product
                         join s in query
                         on prdct.SellerId equals s.SellerId
                         where prdct.Status == 1
                         select prdct;
            switch (sortOrder)
            {
                case "Sort7":
                    query2 = query2.OrderBy(s => s.Last_7_Days_Sold);
                    break;
                case "Sort30":
                    query2 = query2.OrderBy(s => s.Last_30_Days_Sold);
                    break;
                case "SortDate":
                    query2 = query2.OrderBy(s => s.AddedDate);
                    break;
                case "SortDate_desc":
                    query2 = query2.OrderByDescending(s => s.AddedDate);
                    break;
                case "Sort7_desc":
                    query2 = query2.OrderByDescending(s => s.Last_7_Days_Sold);
                    break;
                case "Sort30_desc":
                    query2 = query2.OrderByDescending(s => s.Last_30_Days_Sold);
                    break;
                case "Sort1":
                    query2 = query2.OrderBy(s => s.Last_Day_Sold_Number);
                    break;
                case "Sort1_desc":
                    query2 = query2.OrderByDescending(s => s.Last_Day_Sold_Number);
                    break;
                default:
                    query2 = query2.OrderByDescending(s => s.Last_Day_Sold_Number);
                    break;
            }

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(query2.ToPagedList(pageNumber, pageSize));
        }










        /****  SellersList  *******/

        public ActionResult SellersList(string sortOrder, int? page)
        {
            Utilities.userEmailId = User.Identity.Name;
            EbaysiteEntities db = new EbaysiteEntities();


            //Check if the user in "WorkedUsers" Table
            if (db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).WorkedUsers == null)
            {
                WorkedUsers ws = new WorkedUsers();
                ws.UserId = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).UserId;
                ws.Worked = false;
                db.WorkedUsers.Add(ws);
                db.SaveChanges();
            }


            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortParm1 = String.IsNullOrEmpty(sortOrder) ? "SortRating" : "";
            ViewBag.SortParmRate = sortOrder == "SortRating" ? "SortRating_desc" : "SortRating";
            ViewBag.SortParmDate = sortOrder == "SortDate" ? "SortDate_desc" : "SortDate";
            ViewBag.SortParm30 = sortOrder == "Sort30Sales" ? "Sort30Sales_desc" : "Sort30Sales";
            ViewBag.SortParm7 = sortOrder == "Sort7Sales" ? "Sort7Sales_desc" : "Sort7Sales";
            ViewBag.SortParm1 = sortOrder == "Sort1Sales" ? "Sort1Sales_desc" : "Sort1Sales";
            ViewBag.SortParmSpy = sortOrder == "SortSpy" ? "SortSpy_desc" : "SortSpy";

                        try{ViewData["name"] = db.Users.FirstOrDefault(x => x.EmailId == User.Identity.Name).FirsName;} catch(Exception){}
            var query = from seler in db.Seller
                        where seler.Users.Any(c => c.EmailId == User.Identity.Name)
                        where seler.BlackList == null
                        select seler;
            switch (sortOrder)
            {
                case "SortSpy":
                    query = query.OrderBy(s => s.Spy);
                    break;
                case "SortRating":
                    query = query.OrderBy(s => s.Rating);
                    break;
                case "SortDate":
                    query = query.OrderBy(s => s.AddedDate);
                    break;
                case "Sort30Sales":
                    query = query.OrderBy(s => s.TotalSoldsInLastMonth);
                    break;
                case "Sort7Sales":
                    query = query.OrderBy(s => s.TotalSoldsInLast7Days);
                    break;
                case "Sort1Sales":
                    query = query.OrderBy(s => s.TotalSoldsInLast1Days);
                    break;
                case "SortRating_desc":
                    query = query.OrderByDescending(s => s.Rating);
                    break;
                case "SortSpy_desc":
                    query = query.OrderByDescending(s => s.Spy);
                    break;
                case "SortDate_desc":
                    query = query.OrderByDescending(s => s.AddedDate);
                    break;
                case "Sort30Sales_desc":
                    query = query.OrderByDescending(s => s.TotalSoldsInLastMonth);
                    break;
                case "Sort7Sales_desc":
                    query = query.OrderByDescending(s => s.TotalSoldsInLast7Days);
                    break;
                case "Sort1Sales_desc":
                    query = query.OrderByDescending(s => s.TotalSoldsInLast1Days);
                    break;
                default:
                    query = query.OrderByDescending(s => s.AddedDate);
                    break;
            }
            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(query.ToPagedList(pageNumber, pageSize));
        }


        #endregion




        #region Help Functions




        [System.Web.Http.NonAction]
        private int GetProductSoldNumber(ChromeDriver driver)
        {
            try
            {
                IList<IWebElement> slodnumber = driver.FindElements(By.CssSelector("*[class^='vi-qtyS-hot-red  vi-qty-vert-algn vi-qty-pur-lnk']"));
                IList<IWebElement> slodnumber1 = driver.FindElements(By.CssSelector("*[class^='vi-qtyS-hot-red  vi-bboxrev-dsplblk vi-qty-vert-algn vi-qty-pur-lnk']"));
                IList<IWebElement> slodnumber2 = driver.FindElements(By.CssSelector("*[class^='vi-qtyS  vi-bboxrev-dsplblk vi-qty-vert-algn vi-qty-pur-lnk']"));
                IList<IWebElement> slodnumber3 = driver.FindElements(By.CssSelector("*[class^='vi-qtyS-hot  vi-qty-vert-algn vi-qty-pur-lnk']"));
                IList<IWebElement> slodnumber4 = driver.FindElements(By.CssSelector("*[class^='vi-qtyS  vi-qty-vert-algn vi-qty-pur-lnk']"));


                string NotclearSoldNumber = "";
                if (slodnumber.Count != 0)
                {
                    NotclearSoldNumber = slodnumber[0].FindElement(By.CssSelector("a")).GetAttribute("innerText");
                }
                else if (slodnumber1.Count != 0)
                {
                    NotclearSoldNumber = slodnumber1[0].FindElement(By.CssSelector("a")).GetAttribute("innerText");
                }
                else if (slodnumber2.Count != 0)
                {
                    NotclearSoldNumber = slodnumber2[0].FindElement(By.CssSelector("a")).GetAttribute("innerText");
                }
                else if (slodnumber3.Count != 0)
                {
                    NotclearSoldNumber = slodnumber3[0].FindElement(By.CssSelector("a")).GetAttribute("innerText");
                }
                else if (slodnumber4.Count != 0)
                {
                    NotclearSoldNumber = slodnumber4[0].FindElement(By.CssSelector("a")).GetAttribute("innerText");
                }
                if (NotclearSoldNumber.Length != 0)
                {
                    string soldnumberclear = Regex.Replace(NotclearSoldNumber, "[^0-9.]", ""); //remove "sold" word
                    int finalsoldnumber;
                    if (Int32.TryParse(soldnumberclear, out finalsoldnumber)) return finalsoldnumber; //check if the convert to int is ok
                }
            }
            catch (Exception e) {
                Debug.WriteLine("Ahmd:Error" + e.Message);
                return -1;
            }

            return 0;
        }


        #endregion


    }
}