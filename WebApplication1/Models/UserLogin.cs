using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace WebApplication1.Models
{
    public class UserLogin
    {
        [Display(Name = "Email ID")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email Id Required")]
    public string EmailId { set; get; }

        [Required(AllowEmptyStrings =false,ErrorMessage ="Password Requird")]
        [DataType(DataType.Password)]
        public string Password { set; get; }

        [Display(Name = "Remember Me")]
        public bool Rememberme { set; get; }
    }
}