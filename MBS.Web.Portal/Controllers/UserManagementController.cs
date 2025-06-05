using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;
using MBS.DomainModel;
using MBS.Web.Portal.Models;
using MBS.Common;
using MBS.Web.Portal.Services;
using System.Web.Http.Results;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize(Roles="Administrator")]
    public class UserManagementController : BaseController
    {
        private int _timeZoneOffset = -6;

        public UserManagementController()
        {
            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
        }

        #region Index

        // GET: /UserManagement/Index        
        public ActionResult Index()
        {          
            var model = new UserManagementViewModel();

            ViewBag.ErrorMessage = string.Empty;

            try
            {
                model = GetUserList(true);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message + "<br>" + ex.StackTrace;
            }

            return View(model);
        }

        public ActionResult DisabledUsers()
        {
            var model = new UserManagementViewModel();

            ViewBag.ErrorMessage = string.Empty;

            try
            {
                model = GetUserList(false);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message + "<br>" + ex.StackTrace;
            }

            return View(model);
        }

        private UserManagementViewModel GetUserList(bool IsEnabled)
        {
            var model = new UserManagementViewModel();

            using (var context = new MedicalBillingSystemEntities())
            {
                var userProfiles = context.UserProfiles.ToList();
                var memberships = context.Memberships.ToList();

                model.Users = context.Users.ToList().Select(x => GetUserModel(x, userProfiles, memberships)).Where(x => x.IsLockOut == !IsEnabled).OrderBy(u => u.Name).ToList();
            }

            return model;
        }

        private UserModel GetUserModel(Users user, IEnumerable<UserProfiles> userProfiles, IEnumerable<Memberships> memberships)
        {
            var result = new UserModel();
            result.UserId = user.UserId;
            result.UserName = user.UserName;

            var memberShip = memberships.FirstOrDefault(x => x.UserId == user.UserId);
            if (memberShip != null)
            {
                result.Email = memberShip.Email;
                result.CreatedDate = memberShip.CreateDate.AddHours(_timeZoneOffset);
                result.LastLoginDate = memberShip.LastLoginDate.AddHours(_timeZoneOffset);
                result.IsLockOut = memberShip.IsLockedOut;
            }

            var userProfile = userProfiles.FirstOrDefault(x => x.UserId == user.UserId);
            if (userProfile != null)
            {
                result.Name = userProfile.DoctorName;
            }

            return result;
        }

        #endregion        

        #region Register

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {                
                MembershipCreateStatus status;
                var newUser = Membership.CreateUser(model.Username.ToLower(), model.Password, model.Email.ToLower(), null, null, true, out status);

                if (newUser != null && status == MembershipCreateStatus.Success)
                {
                    System.Web.Security.Roles.AddUserToRole(model.Username.ToLower(), "Member");
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("Register", ErrorCodeToString(status));
                }
            }            

            return View(model);
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {            
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "The e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        #endregion

        #region Change Password

        public ActionResult ResetPassword(string id)
        {
            Guid userId;
            Guid.TryParse(id, out userId);

            if (userId != Guid.Empty)
            {
                var model = new ResetPasswordModel();
                model.UserId = userId;
                using (var context = new MedicalBillingSystemEntities())
                {
                    var user = context.Users.FirstOrDefault(x => x.UserId == model.UserId);
                    if (user != null)
                    {
                        model.Username = user.UserName;
                    }
                }                              
                
                return View(model);                
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var userName = string.Empty;
                using (var context = new MedicalBillingSystemEntities())
                {
                    var user = context.Users.FirstOrDefault(x => x.UserId == model.UserId);
                    if (user != null)
                    {
                        userName = user.UserName;
                    }
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    var membershipUser = Membership.GetUser(userName, false);

                    if (membershipUser.IsLockedOut)
                    {
                        using (var context = new MedicalBillingSystemEntities())
                        {
                            var userId = new Guid(membershipUser.ProviderUserKey.ToString());
                            var user = context.Memberships.FirstOrDefault(x => x.UserId == userId);
                            if (user != null)
                            {
                                user.IsLockedOut = !user.IsLockedOut;
                                context.Entry(user).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                            }
                        }
                    }

                    var regEx = new Regex(@"^.*(?=.{7,})(?=.*\d)(?=.*[!.@#$%^&+=])");
                    if (regEx.Match(model.NewPassword).Success)
                    {
                        var resetPassword = membershipUser.ResetPassword();
                        try
                        {
                            if (membershipUser.ChangePassword(resetPassword, model.NewPassword))
                            {
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                ModelState.AddModelError("ResetPassword", "Unable to change the password, the new password is invalid. Please check the password format! BUT, the password for the account is reset to: " + resetPassword);
                            }
                        }
                        catch
                        {
                            ModelState.AddModelError("ResetPassword", "Unable to change the password, the new password is invalid. Please check the password format! BUT, the password for the account is reset to: " + resetPassword);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ResetPassword", "Unable to change the password, the new password is invalid. Please check the password format!");
                    }
                }
                else
                {
                    ModelState.AddModelError("ResetPassword", "Unable to find the user to change the password.");
                }
            }

            return View(model);
        }

        #endregion

        #region Send Bulk Email

        [HttpPost]
        public JsonResult SendBulkEmail(SendBulkEmailModel emailModel)
        {
            var messageTemplate = TemplateLoader.LoadFile("BulkEmailTemplate.html");
            messageTemplate = string.Format(messageTemplate, emailModel.MessageBody);
            var mailSender = new MailSender();
            var sendMessage = mailSender.SendEmail(emailModel.UserEmail, emailModel.MessageSubject, messageTemplate);            
            return this.Json(string.IsNullOrEmpty(sendMessage));
        }

        #endregion

        #region Lock Status JSON

        [HttpPost]
        public JsonResult ChangeLockStatus(string userId)
        {
            var success = false;
            var userGuid = Guid.Empty;
            Guid.TryParse(userId, out userGuid);
            if (userGuid != Guid.Empty)
            {                
                using (var context = new MedicalBillingSystemEntities())
                {
                    var user = context.Memberships.FirstOrDefault(x => x.UserId == userGuid);
                    if (user != null)
                    {
                        user.IsLockedOut = !user.IsLockedOut;
                        context.Entry(user).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                        success = true;
                    }
                }
            }

            if (success)
            {
                return this.Json(new { success = true });
            }
            else
            {
                return this.Json(new { success = false });
            }
        }

        #endregion

        #region Get Memory Usage

        public JsonResult GetCurrentMemory()
        {
            var result = string.Format("{0:N}MB", (GC.GetTotalMemory(false) / 1000000));
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region User Impersonation

        public JsonResult UserImpersonation(string targetId)
        {
            var result = false;
            var userId = new Guid(targetId);
            using (var context = new MedicalBillingSystemEntities())
            {
                var user = context.Users.FirstOrDefault(x => x.UserId == userId);
                if (user != null)
                {
                    FormsAuthentication.SetAuthCookie(user.UserName, false);
                    result = true;
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}

