using MBS.Common;
using MBS.DataCache;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize]
    public class HomeController : Controller
    {        
        private readonly IHomeRepository _repository;
        private readonly Guid _userId = Guid.Empty;
        private readonly int _timeZoneOffset = -6;
        private readonly bool _isUserProfileExisted = false;
        private readonly bool _isAdmin;

        // If you are using Dependency Injection, you can delete the following constructor
        public HomeController() : this(new HomeRepository())
        {
        }

        public HomeController(IHomeRepository repository)
        {
            _repository = repository;
            var user = Membership.GetUser();
            if (user != null)
            {
                _userId = new Guid(user.ProviderUserKey.ToString());
                _isUserProfileExisted = _repository.IsUserProfileExisted(_userId);
                _isAdmin = user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase);
            }

            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
        }

        public ActionResult ManualPDF()
        {
            var _pdfFilePath = Server.MapPath("App_Data/export.pdf").Replace("\\Home", string.Empty);
            return File(_pdfFilePath, "application/pdf");
        }

        public ActionResult Index()
        {
            #region Test Code

            #endregion Test Code

            if (_isAdmin)
            {
                return RedirectToAction("Index", "UserManagement");
            } 
            else if (!_isUserProfileExisted)
            {
                return RedirectToAction("UserProfile", "Account");
            }

            var model = new HomeViewModel();
            model.IntervalList = new SelectList(IntervalSelectList.GetList(), "Value", "Text");

            var currentTimeZoneDate = DateTime.UtcNow.AddHours(_timeZoneOffset);
            var yearToDate = new DateTime(currentTimeZoneDate.Year, 1, 1);

            model.ClaimsTotal = GetClaimsTotal(yearToDate, currentTimeZoneDate);

            model.ClaimsLost = _repository.GetClaimsLostInfo(_userId, currentTimeZoneDate);
          
            #region Notification

            var notifications = new List<Notification>();

            notifications.Add(new Notification() { Level = 1, Message = "Welcome to Perspect Medical Billing" });

            var userProfile = _repository.GetUserProfile(_userId);
            if (userProfile == null)
            {
                var action = RedirectToAction("UserProfile", "Account");
                var item = new Notification()
                {
                    Level = 3,
                    Message = "You must fill in your User Profile in order to submit to MSB site. Click <a href='./Account/UserProfile'>here</a> to fill in your profile."
                };

                notifications.Add(item); 
            }
            else if (userProfile != null && (string.IsNullOrEmpty(userProfile.GroupNumber) || string.IsNullOrEmpty(userProfile.GroupUserKey)))
            {

                var action = RedirectToAction("UserProfile", "Account");
                var item = new Notification()
                {
                    Level = 3,
                    Message = "You must fill in Group Number and Group User Key in order to submit to MSB site. Click <a href='./Account/UserProfile'>here</a> to fill them in your profile."
                };

                notifications.Add(item);
            }
                        
            if (_repository.NeedToReSubmitFax(_userId))            
            {
                var item = new Notification()
                {
                    Level = 2,
                    Message = "There are faxes that fail to submit to WCB, please click <a href='javascript:void(0)' onclick='ResubmitFax();' >here</a> to resubmit."
                };

                notifications.Add(item);
            }

            var claimsInSubmission = _repository.GetRejectedOrRecentlyAcceptedClaimsIn(_userId);
            if (claimsInSubmission.Any())
            {
                foreach (var record in claimsInSubmission)
                {
                    var submittedDate = record.CreatedDate.AddDays(_timeZoneOffset).ToString("g");
                    if (record.FileSubmittedStatus == "REJECTED")
                    {
                        var item = new Notification()
                        {
                            Level = 3,
                            Message = "Your batch of claims submitted on " + submittedDate + " (Filename: " + record.SubmittedFileName + ") is rejected by MSB. The claims are back in the New Claims page. Click <a href='./ClaimsIn/ViewClaimsInFiles'>here</a> to view the MSB validation result."
                        };

                        notifications.Add(item);
                    }
                    else
                    {
                        var item = new Notification()
                        {
                            Level = 1,
                            Message = "Your batch of claims submitted on " + submittedDate + " (Filename: " + record.SubmittedFileName + ") is accepted by MSB. Click <a href='./ServiceRecord'>here</a> to view them."
                        };

                        notifications.Add(item);
                    }
                }
            }

            model.NotificationList = notifications;
            
            #endregion

            return View(model);
        }

        private TotalInfo GetClaimsTotal(DateTime period, DateTime currentDate)
        {
            var model = new TotalInfo();

            model.UnSubmitted = _repository.GetUnSubmittedInfo(_userId, period);

            model.Submitted = _repository.GetSubmittedInfo(_userId, period);

            model.Pending = _repository.GetPendingInfo(_userId, period);

            model.Paid = _repository.GetPaidInfo(_userId, period);

            model.Rejected = _repository.GetRejectedInfo(_userId, period);                                   

            model.Expiring = _repository.GetExpiringInfo(_userId, currentDate);

            return model;
        }

        public JsonResult GetTotals(string period)
        {            
            var selectedPeriod = GetProperTimePeriod(period);
            var result = new TotalInfo();

            if (selectedPeriod != DateTime.MinValue)
            {
                result = GetClaimsTotal(selectedPeriod, DateTime.Now.Date);
            }
            
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private DateTime GetProperTimePeriod(string period)
        {
            var result = DateTime.MinValue;

            if (period.Equals("ytd", StringComparison.OrdinalIgnoreCase))
            {
                result = new DateTime(DateTime.Now.Year, 1, 1);
            }
            else if (period.Equals("last12month", StringComparison.OrdinalIgnoreCase))
            {
                result = DateTime.Now.AddMonths(-12).Date;
            }
            else if (period.Equals("last3month", StringComparison.OrdinalIgnoreCase))
            {
                result = DateTime.Now.AddMonths(-3).Date;
            }
            else if (period.Equals("last2week", StringComparison.OrdinalIgnoreCase))
            {
                result = DateTime.Now.AddDays(-14).Date;
            }
            else if (period.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                result = new DateTime(1753, 1, 1);
            }

            return result;
        }        
    }
}