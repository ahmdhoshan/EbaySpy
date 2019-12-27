using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ValuesController : ApiController
    {

        

        private Object thisLock = new Object();
        private Object staam = new Object(); //stam: is an empty object (Just to make thread work!)

        //work on one page thrid function!
        public void PageDriverWork(object args)
        {

            int from, to;

            Array argArray = new object[4];
            argArray = (Array)args;
            string pageurl = (string)argArray.GetValue(0);
            ChromeDriver driver = (ChromeDriver)argArray.GetValue(1);
            from = (int)argArray.GetValue(2);
            to = (int)argArray.GetValue(3);

            try
            {
                //Go To The Page With New Driver!
                driver.Url = pageurl;

                /*  TODO:  MAKE IT A FUNCTION !! */
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
                    //Go To The Product Page
                    driver.Url = ProductsURLAray[i];
                    //Save The Seller Page URL
                    string sellerPageURL = driver.FindElement(By.Id("mbgLink")).GetAttribute("href");
                    sellerPageURL = sellerPageURL.Remove(0, 24); // remove ebay url
                    sellerPageURL = sellerPageURL.Remove(sellerPageURL.Length - 23); //remove the last part of the url

                    //contact with database
                    EbaysiteEntities dc = new EbaysiteEntities();

                    //check if the seller In The Database!
                    if (dc.Seller.FirstOrDefault(x => x.SellerId == sellerPageURL) == null)
                    {
                        //Go to Seller page
                        driver.FindElement(By.XPath(".//*[@id='mbgLink']/span")).Click();
                        //Get Seller Rating!
                        string sellerrate = driver.FindElement(By.XPath("//*[@id='user_info']/div[1]/span[1]/a[2]")).Text;
                        //*[@id="user_info"]/div[1]/span[1]/a[2]
                        Debug.WriteLine(sellerrate);
                        int SellerRateNUMBR; //to convert seller rate from string to int
                        if (Int32.TryParse(sellerrate, out SellerRateNUMBR)) //Check if the convert to int is ok
                            if (SellerRateNUMBR < from || SellerRateNUMBR > to)
                            {
                                Debug.WriteLine("You Don't Need This Seller");
                            }
                            else
                            {
                                lock (thisLock)
                                {
                                    //resolve sinchrone problem
                                    if (dc.Seller.FirstOrDefault(x => x.SellerId == sellerPageURL) == null)
                                    {
                                        Seller seller = new Seller();
                                        seller.SellerId = sellerPageURL;
                                        seller.Rating = SellerRateNUMBR;
                                        seller.AddedDate = DateTime.Now;
                                        //Add The Seller To database:
                                        dc.Seller.Add(seller);
                                        //save changes after add the seller!
                                        dc.SaveChanges();
                                    }
                                }
                                //resolve sinchrone problem
                                if (dc.Seller.FirstOrDefault(x => x.SellerId == sellerPageURL) != null)
                                {
                                    //connect the seller with the User:
                                    Users currentuser = dc.Users.FirstOrDefault(x => x.EmailId == Utilities.userEmailId);
                                    dc.Seller.FirstOrDefault(x => x.SellerId == sellerPageURL).Users.Add(currentuser);
                                    dc.SaveChanges();
                                    Debug.WriteLine("its a good seller! - ADDED TO your list!");
                                }
                            }
                        else
                        {
                            Debug.WriteLine("Error! We Can't Detect Score For This User!");
                        }
                    }
                    else //if the Seller On The Database
                    {
                        Debug.WriteLine("There Is another user spy on this seller! Or it is in blacklist!");
                    }

                }
            }
            catch (Exception) { Utilities.ChangeUserStatus_WorkNow(2); }
            finally
            {
                try { driver.Close(); } catch (Exception) { }
            }
        }
        



        //To resolve a problem
        public void PagesLoopThread(object args)
        {
            Array argArray = new object[5];
            argArray = (Array)args;

            ChromeDriver driver = (ChromeDriver)argArray.GetValue(1);
            int from = (int)argArray.GetValue(2);
            int to = (int)argArray.GetValue(3);
            int cases = (int)argArray.GetValue(4);


            if (cases == 1)
            {
                string[] PageURLAray = (string[])argArray.GetValue(0);
                //Run In One Thread
                for (int j = 0; j < PageURLAray.Length; j++) //Loop For The Pages
                {
                    object argss = new object[4] { PageURLAray[j], driver, from, to };
                    PageDriverWork(argss);
                }
            }
            else if(cases==0)
            {
                string PageURLAray = (string)argArray.GetValue(0);
                object argss = new object[4] { PageURLAray, driver, from, to };
                PageDriverWork(argss);
            }
            else if (cases == 3)
            {
                string[] PageURLAray = (string[])argArray.GetValue(0);
                //Define Array for Chrome Drivers (Driver For Each Page)
                ChromeDriver[] drivers = new ChromeDriver[PageURLAray.Length];
                for (int i = 0; i < drivers.Length; i++) drivers[i] = Utilities.DriverMaker();

                for (int j = 0; j < PageURLAray.Length; j++) //Loop For The Pages
                {
                    //Make a Thread To Work On Page Driver!
                    object argss = new object[5] { PageURLAray[j], drivers[j], from, to, Utilities.userEmailId };
                    Thread b1 = new Thread(new ParameterizedThreadStart(PageDriverWork));
                    b1.Start(argss);
                }
            }
        }





        #region Get Sellers To Spy

        [HttpGet]
        [Route("getsellersapi/{keyword}/{from}/{to}")]
        public string getsellers(string keyword, int from, int to)
        {
            Utilities.userEmailId = HttpContext.Current.User.Identity.Name;
            EbaysiteEntities dc = new EbaysiteEntities();
            Users currentuser = dc.Users.FirstOrDefault(x => x.EmailId == Utilities.userEmailId);
            //Mark as used Keyword:
            if (currentuser.Keywords.FirstOrDefault(x => x.Keywords1 == keyword) == null)
            {
                //check if the keyword in the database and then connect it with the user
                Keywords kws = dc.Keywords.FirstOrDefault(x => x.Keywords1 == keyword);
                if (kws == null)
                {
                    Keywords kw = new Keywords();
                    kw.Keywords1 = keyword;
                    kw.UsedDate = DateTime.Now;
                    currentuser.Keywords.Add(kw);
                }
                else
                {
                    currentuser.Keywords.Add(kws);
                }
                dc.SaveChanges();
            }
            else
            {
                return "This Keyword Used In The Past";
            }
            ChromeDriver driver = Utilities.DriverMaker();
            try
            {
                Thread bb = new Thread(new ParameterizedThreadStart(PagesLoopThread));
                string currentpageurl;
                string[] PageURLAray;
                Utilities.ChangeUserStatus_WorkNow(1);
                driver.Url = "https://www.ebay.com/";
                //find the element
                IWebElement element = driver.FindElement(By.Name("_nkw"));
                element.SendKeys(keyword);
                //Click on Search Button
                driver.FindElement(By.Id("gh-btn")).Click();
                //to resolve one page problem!
                IList<IWebElement> buyitnowbutton = driver.FindElements(By.XPath(".//*[@id='cbelm']/div[1]/div[2]/a[2]"));

                if (buyitnowbutton.Count() == 0)
                {
                    //get the 0 page url
                    currentpageurl = driver.FindElement(By.XPath(".//*[@id='cbelm']/div[1]/div[2]/a[1]")).GetAttribute("href");
                    driver.FindElement(By.XPath(".//*[@id='cbelm']/div[1]/div[2]/a[1]")).Click();
                }
                else
                {
                    currentpageurl = buyitnowbutton[0].GetAttribute("href");
                    //Click on "Buy it now!"
                    buyitnowbutton[0].Click();
                }
                //List For Pages URLS
                IList<IWebElement> listofpageurls = driver.FindElements(By.CssSelector("*[class^='pg ']"));

                if (listofpageurls.Count() > 0)
                {
                    //Bulid an Array To Putt All The Page URLS in (To Solve DOM Problem)
                    PageURLAray = new string[listofpageurls.Count];

                    //Put The Pages URLs in The Array
                    for (int i = 0; i < PageURLAray.Length; i++)
                    {
                        PageURLAray[i] = listofpageurls[i].GetAttribute("href");
                    }
                    //Print The Number Of Pages
                    Debug.WriteLine("Number Of Pages =" + listofpageurls.Count);
                    //SpeedUp the Proceses by make many threads
                    if (dc.Setting.FirstOrDefault().SpeedUpServer == true)
                    {
                        //Run In One Thread
                        object args = new object[5] { PageURLAray, driver, from, to, 3 };
                        bb.Start(args);
                    }
                    else
                    {
                        //Run In One Thread
                        object args = new object[5] { PageURLAray, driver, from, to,  1 };
                        bb.Start(args);
                    }
                }
                else
                {
                    object args = new object[5] { currentpageurl, driver, from, to,  0 };
                    bb.Start(args);
                }
            }
            //If There is any error thin change the status and exit!
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
            }
            finally
            {
                //stam: is an empty object (Just to make thread work!)
                Utilities.Wait(staam);
            }
            return "All Done";
        }


        #endregion

        [HttpGet]
        [Route("addonesellerapi/{sellername}")]
        public string AddsellerToDatabase(string sellername)
        {
            EbaysiteEntities dc = new EbaysiteEntities();
            ChromeDriver driver = Utilities.DriverMaker();
            try
            {
                //If The Seller is not in the database!
                if (dc.Seller.FirstOrDefault(x => x.SellerId == sellername) == null)
                {
                    Seller slr = new Seller();
                    driver.Url = "https://www.ebay.com/" + "usr/" + sellername;
                    //Get Seller Rating!
                    IList<IWebElement> rt = driver.FindElements(By.XPath("//*[@id='user_info']/div[1]/span[1]/a[2]"));
                    if (rt.Count > 0)
                    {
                        string sellerrate = rt[0].Text;
                        int SellerRateNUMBR; //to convert seller rate from string to int
                        if (Int32.TryParse(sellerrate, out SellerRateNUMBR))
                        {
                            slr.Rating = SellerRateNUMBR;
                        }
                        else { return "Can't Add Seller!"; }
                    }
                    else { return "Can't Add Seller!"; }
                    slr.SellerId = sellername;
                    slr.AddedDate = DateTime.Now;
                    //Add The Seller To the user!
                    dc.Users.FirstOrDefault(x => x.EmailId == HttpContext.Current.User.Identity.Name).Seller.Add(slr);
                    dc.SaveChanges();
                }
                else
                {
                    return "There is anothe User Spy On This Seller";
                }
            }
            catch (Exception)
            {
                return "Can't Add Seller!";
            }
            finally
            {
                try { driver.Close(); } catch (Exception) { }
            }
            return "User Added To Database!";
        }


      



        [HttpGet]
        [Route("GetSiteSetting/{SpeedUpServer}/{WaitedTime}/{HomeAd}/")]
        public string GetSiteSetting(bool SpeedUpServer, float WaitedTime, string HomeAd)
        {
            EbaysiteEntities dc = new EbaysiteEntities();
            dc.Setting.FirstOrDefault().UserWaitedTime = WaitedTime;
            dc.Setting.FirstOrDefault().SpeedUpServer = SpeedUpServer;
            dc.Setting.FirstOrDefault().IndexAd = HomeAd;
            try
            {
                dc.SaveChanges();
            }
            catch (Exception) { return "Can't Update Database";  }
            return "Database Updated";
        }


    }


}
