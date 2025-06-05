using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MBS.Common;
using MBS.DataCache;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using System.Security.Cryptography;
using System.Collections.Generic;
using MBS.WebApiService;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IAccountRepository _repository;
        private readonly Guid _userId = Guid.Empty;
        
        private static string _serverDownMessage = "We had experienced technical issues with the system, please come back later or contact support!";

        private int _timeZoneOffset = -6;

        // If you are using Dependency Injection, you can delete the following constructor
        public AccountController() : this(new AccountRepository())
        {
        }

        public AccountController(IAccountRepository repository)
        {
            _repository = repository;
            var user = Membership.GetUser();
            if (user != null)
            {
                _userId = new Guid(user.ProviderUserKey.ToString());
            }

            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
        }

        #region Login Methods

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {            
            var model = new UserManagementViewModel();

            ViewBag.ReturnUrl = returnUrl;

            if (HttpContext.Request.Cookies["LastEnterServiceDate"] != null)
            {
                var c = new HttpCookie("LastEnterServiceDate");
                c.Expires = DateTime.UtcNow.AddHours(_timeZoneOffset).AddDays(-1);
                HttpContext.Response.Cookies.Add(c);
            }

            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {                    
                    if (Membership.ValidateUser(model.UserName, model.Password))
                    {
                        FormsAuthentication.SetAuthCookie(model.UserName.ToLower(), true);

                        if (Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }                        
                    }
                    else
                    {
                        ModelState.AddModelError("", "The user name or password provided is incorrect or your account is locked.");
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, _serverDownMessage);
                }
            }
            else
            {
                ModelState.AddModelError("", "The user name or password provided is incorrect.");
            }

            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        public ActionResult LogOff()
        {
            var cookieNames = new[] { "LastSelectedDiagCode", "LastEnterServiceDate" };

            foreach (var cookieName in cookieNames)
            {
                HttpCookie myCookie = new HttpCookie(cookieName);
                myCookie.Expires = DateTime.Now.AddDays(-1d);
                Response.Cookies.Add(myCookie);
            }

            //if (Request.Cookies["MyCookie"] != null)
            //{
            //    var c = new HttpCookie("MyCookie");
            //    c.Expires = DateTime.Now.AddDays(-1);
            //    Response.Cookies.Add(c);
            //}
            //if (HttpContext.Request.Cookies[".AspNet.ApplicationCookie"] != null)
            //{
            //    var c = new HttpCookie(".AspNet.ApplicationCookie");
            //    c.Expires = DateTime.Now.AddDays(-1);
            //    Response.Cookies.Add(c);
            //}

            FormsAuthentication.SignOut();

            var requestCookiesKey = HttpContext.Request.Cookies.AllKeys.FirstOrDefault(x => x.StartsWith("__RequestVerificationToken"));

            if (!string.IsNullOrEmpty(requestCookiesKey))
            {
                var c = new HttpCookie(requestCookiesKey);
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }

            if (HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var c = new HttpCookie(FormsAuthentication.FormsCookieName);
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }

            if (HttpContext.Request.Cookies["ASP.NET_SessionId"] != null)
            {
                var c = new HttpCookie("ASP.NET_SessionId");
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }
            
            return RedirectToAction("Login");
        }

        #endregion

        #region User Profile

        //
        // GET: /Account/UserProfile   
        [Authorize]
        public ActionResult UserProfile()
        {
            MBS.DomainModel.UserProfiles userProfileView = new DomainModel.UserProfiles();
            userProfileView.DiagnosticCode = "Z17";
            userProfileView.Province = "Saskatchewan";
            userProfileView.CorporationIndicator = "A";

            var userProfile = _repository.GetUserProfile(_userId);
            if (userProfile != null)
            {
                userProfileView = userProfile;
            }
            
            ViewBag.CorporationIndicatorList = new SelectList(IndicatorSelectList.GetList(), "Value", "Text");
            ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
            ViewBag.ClaimModeList = new SelectList(ClaimModeSelectList.GetList(), "Value", "Text");
            ViewBag.LocationOfServiceList = new SelectList(StaticCodeList.MyLocationOfServiceList.Select(x => new SelectListItem() { Value = x.Key, Text = x.Value }), "Value", "Text");
            ViewBag.ServiceLocationList = new SelectList(StaticCodeList.MyServiceLocationList.Select(x => new SelectListItem() { Value = x.Key, Text = x.Value }), "Value", "Text");


            return View(userProfileView);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(MBS.DomainModel.UserProfiles model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.PostalCode.Length == 6 && model.DoctorNumber.Length == 4 && model.PhoneNumber.Length == 12 && model.ClinicNumber.Length == 3 && 
                            model.GroupNumber.Length == 3)
                    {
                        _repository.InsertOrUpdate(model, _userId);
                        _repository.Save();
                        return RedirectToAction("Manage");
                    }

                    if (model.PostalCode.Length < 6)
                    {
                        ModelState.AddModelError("PostalCode", "The Postal Code is not in the correct format.");
                    }

                    if (model.ClinicNumber.Length < 3)
                    {
                        ModelState.AddModelError("ClinicNumber", "The Clinic Number is not in the correct format.");
                    }

                    if (model.DoctorNumber.Length < 4)
                    {
                        ModelState.AddModelError("DoctorNumber", "The Doctor Number is not in the correct format.");
                    }

                    if (model.PhoneNumber.Length < 12)
                    {
                        ModelState.AddModelError("PhoneNumber", "The Phone Number is not in the correct format.");
                    }

                    if (model.GroupNumber.Length < 3)
                    {
                        ModelState.AddModelError("GroupNumber", "The Group Number is not in the correct format.");
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("UserProfile", "There is an error saving your profile. Please try again or contact support.");
                }
            }

            ViewBag.CorporationIndicatorList = new SelectList(IndicatorSelectList.GetList(), "Value", "Text");
            ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
            ViewBag.ClaimModeList = new SelectList(ClaimModeSelectList.GetList(), "Value", "Text");
            ViewBag.LocationOfServiceList = new SelectList(StaticCodeList.MyLocationOfServiceList.Select(x => new SelectListItem() { Value = x.Key, Text = x.Value }), "Value", "Text");
            ViewBag.ServiceLocationList = new SelectList(StaticCodeList.MyServiceLocationList.Select(x => new SelectListItem() { Value = x.Key, Text = x.Value }), "Value", "Text");

            return View(model);
        }

        #endregion

        #region Manage

        [Authorize]
        public ActionResult Manage()
        {
            ViewBag.RoleName = System.Web.Security.Roles.GetRolesForUser().FirstOrDefault();
            return View();
        }

        #endregion

        #region Change Email

        [Authorize]
        public ActionResult ChangeEmail()
        {
            var model = new ChangeEmailModel();

            var user = Membership.GetUser();
            if (user != null)
            {
                model.CurrentEmail = user.Email;
            }
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeEmail(ChangeEmailModel model)
        {
            if (ModelState.IsValid)
            {
                var user = Membership.GetUser();
                if (user != null)
                {
                    user.Email = model.NewEmail;
                    Membership.UpdateUser(user);

                    return RedirectToAction("Manage");
                }
                else
                {
                    ModelState.AddModelError("ChangeEmail", "Unable to change email!");
                }                
            }

            return View(model);
        }

        #endregion       

        #region Change Password

        [Authorize]
        public ActionResult ChangePassword(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.ResetPasswordNotication ? "Your password had been reset, please change your password!"
                : "";

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(LocalPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                bool changePasswordSucceeded = false;
                try
                {
                    changePasswordSucceeded = System.Web.Security.Membership.GetUser().ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception ex)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    UpdateResetPasswordFlag(false, _userId);
                    return RedirectToAction("ChangePassword", new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Forgot Password

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            ViewBag.ErrorCount = 1;
            return View(new ForgotPasswordModel() { ErrorCount = 1 });
        }

        /// <summary>
        /// Reset the password for the user and email it to him.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {                
                var user = Membership.GetUser(model.UserName);

                if (user == null || !user.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    model.ErrorCount++;

                    ViewBag.ErrorCount = model.ErrorCount;

                    if (model.ErrorCount > 3)
                    {
                        ModelState.AddModelError("", "The information is incorrect. Please email info@perspect.ca, and include Password Reset in the subject line.");                        
                    }
                    else
                    {
                        ModelState.AddModelError("", "Information is not valid. Please check your entry and try again.");
                    }

                }
                else if (user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Login", "Account");
                }                
                else
                {
                    var expiryMinute = ConfigHelper.GetPasswordTokenExpiryMinute();

                    var token = (Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n")).ToLower();
                    
                    var passwordTokenExpiryDate = DateTime.UtcNow.AddMinutes(expiryMinute);

                    var membership = _repository.GetUserMemberShip((Guid)user.ProviderUserKey);
                    if (membership != null)
                    {
                        var requestSubmittedDate = DateTime.UtcNow.AddHours(_timeZoneOffset).ToString("F");

                        //Send Password Reset Email out
                        var messageTemplate = TemplateLoader.LoadFile("ResetPasswordEmailTemplate.html");
                        messageTemplate = string.Format(messageTemplate, requestSubmittedDate, token, expiryMinute);

                        var mailSender = new MailSender();
                        var result = mailSender.SendEmail(model.Email, "Reset Password Request", messageTemplate);
                        if (string.IsNullOrEmpty(result))
                        {
                            membership.PasswordTokenExpireDate = passwordTokenExpiryDate;
                            membership.PasswordToken = token;
                            _repository.UpdateMembership(membership);
                            _repository.Save();

                            return RedirectToAction("ForgotPasswordSuccess", "Account");
                        }
                        else
                        {
                            ModelState.AddModelError("ForgotPasswordError", "There is an issue sending out an email to you! Please contact info@perspect.ca!");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ForgotPasswordError", "There is an issue sending out an email to you! Please contact info@perspect.ca!");
                    }                           
                }
            }

            return View(model);
        }       

        [AllowAnonymous]
        public ActionResult ForgotPasswordSuccess()
        {
            return View();
        }

        #endregion

        #region Reset Forgot Password

        [AllowAnonymous]
        public ActionResult ResetForgotPassword(string token)
        {
            var model = new ResetForgotPasswordModel();
            model.ResetForgotPasswordToken = token.ToLower();

            ViewBag.StatusMessage = "INVALID";

            if (!string.IsNullOrEmpty(token) && token.Length == 64)
            {               
                var memberships = _repository.GetUserMemberShip(model.ResetForgotPasswordToken);
                if (memberships != null)
                {
                    var currentDate = DateTime.UtcNow;
                    if (memberships.PasswordTokenExpireDate > currentDate)
                    {
                        ViewBag.StatusMessage = string.Empty;
                    }                    
                }                
            }
            
            return View(model);
        }

        /// <summary>
        /// Change the password for the user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult ResetForgotPassword(ResetForgotPasswordModel model)
        {
            ViewBag.StatusMessage = string.Empty;
            if (ModelState.IsValid)
            {
                var membership = _repository.GetUserMemberShip(model.ResetForgotPasswordToken);
                if (membership != null)
                {
                    var currentDate = DateTime.UtcNow;
                    if (membership.PasswordTokenExpireDate < currentDate)
                    {
                        ViewBag.StatusMessage = "INVALID";
                        ModelState.AddModelError("InvalidToken", "InvalidToken");
                    }
                    else
                    {
                        membership.PasswordTokenExpireDate = null;
                        membership.PasswordToken = null;
                        membership.IsLockedOut = false;
                        _repository.UpdateMembership(membership);
                        _repository.Save();
                        
                        var user = Membership.GetUser(membership.UserId);

                        var newPassword = user.ResetPassword();

                        user.ChangePassword(newPassword, model.NewPassword);
                        
                        return RedirectToAction("ResetForgotPasswordSuccess", "Account");
                    }
                }
                else
                {
                    ViewBag.StatusMessage = "INVALID";
                    ModelState.AddModelError("InvalidToken", "InvalidToken");
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ResetForgotPasswordSuccess()
        {
            return View();
        }

        #endregion

        #region JSON

        [AllowAnonymous]
        public string WakeUp()
        {
            var admin = Membership.GetUser();

            _repository.WakeUp();

            return string.Empty;
        }

        public string GetUserDoctoName()
        {
            var result = string.Empty;

            if (_userId != Guid.Empty)
            {
                result = _repository.GetUserDoctorName(_userId);
            }

            return result;
        }

        public JsonResult IsMSBGroupValid(string groupNumber, string groupUserKey)
        {
            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());
            var apiService = new ClaimService(msbServiceConfig);
            var isValid = false;

            try
            {
                var dailyReturnFiles = apiService.GetDailyReturnFileList(groupUserKey, groupNumber);
                isValid = dailyReturnFiles.IsSuccess;
            }
            catch
            {
            }

            return Json(isValid, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Helpers

        private void UpdateResetPasswordFlag(bool flag, Guid userId)
        {
            var membership = _repository.GetUserMemberShip(userId);
            if (membership != null)
            {
                membership.IsResetPassword = flag;
                _repository.UpdateMembership(membership);
                _repository.Save();
            }
            
        } 

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            ResetPasswordNotication
        }      

        #endregion
    }
}

