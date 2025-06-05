using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.RazorPDF;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using System.Collections.Generic;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize(Roles="Member")]
    public class ClaimsReportController : Controller
    {
        private readonly IClaimsReportRepository _repository;
		private readonly Guid _userId = Guid.Empty;
        private int _timeZoneOffset = -6;

        // If you are using Dependency Injection, you can delete the following constructor
        public ClaimsReportController() : this(new ClaimsReportRepository())
        {
        }

		public ClaimsReportController(IClaimsReportRepository repository)
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
			var model = new ClaimsReportViewModel();

            var startDate = DateTime.UtcNow.AddHours(_timeZoneOffset).AddDays(-7);
            model.ServiceStartDate = string.Format("{0}/{1}/{2}", startDate.ToString("dd"), startDate.ToString("MM"), startDate.ToString("yyyy"));
            
            startDate = DateTime.UtcNow.AddHours(_timeZoneOffset);
            model.ServiceEndDate = string.Format("{0}/{1}/{2}", startDate.ToString("dd"), startDate.ToString("MM"), startDate.ToString("yyyy"));
            model.ReportTypeList = new SelectList(ReportTypeSelectList.GetList(), "Value", "Text");
			model.IsInfoFilled = _repository.GetUserProfile(_userId) == null ? false : true;
            return View(model);
        }

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Index(ClaimsReportViewModel model)
		{
			if (ModelState.IsValid)
			{
                var startDate = GetSearchDate(model.ServiceStartDate);
                var endDate = GetSearchDate(model.ServiceEndDate);

                TotalItem record;

                if ((ReportType)model.ReportType == ReportType.UnitRecordWithPaidClaim || 
                    (ReportType)model.ReportType == ReportType.UnitRecordWithRejectedClaim || 
                    (ReportType)model.ReportType == ReportType.UnitRecord)
                {
                    record = _repository.GetTotalItemWithUnitRecords(_userId, (ReportType)model.ReportType, startDate, endDate);
                }
                else
                {
                    record = _repository.GetTotalItemWithServiceRecords(_userId, (ReportType)model.ReportType, startDate, endDate);
                }

                model.TotalNumberOfRecords = record.NumberOfRecords;
                model.TotalPaidAmount = record.Amount;
			}
            
            model.ReportTypeList = new SelectList(ReportTypeSelectList.GetList(), "Value", "Text");
						        		
			return View(model);
		}

        #endregion

        #region Download - RTF Report

        public ActionResult Download(string start, string end, string type)
		{
			var creator = GetReportCreator(start, end, type);
            byte[] fileContent = new byte[1];

            if (creator != null)
            {
                var reportType = 0;
                int.TryParse(type, out reportType);

                if ((ReportType)reportType == ReportType.UnitRecordWithPaidClaim || 
                    (ReportType)reportType == ReportType.UnitRecordWithRejectedClaim ||
                    (ReportType)reportType == ReportType.UnitRecord)
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes(creator.GetRTFUnitRecordSummaryContent());
                }
                else
                {
                    fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes(creator.GetRTFContent());
                }
            }
            else
            {
                fileContent = System.Text.ASCIIEncoding.ASCII.GetBytes("Unable to retrieve report content! Please check the parameters!");
            }

			return File(fileContent, "application/msword", "MyReturnClaimReport-" + DateTime.UtcNow.AddHours(_timeZoneOffset).ToString("yyyyMMdd") + ".rtf");
		}

        public ActionResult DownloadPDF(string start, string end, string type)
        {
            var creator = GetReportCreator(start, end, type);

            if (creator != null)
            {
                var reportType = 0;
                int.TryParse(type, out reportType);

                if ((ReportType)reportType == ReportType.UnitRecordWithPaidClaim ||
                    (ReportType)reportType == ReportType.UnitRecordWithRejectedClaim ||
                    (ReportType)reportType == ReportType.UnitRecord)
                {
                    creator.GetRTFUnitRecordSummaryContent();
                    return new PdfResult(creator.GetModel(), "PDFReportUnitRecordSummary");
                }
                else
                {
                    creator.GetRTFContent();
                    return new PdfResult(creator.GetModel(), "PDFReport");
                }
            }

            return View();
        }

        private ClaimsReportCreator GetReportCreator(string start, string end, string type)
        {
            var userProfile = _repository.GetUserProfile(_userId);
            var reportType = 0;
            int.TryParse(type, out reportType);

            ClaimsReportCreator result = null;
            if (userProfile != null && reportType > 0)
            {
                var startDate = GetSearchDateDash(start);
                var endDate = GetSearchDateDash(end);

                IEnumerable<ServiceRecord> myRecords = null;

                if ((ReportType)reportType == ReportType.UnitRecordWithPaidClaim ||
                    (ReportType)reportType == ReportType.UnitRecordWithRejectedClaim ||
                    (ReportType)reportType == ReportType.UnitRecord)
                {
                    myRecords = _repository.GetFilterServiceRecordsWithUnitRecords(_userId, (ReportType)reportType, startDate, endDate);
                }
                else
                {
                     myRecords = _repository.GetFilterServiceRecords(_userId, (ReportType)reportType, startDate, endDate).ToList();          
                }

                result = new ClaimsReportCreator(myRecords, userProfile.DoctorNumber.ToString(), _timeZoneOffset, startDate, endDate, (ReportType)reportType);
            }

            return result;
        }

		#endregion

		private DateTime GetSearchDateDash(string mySearchDate)
		{
			DateTime myReturnDate = new DateTime(1980, 1, 1);
			try
			{
				string[] mySplit = mySearchDate.Split('-');
				myReturnDate = new DateTime(int.Parse(mySplit[2]), int.Parse(mySplit[1]), int.Parse(mySplit[0]));
			}
			catch
			{
			}
			return myReturnDate;
		}

        private DateTime GetSearchDate(string mySearchDate)
        {
            DateTime myReturnDate = new DateTime(1980, 1, 1);
            try
            {
                string[] mySplit = mySearchDate.Split('/');
                myReturnDate = new DateTime(int.Parse(mySplit[2]), int.Parse(mySplit[1]), int.Parse(mySplit[0]));
            }
            catch
            {
            }
            return myReturnDate;
        }
    }
}
