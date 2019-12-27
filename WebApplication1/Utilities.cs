using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using WebApplication1.Models;

namespace WebApplication1
{
    public static class Utilities
    {


        public static string userEmailId;

        static object stamobj = new object();


        //Function to wait before change user status (to stop it after big process)
        public static void Wait(object obj)// the parameter is an empty object (Just to make thread work!)
        {
            EbaysiteEntities dc = new EbaysiteEntities();
            Thread.Sleep(Convert.ToInt32(dc.Setting.FirstOrDefault().UserWaitedTime));
            ChangeUserStatus_WorkNow(2);
        }


        public static void ChangeUserStatus_WorkNow(int u)
        {

            EbaysiteEntities dc = new EbaysiteEntities();
            //Get Current Login Member
            Users Currentuser = dc.Users.FirstOrDefault(x => x.EmailId == userEmailId);
            if (u == 1)
            {
                dc.WorkedUsers.FirstOrDefault(x => x.UserId == Currentuser.UserId).Worked = true;
            }
            else if (u == 2)
            {
                dc.WorkedUsers.FirstOrDefault(x => x.UserId == Currentuser.UserId).Worked = false;
                Debug.WriteLine("Here");
            }
            dc.SaveChanges();
        }




        public static ChromeDriver DriverMaker()
        {

            ChromeOptions chromeOptions = new ChromeOptions();
            //chromeOptions.AddUserProfilePreference("download.default_directory", fullpath);
            /* chromeOptions.AddUserProfilePreference("intl.accept_languages", "nl");
             chromeOptions.AddUserProfilePreference("no-startup-window", "true");
             chromeOptions.AddArgument("start-maximized");
             chromeOptions.AddArgument("--silent");
             chromeOptions.AddArgument("no-sandbox"); 
             //chromeOptions.AddExtensions(Path.GetFullPath("C:\\adblock.crx"));
             
            var outPutDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var relativePath = @"..\..\bin\Debug\BrowserDriver";
            var chromeDriverPath = Path.GetFullPath(Path.Combine(outPutDirectory, relativePath));

            chromeOptions.BinaryLocation = "@c" ;*/


            chromeOptions.AddExtensions(Path.GetFullPath("C:\\Noimg.crx"));
            ChromeDriver driver = new ChromeDriver(chromeOptions);

            return driver;
        }
    }
}