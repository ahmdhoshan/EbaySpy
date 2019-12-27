using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {

        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }


        //Registration post action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration(Users user)
        {
            bool Status = false;
            string message = "";
            //Model Vailidation
            if(ModelState.IsValid)
            {
                //Email is already  exist
                var isExist = IsEmailExest(user.EmailId);
                if(isExist)
                {
                    ModelState.AddModelError("EmailExist","Email Already Exist");
                    return View(user);
                }



                #region Password Hashing
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);



                #endregion


                #region  Save Data to database

                using (EbaysiteEntities dc = new EbaysiteEntities())
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();

                    Status = true;
                }

                #endregion

            }
            else
            {
                message = "Invalid Request";
            }
            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }


        //Login

            [HttpGet]
      public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl = "")
        {
            string message = "";
            using (EbaysiteEntities dc = new EbaysiteEntities())
            {
                var v = dc.Users.Where(a => a.EmailId == login.EmailId).FirstOrDefault();
                if (v != null)
                {
                    if (string.Compare(Crypto.Hash(login.Password), v.Password) == 0)
                    {
                        int timeout = login.Rememberme ? 525600 : 20; // 525600 min = 1 year
                        var ticket = new FormsAuthenticationTicket(login.EmailId, login.Rememberme, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);


                        if (Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        message = "Invalid credential provided";
                    }
                }
                else
                {
                    message = "Invalid credential provided";
                }
            }
            ViewBag.Message = message;
            return View();
        }



        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");
        }


        // GET: User
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }




        [NonAction]
        public bool IsEmailExest(string EmailID)
        {
            using (EbaysiteEntities dc = new EbaysiteEntities())
            {
                var v = dc.Users.Where(a => a.EmailId == EmailID).FirstOrDefault();
                return v == null ? false : true;
            }
        }



        public ActionResult Setting()
        {
            return View();
        }








    }


}