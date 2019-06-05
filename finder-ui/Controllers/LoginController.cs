using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using finder_ui.Models;
using finder_ui.UserProfileServiceReference;

namespace finder_ui.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            var viewModel = new LoginViewModel
            {
                // This is the controller and action to where you are sent after login
                // If no Controller or Action is found in session 
                // You are sent to Services.MyServices
                Controller = TempData["ReturnToController"]?.ToString() ?? "Service",
                Action = TempData["ReturnToAction"]?.ToString() ?? "MyServices",
            };
            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel vm)
        {
            string loginEmail;
            User user;
            using (var profileClient = new UserProfileServiceReference.UserProfileServiceClient())
            {
                user = profileClient.GetUserByUserNameOrEmail(vm.username);

                // use user.Email for login, but...
                // if for some reason there is no user stored in profileservice then
                // No user is found (user==null). If so then use vm.username for login
                loginEmail = user?.Email ?? vm.username;


                using (var loginClient = new UserLoginServiceReference.LoginServiceClient())
                {
                    if (!loginClient.UserLogin(loginEmail, vm.userPassword ?? ""))
                    {
                        Session["AuthorizedAsUser"] = "false";
                        Session["UserID"] = null;
                        ModelState.AddModelError("", "inloggningen misslyckades.");
                        return View(vm);
                    }


                    // Login successful!
                    // if user==null then there is no profile object present in UserProfileServiceReference
                    // So we call UserProfileServiceReference.UpdateUser() to make one
                    if (user == null)
                    {
                        var loginUser = loginClient.GetUserById(loginClient.GetUserId(loginEmail));
                        user = new User
                        {
                            Id = loginUser.ID,
                            Email = loginUser.Email,
                            Username = loginUser.Username,
                            Name = loginUser.Firstname,
                            Surname = loginUser.Surname,
                        };
                        profileClient.UpdateUser(user);
                    }
                }
            }
            Session["AuthorizedAsUser"] = "true";
            Session["UserID"] = user.Id;
            return RedirectToAction(vm.Action, vm.Controller);
        }
    }
}
