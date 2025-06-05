using MBS.Web.Portal.Models;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using MBS.DataCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize(Roles = "Member")]
    public class ClaimsInReturnController : Controller
    {
        private readonly IClaimsInReturnRepository _repository;
		private readonly Guid _userId = Guid.Empty;
        private int _timeZoneOffset = -6;

		// If you are using Dependency Injection, you can delete the following constructor
        public ClaimsInReturnController() : this(new ClaimsInReturnRepository())
        {
        }

		public ClaimsInReturnController(IClaimsInReturnRepository repository)
        {
            _repository = repository;
            var user = Membership.GetUser();
            if (user != null)
            {
                _userId = new Guid(user.ProviderUserKey.ToString());
            }

            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
        }

        #region Index

        public ActionResult Index()
        {            
            var model = _repository.GetClaimsInReturnWithContentByUser(_userId).OrderByDescending(x => x.UploadDate);
            ViewBag.TimeZoneOffset = _timeZoneOffset;
			return View(model);
        }        

        #endregion

        #region Download file

        public ActionResult DownloadFile(string id)
        {
            byte[] fileContent = new byte[1];
            var uploadDate = DateTime.UtcNow.AddHours(_timeZoneOffset);

            var fileName = "Return-" + uploadDate.ToString("yyyyMMdd") + ".txt";
            var returnId = Guid.Empty;
            Guid.TryParse(id, out returnId);

            if (_repository.IsAccessValidClaimsInReturn(_userId, returnId))
            {
                var returnClaim = _repository.Find(returnId);
                if (returnClaim != null)
                {
                    fileName = "Return-" + returnClaim.UploadDate.AddHours(_timeZoneOffset).ToString("yyyyMMdd") + ".txt";
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes(returnClaim.Content);
                    uploadDate = returnClaim.UploadDate.AddHours(_timeZoneOffset);
                    if (!string.IsNullOrEmpty(returnClaim.ReturnFileName))
                    {
                        fileName = returnClaim.ReturnFileName;
                    }
                }
                else
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve report content or invalid access! Please check the parameters!");
                }
            }
            else
            {
                fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve report content or invalid access! Please check the parameters!");
            }

            return File(fileContent, "text", fileName);
        }

        #endregion

        #region Payment Summary

        public JsonResult GetPaymentSummary(string id)
        {
            var returnId = Guid.Empty;
            Guid.TryParse(id, out returnId);

            var result = new PaymentInfoJson();
            if (_repository.IsAccessValidClaimsInReturn(_userId, returnId))
            {
                var data = new List<string[]>();
                var paymentInfoList = _repository.GetPaymentSummary(returnId);
                if (paymentInfoList.Any())
                {

                    foreach (var info in paymentInfoList)
                    {
                        var temp = new[]
                        {
                            info.GroupIndex.ToString(),
                            info.TotalLineType,
                            StaticCodeList.MyTotalLineType.ContainsKey(info.TotalLineType) ? StaticCodeList.MyTotalLineType[info.TotalLineType] : "N/A",
                            string.Format("{0:C}", info.FeeSubmitted),
                            string.Format("{0:C}", info.FeeApproved),
                            string.Format("{0:C}", info.TotalPremiumAmount),
                            string.Format("{0:C}", info.TotalProgramAmount),
                            string.Format("{0:C}", info.TotalPaidAmount),

                        };

                        data.Add(temp);
                    }                    
                }

                result.data = data.ToArray();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
