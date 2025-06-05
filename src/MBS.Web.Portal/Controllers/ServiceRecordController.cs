using MBS.Common;
using MBS.DataCache;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;
using MBS.WebApiService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [Authorize(Roles= "Member,Administrator")]
    public class ServiceRecordController : Controller
    {
        private readonly IServiceRecordRepository _repository;
		private readonly Guid _userId = Guid.Empty;
        private readonly string _myDiagCode = string.Empty;
        private readonly string _myPremCode = string.Empty;
        private readonly string _myServiceLocation = string.Empty;
        private readonly int _timeZoneOffset = -6;
        private readonly ServiceConfig _msbServiceConfig;
        private readonly bool ContainUserProfile = false;
        private readonly bool ContainRequiredSubmissionData = false;
        private readonly bool _isAdmin = false;

        public ServiceRecordController() : this(new ServiceRecordRepository())
        {
        }

		public ServiceRecordController(IServiceRecordRepository repository)
        {
            _repository = repository;

            var user = Membership.GetUser();
            if (user != null)
            {
                _userId = new Guid(user.ProviderUserKey.ToString());
                if (System.Web.Security.Roles.GetRolesForUser().Contains("Administrator"))
                {
                    _isAdmin = true;
                }
                else
                {
                    var userProfile = _repository.GetUserProfile(_userId);

                    if (userProfile != null)
                    {
                        ContainUserProfile = true;
                        ContainRequiredSubmissionData = !string.IsNullOrEmpty(userProfile.GroupUserKey) && !string.IsNullOrEmpty(userProfile.GroupNumber);

                        _myDiagCode = userProfile.DiagnosticCode.Trim();
                        _myPremCode = string.IsNullOrEmpty(userProfile.DefaultPremCode) ? "2" : userProfile.DefaultPremCode;
                        _myServiceLocation = userProfile.DefaultServiceLocation;
                    }
                }
            }

            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();

            _msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());
        }

        #region Submitted 

        public ActionResult Index()
        {
            var serviceRecordList = _repository.GetAllSimpleSubmittedServiceRecords(_userId);
			return View(serviceRecordList.OrderByDescending(x => x.ServiceDate));
        }       		

        #endregion             

        #region Not Submitted

        public ActionResult NotSubmitted()
        {
            ViewBag.TimeZoneOffset = _timeZoneOffset;
            var model = new NotSubmittedViewModel();
            model.CurrentDate = DateTime.UtcNow.AddHours(_timeZoneOffset);
            model.ContainUserProfile = ContainUserProfile;
            model.ContainRequiredSubmissionData = ContainRequiredSubmissionData;

            model.UnSubmittedList = _repository.GetAllSimpleUnSubmittedServiceRecords(_userId);
            model.TotalAmount = model.UnSubmittedList.Sum(x => x.ClaimAmount);
            model.AllowToSubmit = true;

            return View(model);
        }

        #endregion

        #region Pending 

        public ActionResult PendingClaim()
        {
            var serviceRecordList = _repository.GetAllSimplePendingServiceRecords(_userId);
            return View(serviceRecordList);
        }

        #endregion

        #region Create

        public ActionResult Create()
		{
            var model = new ServiceRecordDetailModel();
            model.Record = new ServiceRecord();
            model.Record.ClaimType = (int)ClaimType.MSB;
            model.Record.Sex = "M";
            model.Record.UserId = _userId;

            var lastEnteredServiceDate = GetLastEnterServiceDate();
            model.ServiceDateString = lastEnteredServiceDate.ToString("ddMMyy");
            model.DischargeDateString = string.Empty;

            model.PremiumCode = _myPremCode;
            model.LastSelectedDiagCode = GetLastSelectedDiagCode(_myDiagCode);

            model.PremiumCodeList = StaticCodeList.MyPremiumCodeList;
            model.HospitalCareServiceCodeList = StaticCodeList.MyCareCodeList;
            model.RNPExcludeCodeList = StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList;

            model.Record.ServiceLocation = _myServiceLocation;

            model.ButtonUsedToSubmit = string.Empty;
                        
            for (var i = 1; i < 8; i++)
            {
                SetPropValue(model, "DiagCode" + i.ToString(), model.LastSelectedDiagCode);
            }

            model.MostUsedCodes = _repository.GetMostPopularUsedCodes(_userId);

            ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
            ViewBag.ServiceLocationList = GetSelectList(StaticCodeList.MyServiceLocationList);
            ViewBag.RecordClaimTypeList = GetSelectList(StaticCodeList.MyRecordClaimTypeList);
            //ViewBag.BilateralIndicatorList = GetSelectList(StaticCodeList.MyBilateralIndicatorList);
            ViewBag.SpecialCircumstancesIndicatorList = GetSelectList(StaticCodeList.MySpecialCircumstancesIndicatorList);
            ViewBag.LocationOfServiceList = GetSelectList(StaticCodeList.MyLocationOfServiceList);
            ViewBag.FaciltiyNumberList = GetSelectList(StaticCodeList.MyFacilityNumberList);
            ViewBag.IsAdmin = _isAdmin;

            return View(model);
		}

        private SelectList GetSelectList(Dictionary<string, string> targetList)
        {
            return new SelectList(
                targetList.Select(x => new SelectListItem() { Value = x.Key, Text = x.Value }),
                "Value",
                "Text"
            );
        }
		
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(ServiceRecordDetailModel model)
		{
            if (ModelState.IsValid)
            {
                var actionResult = PerformAction(model, false);
                if (actionResult != null)
                {
                    return actionResult;
                }
            }

            model.PremiumCodeList = StaticCodeList.MyPremiumCodeList;
            model.HospitalCareServiceCodeList = StaticCodeList.MyCareCodeList;
            model.LastSelectedDiagCode = GetLastSelectedDiagCode(_myDiagCode);
            model.RNPExcludeCodeList = StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList;

            model.ButtonUsedToSubmit = string.Empty;

            ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
            ViewBag.ServiceLocationList = GetSelectList(StaticCodeList.MyServiceLocationList);
            ViewBag.RecordClaimTypeList = GetSelectList(StaticCodeList.MyRecordClaimTypeList);
            //ViewBag.BilateralIndicatorList = GetSelectList(StaticCodeList.MyBilateralIndicatorList);
            ViewBag.SpecialCircumstancesIndicatorList = GetSelectList(StaticCodeList.MySpecialCircumstancesIndicatorList);
            ViewBag.LocationOfServiceList = GetSelectList(StaticCodeList.MyLocationOfServiceList);
            ViewBag.FaciltiyNumberList = GetSelectList(StaticCodeList.MyFacilityNumberList);
            ViewBag.IsAdmin = _isAdmin;

            return View(model);
		}

		#endregion

		#region Edit

		public ActionResult Edit(string id, string mode)
		{
			Guid serviceRecordId;
			Guid.TryParse(id, out serviceRecordId);

			if (serviceRecordId != Guid.Empty && (_repository.IsServiceRecordBelongToUser(_userId, serviceRecordId) || _isAdmin))
			{
                ServiceRecord serviceRecord = null;

                if (_isAdmin)
                {
                    serviceRecord = _repository.GetRecord(serviceRecordId);
                }
                else
                {
                    serviceRecord = _repository.GetRecord(serviceRecordId, _userId);
                }

				if (serviceRecord != null)
				{
                    var model = new ServiceRecordDetailModel();
                    model.ReferFrom = Request.UrlReferrer.Segments.LastOrDefault();
                    model.Record = serviceRecord;
					model.ServiceDateString = serviceRecord.ServiceDate.ToString("ddMMyy");
                    model.DischargeDateString = serviceRecord.DischargeDate.HasValue ? serviceRecord.DischargeDate.Value.ToString("ddMMyy") : string.Empty;
					model.BirthDateString = serviceRecord.DateOfBirth.ToString("MMyy");
                    model.Record.PatientFirstName = serviceRecord.PatientFirstName;
					model.Record.PatientLastName = serviceRecord.PatientLastName;
                    model.Record.HospitalNumber = serviceRecord.HospitalNumber;
                    model.Record.ServiceLocation = serviceRecord.ServiceLocation;
                    model.LastSelectedDiagCode = GetLastSelectedDiagCode(_myDiagCode);
                    model.Record.ServiceLocation = serviceRecord.ServiceLocation;

                    model.MostUsedCodes = _repository.GetMostPopularUsedCodes(_userId);

                    #region Set Property Values

                    if (serviceRecord.ServiceStartTime.HasValue)
                    {
                        model.ServiceStartTimeString = serviceRecord.ServiceStartTime.Value.ToString("hhmm");
                    }

                    if (serviceRecord.ServiceEndTime.HasValue)
                    {
                        model.ServiceEndTimeString = serviceRecord.ServiceEndTime.Value.ToString("hhmm");
                    }

                    var unitRecords = _repository.GetUnitRecords(serviceRecordId);

                    model.PremiumCode = _myPremCode;

                    var index = 1;
                    foreach (var unitRecord in unitRecords.OrderBy(x => x.RecordIndex))
                    {
                        var indexString = index.ToString();
                        SetPropValue(model, "UnitCode" + indexString, unitRecord.UnitCode);

                        SetPropValue(model, "UnitNumber" + indexString, unitRecord.UnitNumber.ToString());

                        var myUnitCode = unitRecord.UnitCode.ToUpper();
                        if (StaticCodeList.MyFeeCodeList.ContainsKey(myUnitCode))
                        {
                            if (Convert.ToDouble(StaticCodeList.MyFeeCodeList[myUnitCode].FeeAmount.ToString()) == 0d ||
                                serviceRecord.PaidClaimId.HasValue || 
                                (serviceRecord.ClaimsInId.HasValue && !serviceRecord.PaidClaimId.HasValue && !serviceRecord.RejectedClaimId.HasValue) ||
                                (!string.IsNullOrEmpty(serviceRecord.CPSClaimNumber) && !serviceRecord.PaidClaimId.HasValue && !serviceRecord.RejectedClaimId.HasValue))
                            {
                                SetPropValue(model, "UnitAmount" + indexString, unitRecord.UnitAmount.ToString());
                            }
                        }
                        
                        SetPropValue(model, "ExplainCode" + indexString, unitRecord.ExplainCode);
                        SetPropValue(model, "ExplainCodeDesc" + indexString, GetExplainCodeDesc(unitRecord.ExplainCode));

                        SetPropValue(model, "ExplainCode" + indexString + "b", unitRecord.ExplainCode2);
                        SetPropValue(model, "ExplainCodeDesc" + indexString + "b", GetExplainCodeDesc(unitRecord.ExplainCode2));

                        SetPropValue(model, "ExplainCode" + indexString + "c", unitRecord.ExplainCode3);
                        SetPropValue(model, "ExplainCodeDesc" + indexString + "c", GetExplainCodeDesc(unitRecord.ExplainCode3));

                        SetPropValue(model, "DiagCode" + indexString, unitRecord.DiagCode);
                        SetPropValue(model, "RunCode" + indexString, unitRecord.RunCode);
                        SetPropValue(model, "RecordClaimType" + indexString, unitRecord.RecordClaimType);
                        SetPropValue(model, "SpecialCircumstanceIndicator" + indexString, unitRecord.SpecialCircumstanceIndicator);
                        SetPropValue(model, "BilateralIndicator" + indexString, unitRecord.BilateralIndicator);
                        SetPropValue(model, "UnitPremiumCode" + indexString, unitRecord.UnitPremiumCode);
                        SetPropValue(model, "UnitSubmittedRecordIndex" + indexString, unitRecord.SubmittedRecordIndex.HasValue ? unitRecord.SubmittedRecordIndex.Value.ToString() : string.Empty);
                        SetPropValue(model, "OriginalRunCode" + indexString, unitRecord.OriginalRunCode);

                        var amountDescString = "Not Paid Yet";

                        if (serviceRecord.PaidClaimId.HasValue)
                        {
                            amountDescString = "Paid $: " + unitRecord.PaidAmount.ToString("C");
                            if (unitRecord.SubmittedAmount.HasValue)
                            {
                                amountDescString += ", Billed $: " + GetAmountWithPremium(unitRecord.UnitCode, unitRecord.SubmittedAmount.Value, unitRecord.UnitPremiumCode, model.Record.ServiceLocation).ToString("C");
                            }
                        }

                        SetPropValue(model, "UnitAmountDesc" + indexString, amountDescString);

                        if (unitRecord.StartTime.HasValue)
                        {
                            SetPropValue(model, "UnitStartTime" + indexString, unitRecord.StartTime.Value.ToString("hhmm"));                            
                        }

                        if (unitRecord.EndTime.HasValue)
                        {
                            SetPropValue(model, "UnitEndTime" + indexString, unitRecord.EndTime.Value.ToString("hhmm"));
                        }                                             
                        
                        index++;
                    }


                    #endregion

                    model.PremiumCodeList = StaticCodeList.MyPremiumCodeList;
                    model.HospitalCareServiceCodeList = StaticCodeList.MyCareCodeList;
                    model.RNPExcludeCodeList = StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList;

                    for (var i = index; i < 8; i++)
                    {
                        SetPropValue(model, "DiagCode" + i.ToString(), model.LastSelectedDiagCode);
                    }

                    model.ButtonUsedToSubmit = string.Empty;

                    ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
                    ViewBag.RecordClaimTypeList = GetSelectList(StaticCodeList.MyRecordClaimTypeList);
                    ViewBag.ServiceLocationList = GetSelectList(StaticCodeList.MyServiceLocationList);
                    //ViewBag.BilateralIndicatorList = GetSelectList(StaticCodeList.MyBilateralIndicatorList);
                    ViewBag.SpecialCircumstancesIndicatorList = GetSelectList(StaticCodeList.MySpecialCircumstancesIndicatorList);
                    ViewBag.LocationOfServiceList = GetSelectList(StaticCodeList.MyLocationOfServiceList);
                    ViewBag.FaciltiyNumberList = GetSelectList(StaticCodeList.MyFacilityNumberList);
                    ViewBag.IsAdmin = _isAdmin;

                    return View(model);
				}
			}
			
			return RedirectToAction("NotSubmitted");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(ServiceRecordDetailModel model)
		{
            if (ModelState.IsValid)
			{
                var actionResult = PerformAction(model, true);
                if (actionResult != null)
                {
                    return actionResult;
                }
			}

            if (model.ButtonUsedToSubmit != null && model.ButtonUsedToSubmit.Equals("ResubmitAndEdit"))
            {
                var serviceRecordId = model.Record.ServiceRecordId;
                if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId) && 
                    (model.Record.ClaimStatus == (int)SearchClaimType.Submitted || model.Record.ClaimStatus == (int)SearchClaimType.Pending || model.Record.ClaimStatus == (int)SearchClaimType.Paid))
                {
                    try
                    {
                        var serviceRecord = _repository.GetRecordWithUnitRecords(serviceRecordId, _userId);

                        foreach (var unitRecord in serviceRecord.UnitRecord)
                        {
                            var resubmitClaim = new ClaimsResubmitted();
                            resubmitClaim.ClaimNumber = serviceRecord.ClaimNumber;
                            resubmitClaim.CreatedDate = DateTime.UtcNow;
                            resubmitClaim.HospitalNumber = serviceRecord.HospitalNumber;
                            resubmitClaim.PatientLastName = serviceRecord.PatientLastName;
                            resubmitClaim.RecordId = Guid.NewGuid();
                            resubmitClaim.ServiceDate = serviceRecord.ServiceDate;
                            resubmitClaim.UnitCode = unitRecord.UnitCode;
                            resubmitClaim.UnitNumber = unitRecord.UnitNumber;
                            resubmitClaim.UnitPremiumCode = unitRecord.UnitPremiumCode;
                            resubmitClaim.UserId = serviceRecord.UserId;

                            _repository.Insert(resubmitClaim);
                        }

                        _repository.Save();

                        var nextNumberModel = _repository.GetNextClaimNumber(_userId);
                        _repository.ResetSubmittedServiceRecord(serviceRecordId, nextNumberModel.RollOverNumber, nextNumberModel.NextClaimNumber);

                        return RedirectToAction("Edit", new { id = serviceRecordId });
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("General", "Unable to set the record for resubmission. Please try again!");
                    }
                }
            }

            model.PremiumCodeList = StaticCodeList.MyPremiumCodeList;
            model.HospitalCareServiceCodeList = StaticCodeList.MyCareCodeList;
            model.LastSelectedDiagCode = GetLastSelectedDiagCode(_myDiagCode);
            model.RNPExcludeCodeList = StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList;

            model.ButtonUsedToSubmit = string.Empty;

            ViewBag.ProvinceList = new SelectList(ProvinceSelectList.GetList(), "Value", "Text");
            ViewBag.RecordClaimTypeList = GetSelectList(StaticCodeList.MyRecordClaimTypeList);
            ViewBag.ServiceLocationList = GetSelectList(StaticCodeList.MyServiceLocationList);
            //ViewBag.BilateralIndicatorList = GetSelectList(StaticCodeList.MyBilateralIndicatorList);
            ViewBag.SpecialCircumstancesIndicatorList = GetSelectList(StaticCodeList.MySpecialCircumstancesIndicatorList);
            ViewBag.LocationOfServiceList = GetSelectList(StaticCodeList.MyLocationOfServiceList);
            ViewBag.FaciltiyNumberList = GetSelectList(StaticCodeList.MyFacilityNumberList);
            ViewBag.IsAdmin = _isAdmin;

            return View(model);
		}

		#endregion
				
		#region Create / Edit code
		
		private ActionResult PerformAction(ServiceRecordDetailModel model, bool isEdit)
		{			
			if (model.Record.UserId == _userId)
            {
                #region Check Custom Field

                if (!IsSexValid(model))
                {
                    ModelState.AddModelError("Record.Sex", "Please enter a correct value for Sex (M or F)");
                }

                if (!IsHospitalNumberValid(model))
                {
                    ModelState.AddModelError("Record.HospitalNumber", "Please enter a correct hospital number");
                }

                if (!IsDoctorNumberValid(model))
                {
                    ModelState.AddModelError("Record.ReferringDoctorNumber", "Please enter a correct referring doctor number");
                }

                if (!IsServiceDateValid(model.ServiceDateString, true))
                {
                    ModelState.AddModelError("ServiceDateString", "Please enter a correct service date");
                }

                if (!IsBirthDateValid(model))
                {
                    ModelState.AddModelError("BirthDateString", "Please enter a correct date of birth");
                }
             
                if (!IsAtLeastOneUnitRecord(model))
                {
                    ModelState.AddModelError("UnitCode1", "Must have at least one unit record");
                }
                
                if (!IsOnlyOneSpecialUnitRecord(model))
                {
                    ModelState.AddModelError("UnitCode1", "Can only have one unit record that contain 893A, 894A, 895A");
                }

                if (!IsFiveWCBClaimRecords(model))
                {
                    ModelState.AddModelError("UnitCode1", "WCB Claim can only have maximum of 5 unit records when the premium code is B or K. Please create a new WCB claim for the sixth record");
                }

                var careCodeIndexList = GetNumberOfHospitalCareCode(model);
                if (careCodeIndexList.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(model.DischargeDateString) && !IsServiceDateValid(model.DischargeDateString, false))
                    {
                        ModelState.AddModelError("DischargeDateString", "Please enter a correct discharge date");
                    }

                    if (careCodeIndexList.Count() > 2)
                    {
                        ModelState.AddModelError("UnitCode" + careCodeIndexList.Last(), "A maximum of only 2 hospital care service records are allowed per claim number");
                    }
                }
                else
                {
                    model.DischargeDateString = string.Empty;
                }
                
                var invalidIndex = IsNotWCBRecordForMSBClaimRecord(model);
                if (invalidIndex > 0)
                {
                    ModelState.AddModelError("UnitCode" + invalidIndex.ToString(), "Cannot have WCB code in MSB Claim for Unit Record " + invalidIndex.ToString());
                }

                if (!string.IsNullOrEmpty(model.Record.Comment))
                {
                    if ((ClaimType)model.Record.ClaimType == ClaimType.MSB && model.Record.Comment.Length > 770)
                    {
                        model.Record.Comment = model.Record.Comment.Substring(0, 770);
                    }
                    else if ((ClaimType)model.Record.ClaimType == ClaimType.WCB && model.Record.Comment.Length > 425)
                    {
                        model.Record.Comment = model.Record.Comment.Substring(0, 425);
                    }

                    model.Record.Comment = model.Record.Comment.Replace("\r", "").Replace("\n", " ");
                }

				#endregion

                if (ModelState.AsQueryable().Count(f => f.Value.Errors.Count > 0) == 0)
                {                   
                    var serviceRecord = model.Record;     
                    
                    if (isEdit)
                    {
                        serviceRecord = _repository.GetRecordWithUnitRecords(model.Record.ServiceRecordId, _userId);

                        if (!string.IsNullOrEmpty(model.ButtonUsedToSubmit) && model.ButtonUsedToSubmit.Equals("SaveAsResubmit"))
                        {
                            foreach (var unitRecord in serviceRecord.UnitRecord)
                            {
                                var resubmitClaim = new ClaimsResubmitted();
                                resubmitClaim.ClaimNumber = serviceRecord.ClaimNumber;
                                resubmitClaim.CreatedDate = DateTime.UtcNow;
                                resubmitClaim.HospitalNumber = serviceRecord.HospitalNumber;
                                resubmitClaim.PatientLastName = serviceRecord.PatientLastName;
                                resubmitClaim.RecordId = Guid.NewGuid();
                                resubmitClaim.ServiceDate = serviceRecord.ServiceDate;
                                resubmitClaim.UnitCode = unitRecord.UnitCode;
                                resubmitClaim.UnitNumber = unitRecord.UnitNumber;
                                resubmitClaim.UnitPremiumCode = unitRecord.UnitPremiumCode;
                                resubmitClaim.UserId = serviceRecord.UserId;

                                _repository.Insert(resubmitClaim);
                            }
                        }
                    }

                    serviceRecord.PatientFirstName = model.Record.PatientFirstName.Replace("\r", "").Replace("\n", "").Replace("’", "'").Trim();
					serviceRecord.PatientLastName = model.Record.PatientLastName.Replace("\r", "").Replace("\n", "").Replace("’", "'").Trim();
                    serviceRecord.HospitalNumber = model.Record.HospitalNumber.Replace("\r", "").Replace("\n", "").Trim().ToUpper();
                    serviceRecord.ServiceStartTime = GetTimeSpan(model.ServiceStartTimeString);
                    serviceRecord.ServiceEndTime = GetTimeSpan(model.ServiceEndTimeString);
                    serviceRecord.Province = model.Record.Province;
                    serviceRecord.Sex = model.Record.Sex;
                    serviceRecord.Notes = model.Record.Notes;
                    serviceRecord.Comment = string.IsNullOrEmpty(model.Record.Comment) ? null : model.Record.Comment.Replace("’", "'").Trim();
                    serviceRecord.ClaimType = model.Record.ClaimType;
                    serviceRecord.ServiceLocation = model.Record.ServiceLocation;

                    if (!string.IsNullOrEmpty(model.Record.ReferringDoctorNumber))
                    {
                        serviceRecord.ReferringDoctorNumber = model.Record.ReferringDoctorNumber.Trim();
                    }

                    serviceRecord.DateOfBirth = GetDateTimeFromCustom(model.BirthDateString);

                    var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
                    var currentDate = DateTime.UtcNow.AddHours(timeZoneOffset).Date;
                    if (serviceRecord.DateOfBirth > currentDate)
                    {
                        serviceRecord.DateOfBirth = serviceRecord.DateOfBirth.AddYears(-100);
                    }
                    
                    serviceRecord.ServiceDate = GetDateTimeFromCustom(model.ServiceDateString);

                    if (!string.IsNullOrEmpty(model.DischargeDateString))
                    {
                        serviceRecord.DischargeDate = GetDateTimeFromCustom(model.DischargeDateString);
                    }
                    else
                    {
                        serviceRecord.DischargeDate = null;
                    }
                                        
                    if (!isEdit)
                    {
                        var nextNumberModel = _repository.GetNextClaimNumber(_userId);
                        serviceRecord.ClaimNumber = nextNumberModel.NextClaimNumber;
                        serviceRecord.RollOverNumber = nextNumberModel.RollOverNumber;                        
                    }

                    _repository.InsertOrUpdate(serviceRecord);

                    #region Unit Record Code                                     
                     
                    var unitRecordList = new List<UnitRecord>();

                    var containHospitalCareCodes = false;

                    var containManuaEntryRecords = false;

                    var containRequiredRefDocCode = false;

                    for (int i = 1; i < 8; i++)
                    {
                        var index = i.ToString();
                        var myUnitCode = GetPropValue(model, "UnitCode" + index).ToUpper();
                        var myUnitAmount = 0.0d;
                        if (IsUnitCodeValid(model.Record.ClaimType, myUnitCode, out myUnitAmount))
                        {
                            var unitNumber = GetPropValue(model, "UnitNumber" + index);
                            var myUnitNumber = 0;
                            if (StaticCodeList.MySpecialCodeList.ContainsKey(myUnitCode) || IsUnitNumberValid(unitNumber, out myUnitNumber))
                            {
                                if (StaticCodeList.MySpecialCodeList.ContainsKey(myUnitCode) || (myUnitNumber > 0 && myUnitAmount > -1))
                                {
                                    var myDiagCode = GetPropValue(model, "DiagCode" + index);
                                    if (IsDiagCodeValid(myDiagCode))
                                    {
                                        StaticCodeList.FeeCodeModel feeCodeModel = null;

                                        if (StaticCodeList.MyFeeCodeList.ContainsKey(myUnitCode.ToUpper()))
                                        {
                                            feeCodeModel = StaticCodeList.MyFeeCodeList[myUnitCode.ToUpper()];
                                        }

                                        var myExplainCode = GetPropValue(model, "ExplainCode" + index);
                                        var myExplainCode2 = GetPropValue(model, "ExplainCode" + index + "b");
                                        var myExplainCode3 = GetPropValue(model, "ExplainCode" + index + "c");
                                        var myRecordClaimType = GetPropValue(model, "RecordClaimType" + index);
                                        var mySpecialCircumstanceIndicator = GetPropValue(model, "SpecialCircumstanceIndicator" + index);
                                        var myBilateralIndicator = GetPropValue(model, "BilateralIndicator" + index);
                                        var myStartTimeString = GetPropValue(model, "UnitStartTime" + index);
                                        var myEndTimeString = GetPropValue(model, "UnitEndTime" + index);
                                        var myUnitPremiumCode = GetPropValue(model, "UnitPremiumCode" + index);
                                        var myUnitSubmittedRecordIndex = GetPropValue(model, "UnitSubmittedRecordIndex" + index);

                                        var unitRecord = new UnitRecord();
                                        unitRecord.RecordIndex = i;

                                        if (string.IsNullOrEmpty(myUnitSubmittedRecordIndex))
                                        {
                                            unitRecord.SubmittedRecordIndex = i;
                                        }
                                        else
                                        {
                                            unitRecord.SubmittedRecordIndex = long.Parse(myUnitSubmittedRecordIndex);
                                        }

                                        unitRecord.ServiceRecordId = serviceRecord.ServiceRecordId;
                                        unitRecord.UnitCode = myUnitCode.ToUpper();
                                        unitRecord.RunCode = GetPropValue(model, "RunCode" + index);

                                        if (StaticCodeList.MySpecialCodeList.ContainsKey(myUnitCode))
                                        {
                                            unitRecord.UnitNumber = int.MinValue;
                                            unitRecord.UnitAmount = double.MinValue;
                                        }
                                        else
                                        {
                                            if (myUnitAmount == 0.0d)
                                            {
                                                containManuaEntryRecords = true;

                                                var unitAmountProp = GetPropValue(model, "UnitAmount" + index);
                                                var unitAmount = 0.0d;

                                                if (double.TryParse(unitAmountProp, out unitAmount))
                                                {
                                                    unitAmount = Math.Round(unitAmount, 2, MidpointRounding.AwayFromZero);

                                                    if (unitAmount > 0.0d)
                                                    {
                                                        unitRecord.UnitNumber = 1;
                                                        unitRecord.UnitAmount = unitAmount;
                                                    }
                                                    else
                                                    {
                                                        ModelState.AddModelError("UnitAmount" + i.ToString(), "Please enter a correct value for Unit Amount " + i.ToString());
                                                    }
                                                }
                                                else
                                                {
                                                    ModelState.AddModelError("UnitAmount" + i.ToString(), "Please enter a correct value for Unit Amount " + i.ToString());
                                                }
                                            }
                                            else
                                            {
                                                unitRecord.UnitNumber = myUnitNumber;
                                                unitRecord.UnitAmount = Math.Round(myUnitAmount * myUnitNumber, 2, MidpointRounding.AwayFromZero);
                                            }
                                        }

                                        if (unitRecord.UnitAmount > 9999.99d)
                                        {
                                            unitRecord.UnitAmount = 9999.99d;
                                        }

                                        if (StaticCodeList.MyCareCodeList.Contains(unitRecord.UnitCode))
                                        {
                                            containHospitalCareCodes = true;
                                            unitRecord.UnitPremiumCode = "2";
                                            unitRecord.PaidAmount = unitRecord.UnitAmount;                                            
                                        }
                                        else
                                        {
                                            unitRecord.UnitPremiumCode = myUnitPremiumCode;
                                            unitRecord.PaidAmount = GetAmountWithPremium(unitRecord.UnitCode, unitRecord.UnitAmount, unitRecord.UnitPremiumCode, serviceRecord.ServiceLocation);
                                        }
                                        
                                        if (!string.IsNullOrEmpty(myExplainCode))
                                        {
                                            unitRecord.ExplainCode = myExplainCode;
                                        }

                                        if (!string.IsNullOrEmpty(myExplainCode2))
                                        {
                                            unitRecord.ExplainCode2 = myExplainCode2;
                                        }

                                        if (!string.IsNullOrEmpty(myExplainCode3))
                                        {
                                            unitRecord.ExplainCode3 = myExplainCode3;
                                        }

                                        unitRecord.DiagCode = myDiagCode.ToUpper();

                                        if (!string.IsNullOrEmpty(myRecordClaimType))
                                        {
                                            unitRecord.RecordClaimType = myRecordClaimType;
                                        }

                                        if (!string.IsNullOrEmpty(mySpecialCircumstanceIndicator))
                                        {
                                            unitRecord.SpecialCircumstanceIndicator = mySpecialCircumstanceIndicator;
                                            if (feeCodeModel != null && feeCodeModel.FeeDeterminant != "W" && feeCodeModel.FeeDeterminant != "X" && mySpecialCircumstanceIndicator != "TA")
                                            {
                                                ModelState.AddModelError("UnitCode" + i.ToString(), "Selected Unit Code " + i.ToString() + " does not allow for Special Circumstance");
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(myBilateralIndicator))
                                        {
                                            unitRecord.BilateralIndicator = myBilateralIndicator;
                                        }
                                        
                                        if (!string.IsNullOrEmpty(myStartTimeString))
                                        {
                                            if (GetTimeSpan(myStartTimeString) == null)
                                            {
                                                ModelState.AddModelError("UnitStartTime" + i.ToString(), "Please enter a correct value for Unit Start Time " + i.ToString());
                                            }
                                            else
                                            {
                                                unitRecord.StartTime = GetTimeSpan(myStartTimeString);
                                            }
                                        }
                                        else
                                        {
                                            if (feeCodeModel != null && feeCodeModel.RequiredStartTime == "Y")
                                            {
                                                ModelState.AddModelError("UnitStartTime" + i.ToString(), "Please enter a value for Unit Start Time " + i.ToString());
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(myEndTimeString))
                                        {
                                            if (GetTimeSpan(myEndTimeString) == null)
                                            {
                                                ModelState.AddModelError("UnitEndTime" + i.ToString(), "Please enter a correct value for Unit End Time " + i.ToString());
                                            }
                                            else
                                            {
                                                unitRecord.EndTime = GetTimeSpan(myEndTimeString);
                                            }
                                        }
                                        else
                                        {
                                            if (feeCodeModel != null && feeCodeModel.RequiredStartTime == "Y")
                                            {
                                                ModelState.AddModelError("UnitEndTime" + i.ToString(), "Please enter a value for Unit End Time " + i.ToString());
                                            }
                                        }

                                        if (feeCodeModel != null && feeCodeModel.RequiredReferringDoc == "Y")
                                        {
                                            containRequiredRefDocCode = true;
                                        }

                                        unitRecordList.Add(unitRecord);
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("DiagCode" + i.ToString(), "Please enter a correct value for Diag Code " + i.ToString());
                                    }
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("UnitNumber" + i.ToString(), "Please enter a correct value for Unit Number " + i.ToString());
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("UnitCode" + i.ToString(), "Please enter a correct code for Unit Code " + i.ToString());
                        }
                    }

                    if (containManuaEntryRecords && string.IsNullOrEmpty(model.Record.Comment))
                    {
                        ModelState.AddModelError("Record.Comment", "Please enter a comment since a manual entry amount was entered");
                    }

                    if (containRequiredRefDocCode && string.IsNullOrEmpty(model.Record.ReferringDoctorNumber))
                    {
                        ModelState.AddModelError("Record.ReferringDoctorNumber", "Please select a referring doctor since one of the selected unit codes required it");
                    }

                    #endregion

                    if (ModelState.AsQueryable().Count(f => f.Value.Errors.Count > 0) == 0)
                    {
                        var specialUnitRecord = unitRecordList.FirstOrDefault(x => x.UnitNumber == int.MinValue && x.UnitAmount == double.MinValue);
                        if (specialUnitRecord != null)
                        {
                            if (StaticCodeList.MySpecialCodeList.ContainsKey(specialUnitRecord.UnitCode.ToUpper()))
                            {
                                unitRecordList.Remove(specialUnitRecord);
                                specialUnitRecord.UnitNumber = 1;
                                                                
                                var amount = unitRecordList.Where(x => !StaticCodeList.MyPremiumCodeList.Contains(x.UnitCode) && 
                                    !StaticCodeList.MyWCBFeeCodeList.ContainsKey(x.UnitCode + " - WCB")).Sum(x => x.UnitAmount);
                                
                                specialUnitRecord.UnitAmount = Math.Round(amount * StaticCodeList.MySpecialCodeList[specialUnitRecord.UnitCode], 2, MidpointRounding.AwayFromZero);
                                
                                if (specialUnitRecord.UnitAmount > 9999.99d)
                                {
                                    specialUnitRecord.UnitAmount = 9999.99d;
                                }

                                specialUnitRecord.PaidAmount = GetAmountWithPremium(specialUnitRecord.UnitCode, specialUnitRecord.UnitAmount, specialUnitRecord.UnitPremiumCode, serviceRecord.ServiceLocation);

                                unitRecordList.Add(specialUnitRecord);
                            }                         
                        }

                        if (containHospitalCareCodes)
                        {
                            //Need to set the record index properly. Visit / Procedure code need to ABOVE the hospital care codes
                            var index = 1;
                            var myVisitRecordList = unitRecordList.Where(x => !StaticCodeList.MyCareCodeList.Contains(x.UnitCode));

                            foreach(var unitRecord in myVisitRecordList)
                            {
                                unitRecord.RecordIndex = index;
                                unitRecord.SubmittedRecordIndex = index;
                                index++;
                            }

                            var myHospitalRecordList = unitRecordList.Where(x => StaticCodeList.MyCareCodeList.Contains(x.UnitCode));
                            foreach (var unitRecord in myHospitalRecordList)
                            {
                                unitRecord.RecordIndex = index;
                                unitRecord.SubmittedRecordIndex = index;
                                index++;
                            }
                        }

                        if (isEdit)
						{
                            _repository.DeleteAllUnitRecords(serviceRecord.ServiceRecordId);
						}

                        serviceRecord.ClaimAmount = unitRecordList.Sum(x => x.PaidAmount);

                        if (!string.IsNullOrEmpty(model.ButtonUsedToSubmit) && model.ButtonUsedToSubmit.Equals("SaveAsResubmit"))
                        {
                            serviceRecord.RejectedClaimId = null;
                            serviceRecord.CPSClaimNumber = null;
                            serviceRecord.ClaimsInId = null;

                            var nextClaimNumberModel = _repository.GetNextClaimNumber(_userId);
                            serviceRecord.ClaimNumber = nextClaimNumberModel.NextClaimNumber;
                            serviceRecord.RollOverNumber = nextClaimNumberModel.RollOverNumber;
                        }
                        
                        try
                        {
                            _repository.Save();
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                        {
                            foreach (var validationErrors in dbEx.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    System.Diagnostics.Debug.WriteLine("Property: {0} Error: {1}",
                                                            validationError.PropertyName,
                                                            validationError.ErrorMessage);
                                }
                            }
                        }

                        foreach (var unitRecord in unitRecordList)
                        {                            
                            _repository.InsertOrUpdate(unitRecord);
                            _repository.Save();
                        }

                        StoreLastEnterServiceDate(serviceRecord.ServiceDate);
                        StoreLastselectedDiagCode(model.LastSelectedDiagCode);

                        if (!string.IsNullOrEmpty(model.ReferFrom) && model.ReferFrom.Equals("SearchClaims", StringComparison.OrdinalIgnoreCase))
                        {
                            return RedirectToAction("SearchClaims");
                        }
                        else if (model.Record.ClaimStatus == (int) SearchClaimType.Paid)
                        {
                            return RedirectToAction("PaidClaim");
                        }
                        else if (model.Record.ClaimStatus == (int)SearchClaimType.Rejected)
                        {
                            return RedirectToAction("RejectedClaim");
                        }
                        if (!string.IsNullOrEmpty(model.ButtonUsedToSubmit) && model.ButtonUsedToSubmit.Equals("SaveAndAddNew"))
                        {
                            return RedirectToAction("Create");
                        }
                        else
                        {
                            return RedirectToAction("NotSubmitted");
                        }

                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(model.ButtonUsedToSubmit) && model.ButtonUsedToSubmit.Equals("SaveAndAddNew"))
                {
                    return RedirectToAction("Create");
                }
                else
                {
                    return RedirectToAction("NotSubmitted");
                }
            }

            return null;
		}

		#endregion

		#region Validation

		private bool IsServiceDateValid(string inputDateString, bool needToValidatePassToday)
        {
            var result = false;
            if (!string.IsNullOrEmpty(inputDateString) && (inputDateString.Length == 6 || inputDateString.Length == 4))
            {
                var myDate = GetDateTimeFromCustom(inputDateString);

                if (needToValidatePassToday)
                {
                    var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();

                    var endDate = DateTime.UtcNow.AddHours(timeZoneOffset).Date;
                    if (myDate >= endDate.AddYears(-4) && myDate <= endDate)
                    {
                        result = true;
                    }
                }
                else
                {
                    result = myDate != DateTime.MinValue;
                }              
            }

            return result;
        }

        private bool IsSexValid(ServiceRecordDetailModel model)
        {
            var result = false;
            if (!string.IsNullOrEmpty(model.Record.Sex) && model.Record.Sex.Length == 1)
            {
                if (model.Record.Sex.Equals("M", StringComparison.OrdinalIgnoreCase) || model.Record.Sex.Equals("F", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                }
            }

            return result;
        }
        
        private bool IsBirthDateValid(ServiceRecordDetailModel model)
        {
            var result = false;
            if (!string.IsNullOrEmpty(model.BirthDateString) && model.BirthDateString.Length == 4)
            {
                var myDate = GetDateTimeFromCustom(model.BirthDateString);               
                if (myDate > DateTime.MinValue)
                {                    
                    result = true;
                }
            }

            return result;
        }

        private bool IsDoctorNumberValid(ServiceRecordDetailModel model)
        {
            var result = false;
            var validNumbers = new [] { "9908", "9907", "9909", "9906", "9905", "9900" };

            if (!string.IsNullOrEmpty(model.Record.ReferringDoctorNumber))
            {
                var refDocNumber = model.Record.ReferringDoctorNumber.Trim();
                if (!validNumbers.Contains(refDocNumber))
                {
                    result = StaticCodeList.MyRefDocList.ContainsKey(refDocNumber);
                    if (!result)
                    {
                        if (refDocNumber.Length > 4)
                        {
                            result = false;
                        }
                        else
                        {
                            try
                            {
                                int.Parse(refDocNumber);
                                result = true;
                            }
                            catch
                            {
                                result = false;
                            }
                        }
                    }
                }
                else
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }
        
        private bool IsHospitalNumberValid(ServiceRecordDetailModel model)
        {
            var result = false;

            var province = model.Record.Province.ToUpper();
            var hospitalNumber = model.Record.HospitalNumber.Trim().ToUpper();

            if (province.Equals("QC"))
            {
                result = false;
            }
            else if (province.Equals("NT") || province.Equals("NU"))
            {
                var leadChar = hospitalNumber.Substring(0, 1);
                var restHealthNumber = hospitalNumber.Substring(1);
                long myNumber = 0;
                long.TryParse(restHealthNumber, out myNumber);

                if (myNumber > 0)
                {
                    if (province.Equals("NT"))
                    {
                        if ((leadChar == "D" || leadChar == "H" || leadChar == "M" || leadChar == "N" || leadChar == "T") && hospitalNumber.Length == 8)
                        {
                            result = true;
                        }
                    }
                    else if (province.Equals("NU"))
                    {
                        if (leadChar == "1" && hospitalNumber.Length == 9)
                        {
                            result = true;
                        }
                    }
                }
            }           
            else
			{
                long myNumber = 0;
                long.TryParse(hospitalNumber, out myNumber);

                if (myNumber > 0)
                {
                    if (province.Equals("SK") && hospitalNumber.Length == 9)
                    {
                        string myText = hospitalNumber.Substring(0, 8);
                        int myRemainder = GetModulus11Remainder(myText);

                        if (myRemainder > 0)
                            myRemainder = 11 - myRemainder;

                        //Append Check Digit
                        myText += myRemainder.ToString();

                        myRemainder = GetModulus11Remainder(myText);

                        if (myRemainder == 0)
                        {
                            if (myText == hospitalNumber)
                            {
                                result = true;
                            }
                        }
                    }
                    else if ((province.Equals("BC") || province.Equals("ON") || province.Equals("NS")) && hospitalNumber.Length == 10)
                    {                     
                        result = true;
                    }
                    else if ((province.Equals("AB") || province.Equals("MB") || province.Equals("NB") || province.Equals("YT")) && hospitalNumber.Length == 9)
                    {
                        result = true;
                    }
                    else if (province.Equals("PE") && hospitalNumber.Length == 8)
                    {
                        result = true;
                    }
                    else if (province.Equals("NL") && hospitalNumber.Length == 12)
                    {
                        result = true;
                    }
                }				
			}
                        
			return result;
		}

        private bool IsAtLeastOneUnitRecord(ServiceRecordDetailModel model)
        {
            var result = true;
            if (string.IsNullOrEmpty(model.UnitCode1) && string.IsNullOrEmpty(model.UnitCode2) && string.IsNullOrEmpty(model.UnitCode3) &&
                string.IsNullOrEmpty(model.UnitCode4) && string.IsNullOrEmpty(model.UnitCode5) && string.IsNullOrEmpty(model.UnitCode6))
            {
                return false;
            }
            
            return result;
        }

        private bool IsOnlyOneSpecialUnitRecord(ServiceRecordDetailModel model)
        {
            var counter = 0;
            for (int i = 1; i < 8; i++)
            {
                var index = i.ToString();
                var myUnitCode = GetPropValue(model, "UnitCode" + index);
                if (myUnitCode != null && StaticCodeList.MySpecialCodeList.ContainsKey(myUnitCode.ToUpper()))
                {
                    counter++;
                }
            }

            return counter < 2;
        }

        private bool IsFiveWCBClaimRecords(ServiceRecordDetailModel model)
        {
            var result = true;
            if (model.Record.ClaimType == (int)ClaimType.WCB && 
                (model.PremiumCode.Equals("b", StringComparison.OrdinalIgnoreCase) || 
                model.PremiumCode.Equals("k", StringComparison.OrdinalIgnoreCase)))
            {
                var counter = 0;
                for (int i = 1; i < 8; i++)
                {
                    var index = i.ToString();
                    var myUnitCode = GetPropValue(model, "UnitCode" + index);
                    if (!string.IsNullOrEmpty(myUnitCode))
                    {
                        counter++;
                    }
                }

                if (counter > 5)
                {
                    result = false;
                }
            }

            return result;
        }

        private int IsNotWCBRecordForMSBClaimRecord(ServiceRecordDetailModel model)
        {
            if ((ClaimType)model.Record.ClaimType == ClaimType.MSB)
            {
                for (int i = 1; i < 8; i++)
                {
                    var index = i.ToString();
                    var myUnitCode = (GetPropValue(model, "UnitCode" + index) + " - WCB").ToUpper();
                    if (StaticCodeList.MyWCBFeeCodeList.ContainsKey(myUnitCode))
                    {
                        return i;
                    }
                }
            }

            return 0;
        }
        
        private IEnumerable<int> GetNumberOfHospitalCareCode(ServiceRecordDetailModel model)
        {
            var result = new List<int>();
            for (int i = 1; i < 8; i++)
            {
                var index = i.ToString();
                var myUnitCode = GetPropValue(model, "UnitCode" + index);
                if (!string.IsNullOrEmpty(myUnitCode))
                {
                    myUnitCode = myUnitCode.TrimStart('0').ToUpper();
                    if (StaticCodeList.MyCareCodeList.Contains(myUnitCode))
                    {
                        result.Add(i);
                    }
                }
            }

            return result;
        }
        
        private int GetModulus11Remainder(string myValue)
        {
            int myNumber = 0;
            for (int i = 0, j = 9; i < myValue.Length; i++, j--)
                myNumber += j * int.Parse(myValue[i].ToString());

            myNumber = myNumber % 11;

            return myNumber;
        }

        private bool IsUnitCodeValid(int claimType, string myUnitCode, out double feeAmount)
        {
            var result = false;
            feeAmount = 0.0d;

            if (!string.IsNullOrEmpty(myUnitCode))
            {
                myUnitCode = myUnitCode.ToUpper();
                if (StaticCodeList.MyFeeCodeList.ContainsKey(myUnitCode))
                {                        
                    feeAmount = Convert.ToDouble(StaticCodeList.MyFeeCodeList[myUnitCode].FeeAmount.ToString());
                    result = true;
                }
                else if ((ClaimType)claimType == ClaimType.WCB)
                {
                    myUnitCode = myUnitCode + " - WCB";
                    if (StaticCodeList.MyWCBFeeCodeList.ContainsKey(myUnitCode))
                    {
                        feeAmount = Convert.ToDouble(StaticCodeList.MyWCBFeeCodeList[myUnitCode].FeeAmount);
                        result = true;
                    }
                }               
            }
            else
            {
                result = true;
            }

            return result;
        }

        private bool IsUnitNumberValid(string myUnitNumber, out int unitNumber)
        {
            var result = false;                       
            unitNumber = 0;

            if (!string.IsNullOrEmpty(myUnitNumber))
            {
                int.TryParse(myUnitNumber, out unitNumber);

                if (unitNumber > 0)
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        private bool IsDiagCodeValid(string myDiagCode)
        {
            var result = false;

            if (!string.IsNullOrEmpty(myDiagCode))
            {                
                if (StaticCodeList.MyICDList.ContainsKey(myDiagCode.ToUpper()))
                {
                    result = true;
                }             
            }

            return result;
        }

        private bool IsTotalUnitAmountValid(string myTotalAmount, float unitCodeAmount, int unitNumber)
        {
            float totalAmount;

            float.TryParse(myTotalAmount, out totalAmount);

            return (unitCodeAmount * unitNumber) == totalAmount;   
        }

        private DateTime GetDateTimeFromCustom(string myCustomText)
        {
            DateTime myDateTime = DateTime.MinValue;
            try
            {
                int myDay = 0;
                int myMonth = 0;
                int myYear = 0;

                if (myCustomText.Length > 4)
                {
                    int.TryParse(myCustomText.Substring(0, 2), out myDay);
                    int.TryParse(myCustomText.Substring(2, 2), out myMonth);
                    int.TryParse(myCustomText.Substring(4, 2), out myYear);
                }
                else
                {
                    myDay = 1;
                    int.TryParse(myCustomText.Substring(0, 2), out myMonth);
                    int.TryParse(myCustomText.Substring(2, 2), out myYear);					
                }

				myYear = GetFullYear(myYear);

                myDateTime = new DateTime(myYear, myMonth, myDay);
            }
            catch (Exception)
            {
                myDateTime = DateTime.MinValue;
            }

            return myDateTime;
        }

        private int GetFullYear(int myTwoDigitYear)
        {
            var firstTwoDigit = DateTime.UtcNow.AddHours(_timeZoneOffset).Year.ToString().Substring(0, 2);
            return int.Parse(firstTwoDigit + myTwoDigitYear.ToString().PadLeft(2, '0'));
        }

		private TimeSpan? GetTimeSpan(string myValue)
		{            
			int myTime = 0;
			int.TryParse(myValue, out myTime);

            if ((myTime == 0 && myValue != "0000") || myValue.Length < 4)
            {
                return null;
            }
            else
            {
                int myHour = int.Parse(myValue.Substring(0, 2));
                int myMin = int.Parse(myValue.Substring(2, 2));
                if (myHour >= 0 && myHour < 24 && myMin >= 0 && myMin < 60)
                {
                    return new TimeSpan(myHour, myMin, 0);
                }
            }

            return null;
		}

        private string GetPropValue(ServiceRecordDetailModel model, string propName)
        {
            var propList = model.GetType().GetProperties();
            var returnValue = (string)propList.FirstOrDefault(x => x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)).GetValue(model, null);

            return returnValue == null ? string.Empty : returnValue;
        }

        private void SetPropValue(ServiceRecordDetailModel model, string propName, string propValue)
        {
            var propList = model.GetType().GetProperties();
            propList.FirstOrDefault(x => x.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)).SetValue(model, propValue);
        }
        
        #endregion

        #region Paid Claim

        public ActionResult PaidClaim()
		{
            return View();
        } 

		#endregion

		#region Rejected Claim

		public ActionResult RejectedClaim()
		{
            ViewBag.ReturnRecordIndex = string.Empty;
            ViewBag.TimeZoneOffseet = _timeZoneOffset;
            ViewBag.CurrentDate = DateTime.UtcNow.AddHours(_timeZoneOffset);

            var serviceRecordList = _repository.GetAllSimpleRejectedServiceRecords(_userId);
            return View(serviceRecordList);
        }

        #endregion

        #region Search Claims

        public ActionResult SearchClaims()
        {
            ViewBag.IsAdmin = _isAdmin;
            return View();
        }

        public ActionResult SearchClaimsBeta(string hsn, string showClaimsType)
        {
            ViewBag.IsAdmin = _isAdmin;
            ViewBag.HSN = hsn;
            ViewBag.ShowClaimsType = showClaimsType;

            return View();
        }

        #endregion

        #region Helper
        
        private double GetAmountWithPremium(string unitCode, double amount, string premiumCode, string serviceLocation)
        {
            var result = 0.0d;
            if (!StaticCodeList.MyPremiumCodeList.Contains(unitCode))
            {
                if (premiumCode.Equals("b", StringComparison.OrdinalIgnoreCase))
                {
                    result = Math.Round(0.5d * amount, 2, MidpointRounding.AwayFromZero);
                }
                else if (premiumCode.Equals("k", StringComparison.OrdinalIgnoreCase))
                {
                    result = amount;
                }
                else if (premiumCode.Equals("f", StringComparison.OrdinalIgnoreCase))
                {
                    result = Math.Round(0.1d * amount, 2, MidpointRounding.AwayFromZero);
                }
            }

            var result2 = 0.0d;
            if (!string.IsNullOrEmpty(serviceLocation) && !StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList.Contains(unitCode) && 
                    serviceLocation.Equals("X", StringComparison.OrdinalIgnoreCase))
            {
                result2 = Math.Round(0.15d * amount, 2, MidpointRounding.AwayFromZero);
            }

            return amount + result + result2;
        }

        private ClaimsInType GetClaimType(ServiceRecord record)
		{
			var result = ClaimsInType.ClaimsIn;
			if (record.PaidClaimId.HasValue)
			{
				result = ClaimsInType.PaidClaim;
			}
			else if (record.RejectedClaimId.HasValue)
			{
				result = ClaimsInType.RejectedClaim;
			}

			return result;
		}

        private void StoreLastEnterServiceDate(DateTime serviceDate)
        {
            HttpCookie myCookie = HttpContext.Request.Cookies["LastEnterServiceDate"] ?? new HttpCookie("LastEnterServiceDate");
            myCookie.Value = serviceDate.ToString("yyyy-MM-dd");
            myCookie.Expires = DateTime.UtcNow.AddHours(_timeZoneOffset).AddDays(1);
            myCookie.SameSite = SameSiteMode.Lax;
            HttpContext.Response.Cookies.Add(myCookie);
        }

        private DateTime GetLastEnterServiceDate()
        {
            var cookie = HttpContext.Request.Cookies["LastEnterServiceDate"];
            var result = DateTime.UtcNow.AddHours(_timeZoneOffset);
            if (cookie != null)
            {
                try
                {
                    result = DateTime.Parse(cookie.Value);
                }
                catch
                {
                }
            }

            return result;
        }

        private void StoreLastselectedDiagCode(string diagCode)
        {
            HttpCookie myCookie = HttpContext.Request.Cookies["LastSelectedDiagCode"] ?? new HttpCookie("LastSelectedDiagCode");
            myCookie.Value = diagCode;
            myCookie.Expires = DateTime.UtcNow.AddHours(_timeZoneOffset).AddDays(1);
            myCookie.SameSite = SameSiteMode.Lax;
            HttpContext.Response.Cookies.Add(myCookie);
        }

        private string GetLastSelectedDiagCode(string profileDiagCode)
        {
            var cookie = HttpContext.Request.Cookies["LastSelectedDiagCode"];
            var result = profileDiagCode;
            if (cookie != null)
            {
                try
                {
                    result = cookie.Value;
                }
                catch
                {
                }
            }

            return result;
        }

        private string GetExplainCodeDesc(string explainCode)
        {
            var result = "There is no explaination for this code!";
            
            if (!string.IsNullOrEmpty(explainCode) && StaticCodeList.MyExplainCodeList.ContainsKey(explainCode))
            {
                result = StaticCodeList.MyExplainCodeList[explainCode].Replace("\"", "'");
            }

            return result;
        }

        #endregion

        #region JSON Service - Perform Search Claims

        [HttpPost]
        public JsonResult PerformSearchClaims(JQueryDataTableParam data)
        {
            var result = new SearchClaimResult();
            result.draw = data.draw;

            if (!string.IsNullOrEmpty(data.SearchClaimNumber) || !string.IsNullOrEmpty(data.SearchLastName) || !string.IsNullOrEmpty(data.SearchHSN) ||
                !string.IsNullOrEmpty(data.SearchServiceStartDateString) || !string.IsNullOrEmpty(data.SearchServiceEndDateString) ||
                !string.IsNullOrEmpty(data.SearchSubmissionStartDateString) || !string.IsNullOrEmpty(data.SearchSubmissionEndDateString) ||
                !string.IsNullOrEmpty(data.SearchExplainCode)
                )
            {
                #region Validation

                int? claimNumber = null;

                if (!string.IsNullOrEmpty(data.SearchClaimNumber))
                {
                    try
                    {
                        claimNumber = int.Parse(data.SearchClaimNumber);
                    }
                    catch
                    { }
                }

                CultureInfo provider = CultureInfo.InvariantCulture;

                DateTime? serviceStart = null;
                if (!string.IsNullOrEmpty(data.SearchServiceStartDateString))
                {
                    try
                    {
                        serviceStart = DateTime.ParseExact(data.SearchServiceStartDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                DateTime? serviceEnd = null;
                if (!string.IsNullOrEmpty(data.SearchServiceEndDateString))
                {
                    try
                    {
                        serviceEnd = DateTime.ParseExact(data.SearchServiceEndDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                DateTime? submissionStart = null;
                if (!string.IsNullOrEmpty(data.SearchSubmissionStartDateString))
                {
                    try
                    {
                        submissionStart = DateTime.ParseExact(data.SearchSubmissionStartDateString, "dd/MM/yyyy", provider);
                        submissionStart = submissionStart.Value.AddHours(6); //Submission Date is in UTC
                    }
                    catch
                    { }
                }

                DateTime? submissionEnd = null;
                if (!string.IsNullOrEmpty(data.SearchSubmissionEndDateString))
                {
                    try
                    {
                        submissionEnd = DateTime.ParseExact(data.SearchSubmissionEndDateString, "dd/MM/yyyy", provider);
                        submissionEnd = submissionEnd.Value.AddHours(6); //Submission Date is in UTC
                    }
                    catch
                    { }
                }

                #endregion

                var serviceRecords = _repository.SearchServiceRecords(_userId, claimNumber, data.SearchLastName, data.SearchHSN, (SearchClaimType)data.SearchClaimTypeList,
                    serviceStart, serviceEnd, submissionStart, submissionEnd, data.SearchExplainCode, _isAdmin);
                result.recordsTotal = serviceRecords.Count();
                result.recordsFiltered = result.recordsTotal;

                var displayOrder = data.order.FirstOrDefault();
                var columnToOrder = data.columns.ElementAt(displayOrder.column);

                IQueryable<ServiceRecord> orderedRecords;
                if (displayOrder.dir.Equals("asc"))
                {
                    switch (columnToOrder.data)
                    {
                        case "HospitalNumber":
                            orderedRecords = serviceRecords.OrderBy(x => x.HospitalNumber);
                            break;
                        case "LastName":
                            orderedRecords = serviceRecords.OrderBy(x => x.PatientLastName);
                            break;
                        case "FirstName":
                            orderedRecords = serviceRecords.OrderBy(x => x.PatientFirstName);
                            break;
                        case "ServiceDateString":
                            orderedRecords = serviceRecords.OrderBy(x => x.ServiceDate);
                            break;
                        case "SubmissionDateString":
                            orderedRecords = serviceRecords.OrderBy(x => x.ClaimsIn.CreatedDate);
                            break;
                        case "VarianceString":
                            orderedRecords = serviceRecords.OrderBy(x => x.VarianceAmount);
                            break;
                        default:
                            orderedRecords = serviceRecords.OrderBy(x => x.ClaimNumber);
                            break;
                    }
                }
                else
                {
                    switch (columnToOrder.data)
                    {
                        case "HospitalNumber":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.HospitalNumber);
                            break;
                        case "LastName":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.PatientLastName);
                            break;
                        case "FirstName":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.PatientFirstName);
                            break;
                        case "ServiceDateString":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.ServiceDate);
                            break;
                        case "SubmissionDateString":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.ClaimsIn.CreatedDate);
                            break;
                        case "VarianceString":
                            orderedRecords = serviceRecords.OrderByDescending(x => x.VarianceAmount);
                            break;
                        default:
                            orderedRecords = serviceRecords.OrderByDescending(x => x.ClaimNumber);
                            break;
                    }
                }

                var resultLength = data.length == -1 ? result.recordsTotal : data.length;

                var resultSet = orderedRecords.Skip(data.start).Take(resultLength)
                                                    .Select(x => new
                                                    {
                                                        x.ServiceRecordId,
                                                        x.ClaimNumber,
                                                        x.PatientFirstName,
                                                        x.PatientLastName,
                                                        x.HospitalNumber,
                                                        x.ServiceDate,
                                                        x.PaidClaimId,
                                                        x.RejectedClaimId,
                                                        x.ClaimsInId,
                                                        x.UserId,
                                                        x.CPSClaimNumber,
                                                        x.VarianceAmount,
                                                        x.UnitRecord,
                                                        x.ClaimsIn
                                                    }).ToList();

                IEnumerable<SearchResultItem> searchResult;
                if (_isAdmin)
                {
                    var users = _repository.GetUserNames(resultSet.Select(x => x.UserId));
                    searchResult = resultSet.Select(x => new SearchResultItem()
                    {
                        ClaimNumber = x.ClaimNumber.ToString(),
                        FirstName = x.PatientFirstName,
                        LastName = x.PatientLastName,
                        HospitalNumber = x.HospitalNumber,
                        ServiceDateString = string.Format("{0}/{1}/{2}", x.ServiceDate.ToString("dd"), x.ServiceDate.ToString("MM"), x.ServiceDate.ToString("yyyy")),
                        PaidOrRejected = !x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && string.IsNullOrEmpty(x.CPSClaimNumber) ? "Unsubmitted" :
                                         (
                                            x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && string.IsNullOrEmpty(x.CPSClaimNumber) ? "Submitted" :
                                            (
                                                !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber) ? "Pending" :
                                                (
                                                    x.PaidClaimId.HasValue ? "Paid" : "Rejected"
                                                )
                                            )
                                         ),
                        ServiceRecordId = x.ServiceRecordId.ToString(),
                        UserName = users.FirstOrDefault(y => y.UserId == x.UserId).UserName,
                        ExplainCodesString = CombineExplainCodes(x.UnitRecord),
                        FeeCodesString = string.Join(", ", x.UnitRecord.Select(y => y.UnitCode).Distinct().OrderBy(y => y)),
                        CPSClaimNumber = x.CPSClaimNumber,
                        SubmissionDateString = x.ClaimsIn != null ? 
                                                string.Format("{0}/{1}/{2}", 
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("dd"), 
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("MM"), 
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("yyyy")) : string.Empty,
                        VarianceString = string.Format("{0:C}", x.VarianceAmount),
                        DT_RowId = x.ServiceRecordId.ToString()
                    });
                }
                else
                {
                    searchResult = resultSet.Select(x => new SearchResultItem()
                    {
                        ClaimNumber = x.ClaimNumber.ToString(),
                        FirstName = x.PatientFirstName,
                        LastName = x.PatientLastName,
                        HospitalNumber = x.HospitalNumber,
                        ServiceDateString = string.Format("{0}/{1}/{2}", x.ServiceDate.ToString("dd"), x.ServiceDate.ToString("MM"), x.ServiceDate.ToString("yyyy")),
                        PaidOrRejected = !x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && string.IsNullOrEmpty(x.CPSClaimNumber) ? "Unsubmitted" :
                                         (
                                            x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && string.IsNullOrEmpty(x.CPSClaimNumber) ? "Submitted" :
                                            (
                                                !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber) ? "Pending" :
                                                (
                                                    x.PaidClaimId.HasValue ? "Paid" : "Rejected"
                                                )
                                            )
                                         ),
                        ServiceRecordId = x.ServiceRecordId.ToString(),
                        DT_RowId = x.ServiceRecordId.ToString(),
                        ExplainCodesString = CombineExplainCodes(x.UnitRecord),
                        FeeCodesString = string.Join(", ", x.UnitRecord.Select(y => y.UnitCode).Distinct().OrderBy(y => y)),
                        CPSClaimNumber = x.CPSClaimNumber,
                        VarianceString = string.Format("{0:C}", x.VarianceAmount),
                        SubmissionDateString = x.ClaimsIn != null ?
                                                string.Format("{0}/{1}/{2}",
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("dd"),
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("MM"),
                                                    x.ClaimsIn.CreatedDate.AddHours(_timeZoneOffset).ToString("yyyy")) : string.Empty,
                    });
                }

                result.data = searchResult.ToList();
            }
            else
            {
                result.recordsTotal = 0;
                result.data = Enumerable.Empty<SearchResultItem>();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region JSON Service - Perform Search Claims Beta

        [HttpPost]
        public JsonResult PerformSearchClaimsBeta(JQueryDataTableParam data)
        {
            var result = new SearchClaimResult();
            result.draw = data.draw;

            //if (!string.IsNullOrEmpty(data.SearchClaimNumber) || !string.IsNullOrEmpty(data.SearchLastName) || !string.IsNullOrEmpty(data.SearchHSN) ||
            //        !string.IsNullOrEmpty(data.SearchFirstName) || !string.IsNullOrEmpty(data.SearchClinicName) ||
            //        !string.IsNullOrEmpty(data.SearchServiceStartDateString) || !string.IsNullOrEmpty(data.SearchServiceEndDateString) ||
            //        !string.IsNullOrEmpty(data.SearchSubmissionStartDateString) || !string.IsNullOrEmpty(data.SearchSubmissionEndDateString) ||
            //        !string.IsNullOrEmpty(data.SearchDiagCode) || !string.IsNullOrEmpty(data.SearchUnitCode) || !string.IsNullOrEmpty(data.SearchCPSClaimNumber) ||
            //        !string.IsNullOrEmpty(data.SearchExplainCode)
            //    )

            if (data != null)
            {
                int? claimNumber = null;

                if (!string.IsNullOrEmpty(data.SearchClaimNumber))
                {
                    try
                    {
                        claimNumber = int.Parse(data.SearchClaimNumber);
                    }
                    catch
                    { }
                }

                CultureInfo provider = CultureInfo.InvariantCulture;

                DateTime? serviceStart = null;
                if (!string.IsNullOrEmpty(data.SearchServiceStartDateString))
                {
                    try
                    {
                        serviceStart = DateTime.ParseExact(data.SearchServiceStartDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                DateTime? serviceEnd = null;
                if (!string.IsNullOrEmpty(data.SearchServiceEndDateString))
                {
                    try
                    {
                        serviceEnd = DateTime.ParseExact(data.SearchServiceEndDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                DateTime? submissionStart = null;
                if (!string.IsNullOrEmpty(data.SearchSubmissionStartDateString))
                {
                    try
                    {
                        submissionStart = DateTime.ParseExact(data.SearchSubmissionStartDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                DateTime? submissionEnd = null;
                if (!string.IsNullOrEmpty(data.SearchSubmissionEndDateString))
                {
                    try
                    {
                        submissionEnd = DateTime.ParseExact(data.SearchSubmissionEndDateString, "dd/MM/yyyy", provider);
                    }
                    catch
                    { }
                }

                var searchClaimTypes = new List<int>();
                if (data.SearchUnsubmitted)
                {
                    searchClaimTypes.Add((int) SearchClaimType.Unsubmitted);
                }

                if (data.SearchSubmitted)
                {
                    searchClaimTypes.Add((int)SearchClaimType.Submitted);
                }

                if (data.SearchPending)
                {
                    searchClaimTypes.Add((int)SearchClaimType.Pending);
                }

                if (data.SearchPaid)
                {
                    searchClaimTypes.Add((int)SearchClaimType.Paid);
                }

                if (data.SearchRejected)
                {
                    searchClaimTypes.Add((int)SearchClaimType.Rejected);
                }

                if (data.SearchDeleted)
                {
                    searchClaimTypes.Add((int)SearchClaimType.Deleted);
                }
                
                var unitRecords = _repository.SearchUnitRecords(_userId, claimNumber, data.SearchLastName, data.SearchFirstName, data.SearchClinicName, data.SearchHSN, searchClaimTypes,
                    serviceStart, serviceEnd, submissionStart, submissionEnd, data.SearchExplainCode, data.SearchDiagCode, data.SearchUnitCode, data.SearchCPSClaimNumber, _isAdmin);
                result.recordsTotal = unitRecords.Count();
                result.recordsFiltered = result.recordsTotal;

                var displayOrder = data.order.FirstOrDefault();
                var columnToOrder = data.columns.ElementAt(displayOrder.column);

                IQueryable<ClaimsSearchView> orderedRecords;
                if (displayOrder.dir.Equals("asc"))
                {
                    #region Order By Case

                    switch (columnToOrder.data)
                    {
                        case "HospitalNumber":
                            orderedRecords = unitRecords.OrderBy(x => x.HospitalNumber);
                            break;
                        case "LastName":
                            orderedRecords = unitRecords.OrderBy(x => x.PatientLastName);
                            break;
                        case "FirstName":
                            orderedRecords = unitRecords.OrderBy(x => x.PatientFirstName);
                            break;
                        case "ServiceDateString":
                            orderedRecords = unitRecords.OrderBy(x => x.ServiceDate);
                            break;
                        case "SubmissionDateString":
                            orderedRecords = unitRecords.OrderBy(x => x.SubmissionDate);
                            break;
                        case "RunCode":
                            orderedRecords = unitRecords.OrderBy(x => x.RunCode);
                            break;
                        case "UnitCode":
                            orderedRecords = unitRecords.OrderBy(x => x.UnitCode);
                            break;
                        case "UnitNumber":
                            orderedRecords = unitRecords.OrderBy(x => x.UnitNumber);
                            break;
                        case "DiagCode":
                            orderedRecords = unitRecords.OrderBy(x => x.DiagCode);
                            break;
                        case "ExplainCode":
                            orderedRecords = unitRecords.OrderBy(x => x.ExplainCode);
                            break;
                        case "ExplainCode2":
                            orderedRecords = unitRecords.OrderBy(x => x.ExplainCode2);
                            break;
                        case "ExplainCode3":
                            orderedRecords = unitRecords.OrderBy(x => x.ExplainCode3);
                            break;
                        case "CPSClaimNumber":
                            orderedRecords = unitRecords.OrderBy(x => x.CPSClaimNumber);
                            break;
                        case "ClaimAmountString":
                            orderedRecords = unitRecords.OrderBy(x => x.ClaimAmount);
                            break;
                        case "PaidAmountString":
                            orderedRecords = unitRecords.OrderBy(x => x.PaidAmount);
                            break;
                        case "VarianceString":
                            orderedRecords = unitRecords.OrderBy(x => x.VarianceAmount);
                            break;
                        case "Status":
                            orderedRecords = unitRecords.OrderBy(x => x.ClaimStatusString);
                            break;
                        default:
                            orderedRecords = unitRecords.OrderBy(x => x.ClaimNumber);
                            break;
                    }

                    #endregion
                }
                else
                {
                    #region Order By Descending Case

                    switch (columnToOrder.data)
                    {
                        case "HospitalNumber":
                            orderedRecords = unitRecords.OrderByDescending(x => x.HospitalNumber);
                            break;
                        case "LastName":
                            orderedRecords = unitRecords.OrderByDescending(x => x.PatientLastName);
                            break;
                        case "FirstName":
                            orderedRecords = unitRecords.OrderByDescending(x => x.PatientFirstName);
                            break;
                        case "ServiceDateString":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ServiceDate);
                            break;
                        case "SubmissionDateString":
                            orderedRecords = unitRecords.OrderByDescending(x => x.SubmissionDate);
                            break;
                        case "RunCode":
                            orderedRecords = unitRecords.OrderByDescending(x => x.RunCode);
                            break;
                        case "UnitCode":
                            orderedRecords = unitRecords.OrderByDescending(x => x.UnitCode);
                            break;
                        case "UnitNumber":
                            orderedRecords = unitRecords.OrderByDescending(x => x.UnitNumber);
                            break;
                        case "DiagCode":
                            orderedRecords = unitRecords.OrderByDescending(x => x.DiagCode);
                            break;
                        case "ExplainCode":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ExplainCode);
                            break;
                        case "ExplainCode2":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ExplainCode2);
                            break;
                        case "ExplainCode3":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ExplainCode3);
                            break;
                        case "CPSClaimNumber":
                            orderedRecords = unitRecords.OrderByDescending(x => x.CPSClaimNumber);
                            break;
                        case "ClaimAmountString":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ClaimAmount);
                            break;
                        case "PaidAmountString":
                            orderedRecords = unitRecords.OrderByDescending(x => x.PaidAmount);
                            break;
                        case "VarianceString":
                            orderedRecords = unitRecords.OrderByDescending(x => x.VarianceAmount);
                            break;
                        case "Status":
                            orderedRecords = unitRecords.OrderByDescending(x => x.ClaimStatusString);
                            break;
                        default:
                            orderedRecords = unitRecords.OrderByDescending(x => x.ClaimNumber);
                            break;
                    }

                    #endregion
                }

                var resultLength = data.length == -1 ? result.recordsTotal : data.length;

                var searchResult = orderedRecords.Skip(data.start).Take(resultLength).ToList().Select(x => new SearchResultItem()
                {
                    UserName = x.UserName,
                    ClaimNumber = x.ClaimNumber.ToString(),
                    FirstName = x.PatientFirstName,
                    LastName = x.PatientLastName,
                    HospitalNumber = x.HospitalNumber,
                    ServiceDateString = string.Format("{0}/{1}/{2}", x.ServiceDate.ToString("dd"), x.ServiceDate.ToString("MM"), x.ServiceDate.ToString("yyyy")),
                    Status = x.ClaimStatusString,
                    ServiceRecordId = x.ServiceRecordId.ToString(),
                    CPSClaimNumber = x.CPSClaimNumber,
                    SubmissionDateString = x.SubmissionDate.HasValue ? string.Format("{0}/{1}/{2}", x.SubmissionDate.Value.ToString("dd"), x.SubmissionDate.Value.ToString("MM"), x.SubmissionDate.Value.ToString("yyyy")) : string.Empty,
                    ClaimAmountString = string.Format("{0:C}", x.ClaimAmount),
                    PaidAmountString = string.Format("{0:C}", x.PaidAmount),
                    VarianceString = string.Format("{0:C}", x.VarianceAmount),
                    ExplainCode = x.ExplainCode,
                    ExplainCode2 = x.ExplainCode2,
                    ExplainCode3 = x.ExplainCode3,
                    UnitCode = x.UnitCode,
                    UnitNumber = x.UnitNumber,
                    RunCode = x.RunCode,
                    DiagCode = x.DiagCode,
                    DT_RowId = x.ServiceRecordId.ToString()
                });

                result.data = searchResult.ToList();
            }
            else
            {
                result.recordsTotal = 0;
                result.data = Enumerable.Empty<SearchResultItem>();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region JSON Service - Perform Paid Claims

        [HttpPost]
        public JsonResult GetPaidClaims(JQueryDataTableParam data)
        {
            var result = new SearchClaimResult();
            result.draw = data.draw;

            var serviceRecords = _repository.GetAllPaidClaimServiceRecords(_userId);

            if (data.search != null && !string.IsNullOrEmpty(data.search.value))
            {
                var searchValue = data.search.value;
                var claimNumber = 0;
                int.TryParse(searchValue, out claimNumber);
                
                serviceRecords = serviceRecords.Where(x =>
                                    x.ClaimNumber == claimNumber || 
                                    x.HospitalNumber.Contains(searchValue) ||
                                    x.PatientLastName.Contains(searchValue) ||
                                    x.PatientLastName.Contains(searchValue));                
            }

            result.recordsTotal = serviceRecords.Count();
            result.recordsFiltered = result.recordsTotal;

            var displayOrder = data.order.FirstOrDefault();
            var columnToOrder = data.columns.ElementAt(displayOrder.column);
            
            IQueryable<ServiceRecord> orderedRecords;
            if (displayOrder.dir.Equals("asc"))
            {
                switch (columnToOrder.data)
                {
                    case "HospitalNumber":
                        orderedRecords = serviceRecords.OrderBy(x => x.HospitalNumber);
                        break;
                    case "LastName":
                        orderedRecords = serviceRecords.OrderBy(x => x.PatientLastName);
                        break;
                    case "FirstName":
                        orderedRecords = serviceRecords.OrderBy(x => x.PatientFirstName);
                        break;
                    case "ServiceDateString":
                        orderedRecords = serviceRecords.OrderBy(x => x.ServiceDate);
                        break;
                    case "PmtAppDateString":
                        orderedRecords = serviceRecords.OrderBy(x => x.PaymentApproveDate);
                        break;
                    case "VarianceString":
                        orderedRecords = serviceRecords.OrderBy(x => x.VarianceAmount);
                        break;
                    default:
                        orderedRecords = serviceRecords.OrderBy(x => x.ClaimNumber);
                        break;
                }
            }
            else
            {
                switch (columnToOrder.data)
                {
                    case "HospitalNumber":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.HospitalNumber);
                        break;
                    case "LastName":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.PatientLastName);
                        break;
                    case "FirstName":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.PatientFirstName);
                        break;
                    case "ServiceDateString":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.ServiceDate);
                        break;
                    case "PmtAppDateString":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.PaymentApproveDate);
                        break;
                    case "VarianceString":
                        orderedRecords = serviceRecords.OrderByDescending(x => x.VarianceAmount);
                        break;
                    default:
                        orderedRecords = serviceRecords.OrderByDescending(x => x.ClaimNumber);
                        break;
                }
            }

            var resultLength = data.length == -1 ? result.recordsTotal : data.length;

            var resultSet = orderedRecords.Skip(data.start).Take(resultLength).ToList()
                                                .Select(x => new
                                                {
                                                    x.ServiceRecordId,
                                                    x.ClaimNumber,
                                                    x.PatientFirstName,
                                                    x.PatientLastName,
                                                    x.HospitalNumber,
                                                    x.ServiceDate,
                                                    x.PaymentApproveDate,
                                                    x.ClaimAmount,
                                                    x.PaidAmount,
                                                    x.CPSClaimNumber,
                                                    x.VarianceAmount,
                                                    x.UnitRecord
                                                }).ToList();

            result.data = resultSet.Select(x => new SearchResultItem()
            {
                ClaimNumber = x.ClaimNumber.ToString(),
                FirstName = x.PatientFirstName,
                LastName = x.PatientLastName,
                HospitalNumber = x.HospitalNumber,
                ServiceDateString = string.Format("{0}/{1}/{2}", x.ServiceDate.ToString("dd"), x.ServiceDate.ToString("MM"), x.ServiceDate.ToString("yyyy")),
                PmtAppDateString = x.PaymentApproveDate.HasValue ?
                                    string.Format("{0}/{1}/{2}", x.PaymentApproveDate.Value.ToString("dd"), x.PaymentApproveDate.Value.ToString("MM"), x.PaymentApproveDate.Value.ToString("yyyy")) :
                                    string.Empty,
                ClaimAmountString = string.Format("{0:C}", x.ClaimAmount),
                PaidAmountString = string.Format("{0:C}", x.PaidAmount),
                VarianceString = string.Format("{0:C}", x.VarianceAmount),
                ColorRangeString = Math.Round(x.ClaimAmount, 2, MidpointRounding.AwayFromZero) > Math.Round(x.PaidAmount, 2, MidpointRounding.AwayFromZero) ?
                                    "timeSpanDanger" :
                                    Math.Round(x.ClaimAmount, 2, MidpointRounding.AwayFromZero) < Math.Round(x.PaidAmount, 2, MidpointRounding.AwayFromZero) ?
                                    "timeSpanStart" :
                                    string.Empty,
                ServiceRecordId = x.ServiceRecordId.ToString(),
                ExplainCodesString = CombineExplainCodes(x.UnitRecord),
                FeeCodesString = string.Join(", ", x.UnitRecord.Select(y => y.UnitCode).Distinct().OrderBy(y => y)),
                CPSClaimNumber = x.CPSClaimNumber
            }).ToList();
            
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private string CombineExplainCodes(IEnumerable<UnitRecord> unitRecords)
        {
            var codes = new List<string>();
            codes.AddRange(unitRecords.Select(x => x.ExplainCode));
            codes.AddRange(unitRecords.Select(x => x.ExplainCode2));
            codes.AddRange(unitRecords.Select(x => x.ExplainCode3));

            return string.Join(",", codes.Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x));
        }

        #endregion

        #region JSON Service - Record Service

        public bool Delete(string id)
		{
			var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
			{
				try
				{
                    var serviceRecord = _repository.GetRecord(serviceRecordId, _userId);
                    _repository.DeleteAllUnitRecords(serviceRecordId);
                    _repository.Delete(serviceRecord);

                    _repository.Save();
                    success = true;
				}
				catch (Exception)
				{
					success = false;
				}				
			}

            return success;
		}

        public bool ToIgnore(string id)
        {
            var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    _repository.UpdateServiceRecordsWithClaimToIgnore(_userId, new List<Guid>() {  serviceRecordId });
                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        [HttpPost]
        public bool ConvertToPaid(string id)
        {
            var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    var serviceRecord = _repository.GetRecordWithUnitRecords(serviceRecordId, _userId);

                    var paidServiceRecords = _repository.GetPaidRecordsWithUnitRecords(_userId, serviceRecord.ClaimNumber);

                    var matchedPaidRecord = paidServiceRecords.FirstOrDefault(x => x.HospitalNumber == serviceRecord.HospitalNumber);

                    if (matchedPaidRecord != null)
                    {
                        foreach(var unitRecord in serviceRecord.UnitRecord)
                        {
                            var newRecord = CloneUnitRecord(unitRecord);
                            newRecord.ServiceRecordId = matchedPaidRecord.ServiceRecordId;

                            if (!newRecord.SubmittedAmount.HasValue)
                            {
                                newRecord.SubmittedAmount = newRecord.UnitAmount;
                            }

                            matchedPaidRecord.UnitRecord.Add(newRecord);
                            _repository.InsertUnitRecord(newRecord);
                        }

                        var index = 1;
                        foreach(var unitRecord in matchedPaidRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            _repository.UpdateUnitRecord(unitRecord);

                            index++;
                        }

                        matchedPaidRecord.PaymentApproveDate = DateTime.UtcNow;
                        matchedPaidRecord.ClaimAmount = Math.Round(matchedPaidRecord.UnitRecord.Sum(x => GetAmountWithPremium(x.UnitCode, x.UnitAmount, x.UnitPremiumCode, serviceRecord.ServiceLocation)), 2, MidpointRounding.AwayFromZero);
                        matchedPaidRecord.PaidAmount = Math.Round(matchedPaidRecord.UnitRecord.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
                        _repository.InsertOrUpdate(matchedPaidRecord);

                        _repository.Delete(serviceRecord);

                        _repository.Save();                                               
                    }
                    else
                    {
                        //No Paid claim, then convert this claim to paid
                        var claimsInReturnId = _repository.GetClaimsInReturnId(serviceRecord);
                        if (claimsInReturnId != null && claimsInReturnId != Guid.Empty)
                        {
                            var paidClaim = new PaidClaim();
                            paidClaim.ClaimsInReturnId = claimsInReturnId;
                            paidClaim.CreatedDate = DateTime.UtcNow;
                            paidClaim.PaidClaimId = Guid.NewGuid();

                            //_repository.InsertPaidClaim(paidClaim);
                            _repository.AddPaidClaim(paidClaim);
                            
                            foreach(var unitRecord in serviceRecord.UnitRecord)
                            {
                                if (!unitRecord.SubmittedAmount.HasValue)
                                {
                                    unitRecord.SubmittedAmount = unitRecord.UnitAmount;
                                    _repository.InsertOrUpdate(unitRecord);
                                }                                                                
                            }

                            _repository.Save();

                            _repository.ConvertRejectedToPaid(
                                                serviceRecord.ServiceRecordId, 
                                                paidClaim.PaidClaimId,
                                                Math.Round(serviceRecord.UnitRecord.Sum(x => GetAmountWithPremium(x.UnitCode, x.UnitAmount, x.UnitPremiumCode, serviceRecord.ServiceLocation)), 2, MidpointRounding.AwayFromZero),
                                                Math.Round(serviceRecord.UnitRecord.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero)
                                            );
                        }
                    }
                    
                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                }
            }

            return success;
        }

        private UnitRecord CloneUnitRecord(UnitRecord mySource)
        {
            UnitRecord myUnitRecord = new UnitRecord();

            myUnitRecord.UnitRecordId = Guid.NewGuid();
            myUnitRecord.ServiceRecordId = mySource.ServiceRecordId;
            myUnitRecord.UnitCode = mySource.UnitCode;
            myUnitRecord.UnitNumber = mySource.UnitNumber;
            myUnitRecord.UnitAmount = mySource.UnitAmount;
            myUnitRecord.UnitPremiumCode = mySource.UnitPremiumCode;

            myUnitRecord.ExplainCode = mySource.ExplainCode;
            myUnitRecord.RecordIndex = mySource.RecordIndex;

            myUnitRecord.PaidAmount = mySource.PaidAmount;
            myUnitRecord.DiagCode = mySource.DiagCode;
            myUnitRecord.RunCode = mySource.RunCode;

            myUnitRecord.ExplainCode2 = mySource.ExplainCode2;
            myUnitRecord.ExplainCode3 = mySource.ExplainCode3;

            myUnitRecord.StartTime = mySource.StartTime;
            myUnitRecord.EndTime = mySource.EndTime;

            myUnitRecord.ProgramPayment = mySource.ProgramPayment;
            myUnitRecord.SubmittedAmount = mySource.SubmittedAmount;
            myUnitRecord.SubmittedRecordIndex = mySource.SubmittedRecordIndex;

            myUnitRecord.RecordClaimType = mySource.RecordClaimType;
            myUnitRecord.SpecialCircumstanceIndicator = mySource.SpecialCircumstanceIndicator;
            myUnitRecord.BilateralIndicator = mySource.BilateralIndicator;

            return myUnitRecord;
        }

        [HttpPost]
        public bool Activate(string id)
        {
            var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    var serviceRecord = _repository.GetRecord(serviceRecordId, _userId);
                    if (serviceRecord.PaidClaimId.HasValue)
                    {
                        serviceRecord.WCBFaxStatus = string.Empty;
                        _repository.InsertOrUpdate(serviceRecord);
                        _repository.Save();
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        [HttpPost]
        public bool Resubmit(string id)
        {
            var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    var serviceRecord = _repository.GetRecordWithUnitRecords(serviceRecordId, _userId);

                    foreach(var unitRecord in serviceRecord.UnitRecord)
                    {
                        var resubmitClaim = new ClaimsResubmitted();
                        resubmitClaim.ClaimNumber = serviceRecord.ClaimNumber;
                        resubmitClaim.CreatedDate = DateTime.UtcNow;
                        resubmitClaim.HospitalNumber = serviceRecord.HospitalNumber;
                        resubmitClaim.PatientLastName = serviceRecord.PatientLastName;
                        resubmitClaim.RecordId = Guid.NewGuid();
                        resubmitClaim.ServiceDate = serviceRecord.ServiceDate;
                        resubmitClaim.UnitCode = unitRecord.UnitCode;
                        resubmitClaim.UnitNumber = unitRecord.UnitNumber;
                        resubmitClaim.UnitPremiumCode = unitRecord.UnitPremiumCode;
                        resubmitClaim.UserId = serviceRecord.UserId;

                        _repository.Insert(resubmitClaim);
                    }

                    _repository.Save();

                    var nextNumberModel = _repository.GetNextClaimNumber(_userId);                                        
                    _repository.ResetSubmittedServiceRecord(serviceRecordId, nextNumberModel.RollOverNumber, nextNumberModel.NextClaimNumber);

                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                }
            }

            return success;
        }

        public ActionResult GenerateResubmission(string idList)
		{            
			var result = "fail";
			var inputIdList = idList.Split(',');
			if (inputIdList.Count() > 0)
			{
				IList<Guid> rejectedClaimServiceRecordIds = new List<Guid>();
				foreach(var id in inputIdList)
				{
					var serviceRecordId = Guid.Empty;
					Guid.TryParse(id, out serviceRecordId);
					if (serviceRecordId != Guid.Empty)
					{
						rejectedClaimServiceRecordIds.Add(serviceRecordId);
					}
				}

				if (rejectedClaimServiceRecordIds.Count() > 0)
				{					
                    var serviceRecords = _repository.GetServiceRecordByIds(rejectedClaimServiceRecordIds);                    
					var rejectedClaims = _repository.GetRejectedClaims(serviceRecords.Select(x => x.RejectedClaimId.Value).Distinct().ToList());
                    var claimsInReturns = _repository.GetClaimsInReturn(rejectedClaims.Select(x => x.ClaimsInReturnId).Distinct().ToList());
                    var nextNumberModel = _repository.GetNextClaimNumber(_userId);

                    var total = 0.0d;
					foreach (var record in serviceRecords)
					{
                        record.ClaimNumber = nextNumberModel.NextClaimNumber;
                        record.RollOverNumber = nextNumberModel.RollOverNumber;
                        var rejectClaim = rejectedClaims.FirstOrDefault(x => x.RejectedClaimId == record.RejectedClaimId.Value);
                        if (rejectClaim != null)
                        {                            
                            var returnClaim = claimsInReturns.FirstOrDefault(x => x.ClaimsInReturnId == rejectClaim.ClaimsInReturnId);
                            if (returnClaim != null)
                            {
                                returnClaim.TotalRejected--;
                            }
                        }                        

                        total += record.ClaimAmount;
                        _repository.SetServiceRecordToModified(record);

                        nextNumberModel.NextClaimNumber++;
                        if (nextNumberModel.NextClaimNumber > 99999)
                        {
                            nextNumberModel.NextClaimNumber = 10000;
                            nextNumberModel.RollOverNumber++;
                        }
                    }

                    foreach (var item in claimsInReturns)
                    {
                        _repository.UpdateClaimInReturn(item);
                    }

					_repository.Save();

                    _repository.ResetRejectedClaimIdForServiceRecords(rejectedClaimServiceRecordIds);
                    
					result = "success";
				}
			}			

			return Content(result);
		}

        #endregion

        #region JSON Service - Batch Operations

        [HttpPost]
        public bool BatchResubmission(string ids)
        {
            var success = false;

            var tempIds = ids.Split(',').Where(x => !string.IsNullOrEmpty(x)).Distinct();
            
            var serviceRecordIds = new List<Guid>();

            foreach (var id in tempIds)
            {
                Guid tempId;
                Guid.TryParse(id, out tempId);

                if (tempId != Guid.Empty)
                {
                    serviceRecordIds.Add(tempId);
                }
            }

            if (serviceRecordIds.Any())
            {
                try
                {
                    foreach (var serviceRecordId in serviceRecordIds)
                    {
                        var serviceRecord = _repository.GetRecordWithUnitRecords(serviceRecordId, _userId);

                        foreach (var unitRecord in serviceRecord.UnitRecord)
                        {
                            var resubmitClaim = new ClaimsResubmitted();
                            resubmitClaim.ClaimNumber = serviceRecord.ClaimNumber;
                            resubmitClaim.CreatedDate = DateTime.UtcNow;
                            resubmitClaim.HospitalNumber = serviceRecord.HospitalNumber;
                            resubmitClaim.PatientLastName = serviceRecord.PatientLastName;
                            resubmitClaim.RecordId = Guid.NewGuid();
                            resubmitClaim.ServiceDate = serviceRecord.ServiceDate;
                            resubmitClaim.UnitCode = unitRecord.UnitCode;
                            resubmitClaim.UnitNumber = unitRecord.UnitNumber;
                            resubmitClaim.UnitPremiumCode = unitRecord.UnitPremiumCode;
                            resubmitClaim.UserId = serviceRecord.UserId;

                            _repository.Insert(resubmitClaim);
                        }

                        _repository.Save();

                        var nextNumberModel = _repository.GetNextClaimNumber(_userId);
                        _repository.ResetSubmittedServiceRecord(_userId, serviceRecordId, nextNumberModel.RollOverNumber, nextNumberModel.NextClaimNumber);
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                }
            }

            return success;
        }

        [HttpPost]
        public bool BatchIgnore(string ids)
        {
            var success = false;

            var tempIds = ids.Split(',').Where(x => !string.IsNullOrEmpty(x)).Distinct();

            var serviceRecordIds = new List<Guid>();

            foreach (var id in tempIds)
            {
                Guid tempId;
                Guid.TryParse(id, out tempId);

                if (tempId != Guid.Empty)
                {
                    serviceRecordIds.Add(tempId);
                }
            }

            if (serviceRecordIds.Any())
            {
                try
                {
                    _repository.UpdateServiceRecordsWithClaimToIgnore(_userId, serviceRecordIds);

                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                }
            }

            return success;
        }


        #endregion

        #region JSON Service - Code List Services

        public JsonResult GetFeeCode()
        {
            try
            {
                return Json(StaticCodeList.MyFeeCodeList.Concat(StaticCodeList.MyWCBFeeCodeList).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value).ToList(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<KeyValuePair<string, StaticCodeList.FeeCodeModel>>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnitCodeFee(string prefix, int claimType)
        {
            try
            {
                prefix = prefix.ToUpper();

                IEnumerable<KeyValuePair<string, StaticCodeList.FeeCodeModel>> combinedList;

                if (claimType == 0) //MSB
                {
                    combinedList = StaticCodeList.MyFeeCodeList;
                }
                else
                {
                    combinedList = StaticCodeList.MyFeeCodeList.Concat(StaticCodeList.MyWCBFeeCodeList);
                }

                var result = combinedList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.Description.ToUpper().Contains(prefix))
                            .Select(x => new SimpleFeeModel()
                            {
                                key = x.Key,
                                label = x.Key.Contains(" - WCB") ? x.Key : x.Key + " - " + x.Value.Description,
                                requiredUnitTime = x.Value.RequiredStartTime == "Y",
                                requiredReferDoc = x.Value.RequiredReferringDoc == "Y",
                                value = x.Value.FeeAmount
                            })
                            .OrderBy(x => x.orderKey)
                            .Take(30)
                            .ToList();
                
                return Json(result, JsonRequestBehavior.AllowGet);                
            }
            catch
            {                
            }

            return Json(Enumerable.Empty<SimpleFeeModel>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnitCodeList(string unitcodelist)
        {
            try
            {
                var codesNeedToFetch = unitcodelist.Trim(',').Split(',').Distinct().Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
                var result = new List<SimpleFeeModel>();

                foreach (var unitcode in codesNeedToFetch)
                {
                    StaticCodeList.FeeCodeModel feeModel = null;
                    if (StaticCodeList.MyFeeCodeList.ContainsKey(unitcode))
                    {
                        feeModel = StaticCodeList.MyFeeCodeList[unitcode];

                        result.Add(new SimpleFeeModel()
                        {
                            key = unitcode,
                            label = unitcode + " - " + feeModel.Description,
                            requiredUnitTime = feeModel.RequiredStartTime == "Y",
                            requiredReferDoc = feeModel.RequiredReferringDoc == "Y",
                            value = feeModel.FeeAmount
                        });
                    }

                    var wcbFeeCode = unitcode + " - WCB";
                    if (StaticCodeList.MyWCBFeeCodeList.ContainsKey(wcbFeeCode))
                    {
                        feeModel = StaticCodeList.MyWCBFeeCodeList[wcbFeeCode];

                        result.Add(new SimpleFeeModel()
                        {
                            key = wcbFeeCode,
                            label = wcbFeeCode,
                            requiredUnitTime = false,
                            requiredReferDoc = false,
                            value = feeModel.FeeAmount
                        });
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<SimpleFeeModel>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSpecialFeeCode()
        {
            try
            {
                return Json(StaticCodeList.MySpecialCodeList.ToList(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<KeyValuePair<string, StaticCodeList.FeeCodeModel>>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeList(string prefix)
        {
            var result = StaticCodeList.MyICDList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeDescription(string diagcode)
        {
            var message = string.Empty;
            if (StaticCodeList.MyICDList.ContainsKey(diagcode))
            {
                message = StaticCodeList.MyICDList[diagcode];
            }

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeDescriptionList(string diagcodelist)
        {
            var codesNeedToFetch = diagcodelist.TrimEnd(',').Split(',').Distinct().Select(x => x.ToUpper());
            var result = new List<ICD>();

            foreach (var diagcode in codesNeedToFetch)
            {
                var message = string.Empty;
                if (StaticCodeList.MyICDList.ContainsKey(diagcode))
                {
                    result.Add(new ICD(diagcode, StaticCodeList.MyICDList[diagcode]));
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRefDocCodeList(string prefix)
        {
            var result = StaticCodeList.MyRefDocList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRefDocNameList(string prefix)
        {
            var result = StaticCodeList.MyRefDocList.Where(x => x.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private string GetRefDocName(string code)
        {
            var result = string.Empty;
            if (StaticCodeList.MyRefDocList.ContainsKey(code))
            {
                result = StaticCodeList.MyRefDocList[code];

                var endIndex = result.LastIndexOf(" - ");
                result = result.Substring(0, endIndex);
            }

            return result;
        }

        public JsonResult GetPatientList(string prefix)
        {
            var result = _repository.GetPatientList(_userId, prefix.ToLower());
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPatientListUsingHSN(string prefix)
        {
            var result = _repository.GetPatientListUsingHSN(_userId, prefix.ToLower());
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetExplainCode(string explainCode)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(explainCode))
            {
                explainCode = explainCode.ToUpper();
                if (StaticCodeList.MyExplainCodeList.ContainsKey(explainCode))
                {
                    result = StaticCodeList.MyExplainCodeList[explainCode];
                }
            }

            return Content(result);
        }

        #endregion

        #region JSON Service - Convert Submitted to New

        public bool ResetSubmitted(string id)
        {
            var success = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    var nextNumberModel = _repository.GetNextClaimNumber(_userId);
                    _repository.ResetSubmittedServiceRecord(serviceRecordId, nextNumberModel.RollOverNumber, nextNumberModel.NextClaimNumber);
                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        #endregion

        #region JSON Service - MSB and WCB Submit
       
        [HttpPost]
        public JsonResult SubmitWCB()
        {
            var submitResponse = new SubmitResponse();
            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var userProfile = _repository.GetUserProfile(_userId);

            if (userProfile != null && !string.IsNullOrEmpty(userProfile.PhoneNumber) && userProfile.PhoneNumber.Trim().Length > 0)
            {
                var myWCBRecords = _repository.GetUnsubmittedWCBServiceRecords(_userId).OrderBy(x => x.ClaimNumber);
                var myUnitRecords = _repository.GetUnitRecords(myWCBRecords.Select(x => x.ServiceRecordId));

                if (myWCBRecords.Count() > 0)
                {
                    var userName = ConfigHelper.GetInterfaxUserName();
                    var password = ConfigHelper.GetInterfaxPassword();
                    var faxNumber = ConfigHelper.GetWCBFaxNumber();
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(faxNumber))
                    {
                        var faxIssueCounter = 0;
                        var changedServiceIds = new List<Guid>();

                        foreach (var record in myWCBRecords)
                        {
                            try
                            {
                                var doctorName = string.Empty;
                                if (!string.IsNullOrEmpty(record.ReferringDoctorNumber))
                                {
                                    doctorName = GetRefDocName(record.ReferringDoctorNumber);
                                }
                                
                                var wcbUnitRecords = myUnitRecords.Where(x => x.ServiceRecordId == record.ServiceRecordId).ToList();

                                var creator = new WCBPdfCreator(Server.MapPath("App_Data/WCB_Billing_Form.pdf"));
                                var transactionId = creator.SendPDF(userProfile, record, wcbUnitRecords, doctorName, userName, password, faxNumber);

                                if (transactionId > 0)
                                {
                                    changedServiceIds.Add(record.ServiceRecordId);

                                    var faxDeliver = new FaxDeliver();
                                    faxDeliver.UserId = _userId;
                                    faxDeliver.ServiceRecordId = record.ServiceRecordId;
                                    faxDeliver.TransactionId = transactionId;
                                    faxDeliver.Status = (int)DeliverStatus.PENDING;

                                    _repository.InsertFax(faxDeliver);
                                }
                                else
                                {
                                    faxIssueCounter++;
                                    submitResponse.WCBMessage += string.Format("Problem sending Claim #: {0} due to our Fax Provider issues<br>", record.ClaimNumber);
                                }
                            }
                            catch (Exception ex)
                            {
                                faxIssueCounter++;
                                submitResponse.WCBMessage += string.Format("Problem sending Claim #: {0} due to {1}<br>", record.ClaimNumber, ex.Message);
                            }
                        }

                        if (faxIssueCounter == 0)
                        {
                            submitResponse.WCBSubmitted = true;
                            submitResponse.WCBMessage = "Successful";
                        }
                        else
                        {
                            submitResponse.WCBMessage = string.Format("There are {0} / {1} number of claims cannot send to our fax provider, and they are:<br>{2}",
                                                            faxIssueCounter, myWCBRecords.Count(), submitResponse.WCBMessage);
                        }

                        if (changedServiceIds.Count() > 0)
                        {
                            submitResponse.WCBSubmittedIds = string.Join(",", changedServiceIds);

                            try
                            {
                                var myClaimsIn = new ClaimsIn();
                                myClaimsIn.UserId = _userId;
                                myClaimsIn.RecordIndex = _repository.GetNextRecordIndex(_userId, timeZoneOffset);
                                myClaimsIn.ClaimAmount = myWCBRecords.Sum(x => x.ClaimAmount);
                                myClaimsIn.DownloadDate = DateTime.UtcNow;
                                myClaimsIn.FileSubmittedStatus = "ACCEPTED";
                                myClaimsIn.SubmittedFileName = "WCBSubmission.txt";

                                _repository.Insert(myClaimsIn);
                                _repository.Save();
                                _repository.UpdateServiceRecordsWithClaimInId(myClaimsIn.ClaimsInId, _userId, changedServiceIds);
                            }
                            catch (Exception ex)
                            {
                                submitResponse.WCBMessage = string.Format("Database issue occurred - {0}, please contact support", ex.Message);
                            }
                        }
                    }
                    else
                    {
                        submitResponse.WCBMessage = "System is not configure to submit fax, please contact support";
                    }
                }
                else
                {
                    submitResponse.WCBMessage = "There is no claim need to submit to WCB";
                }
            }
            else
            {
                submitResponse.WCBMessage = "Your user profile is not filled or Phone # is empty, please fill it up before submitting";
            }

            return this.Json(submitResponse);            
        }

        [HttpPost]
        public JsonResult SubmitMSB()
        {
            var submitResponse = new SubmitResponse();            
            var userProfile = _repository.GetUserProfile(_userId);
            var myReportContent = string.Empty;
            if (userProfile != null)            
            {
                if (!string.IsNullOrEmpty(userProfile.GroupNumber) && !string.IsNullOrEmpty(userProfile.GroupUserKey))
                {
                    var myMSBRecords = _repository.GetUnsubmittedMSBServiceRecords(_userId).OrderBy(x => x.ClaimNumber);
                    var myUnitRecords = _repository.GetUnitRecords(myMSBRecords.Select(x => x.ServiceRecordId));

                    if (myMSBRecords.Count() > 0)
                    {
                        var changedServiceIds = new List<Guid>();

                        try
                        {
                            myReportContent = new ClaimsInCreator(userProfile, myMSBRecords, myUnitRecords, StaticCodeList.MyCareCodeList).GetClaimsIn();
                        }
                        catch (ArgumentException ex)
                        {
                            var serviceRecordId = new Guid(ex.Message);
                            var claimNumber = myMSBRecords.FirstOrDefault(x => x.ServiceRecordId == serviceRecordId).ClaimNumber;
                            submitResponse.MSBMessage = "Some of the unit records in Claim #" + claimNumber + " contain invalid Diagnostic Code, please fix it and submit again";
                        }

                        if (!string.IsNullOrEmpty(myReportContent))
                        {
                            #region Submitted Code

                            try
                            {
                                var fileName = userProfile.GroupNumber + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                                var claimSubmissionStatus = "ACCEPTED";

                                var apiService = new ClaimService(_msbServiceConfig);

                                //var checkDailyReturnResult = apiService.FakeGetDailyReturnFileList();
                                var checkDailyReturnResult = apiService.GetDailyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                                if (checkDailyReturnResult.IsSuccess)
                                {
                                    #region Upload ClaimsIn Content

                                    //var submitResult = apiService.FakeUploadFile();
                                    var submitResult = apiService.UploadFile(userProfile.GroupUserKey, myReportContent, userProfile.GroupNumber, fileName);
                                    if (submitResult.IsSuccess)
                                    {
                                        changedServiceIds.AddRange(myMSBRecords.Select(x => x.ServiceRecordId));

                                        submitResponse.MSBIsPDFContent = true;
                                        submitResponse.MSBSubmitted = true;
                                        submitResponse.MSBValidationReportPDFFileName = submitResult.FileName;
                                        submitResponse.MSBMessage = submitResult.ISCContent;
                                    }
                                    else
                                    {
                                        if (submitResult.ErrorType == ErrorType.MSB_SERVER_ERROR)
                                        {
                                            changedServiceIds.AddRange(myMSBRecords.Select(x => x.ServiceRecordId));

                                            claimSubmissionStatus = "PENDING";
                                            submitResponse.MSBServerError = true;
                                            submitResult.ISCContent = null;
                                            submitResponse.MSBMessage = "MSB is having an internal server issue, your claims are moved to Submitted page for now but in On Hold status. We will periodically check MSB and convert them to Accepted status.";
                                        }
                                        else if (submitResult.ErrorType == ErrorType.DUPLICATE_FILENAME)
                                        {
                                            changedServiceIds.AddRange(myMSBRecords.Select(x => x.ServiceRecordId));

                                            submitResponse.MSBIsPDFContent = false;
                                            submitResponse.MSBSubmitted = true;
                                            submitResponse.MSBValidationReportPDFFileName = submitResult.FileName;
                                            submitResponse.MSBMessage = "MSB detected the submitted file: " + submitResult.FileName + " was submitted before. Your submission was accepted by MSB, but we are not able to get PDF Validation Summary to show.";
                                            submitResult.ISCContent = null;
                                        }
                                        else if (submitResult.ErrorType == ErrorType.SERVER_ERROR)
                                        {
                                            submitResponse.MSBMessage = "We are having an internal server issue, please try again in 30 minutes.";
                                        }
                                        else if (submitResult.ErrorType == ErrorType.EMPTY_CONTENT)
                                        {
                                            submitResponse.MSBMessage = "There is no claims to be submitted to MSB.";
                                        }
                                        else if (submitResult.ErrorType == ErrorType.VALIDATION_FAILED)
                                        {
                                            submitResponse.MSBIsPDFContent = true;
                                            submitResponse.MSBRejected = true;
                                            submitResponse.MSBMessage = submitResult.ISCContent;
                                            submitResponse.MSBValidationReportPDFFileName = submitResult.FileName;
                                        }
                                        else if (submitResult.ErrorType == ErrorType.UNAVAILABLE)
                                        {
                                            submitResponse.MSBMessage = "MSB is not accepting connection at this moment, please try again in 30 minutes.";
                                        }
                                        else
                                        {
                                            submitResponse.MSBMessage = submitResult.ISCContent;
                                        }
                                    }

                                    #endregion

                                    #region Mark the Service Records As Submitted

                                    if (changedServiceIds.Count() > 0)
                                    {
                                        try
                                        {
                                            var myClaimsIn = new ClaimsIn();
                                            myClaimsIn.UserId = _userId;
                                            myClaimsIn.RecordIndex = _repository.GetNextRecordIndex(_userId, _timeZoneOffset);
                                            myClaimsIn.ClaimAmount = myMSBRecords.Sum(x => x.ClaimAmount);
                                            myClaimsIn.DownloadDate = DateTime.UtcNow;
                                            myClaimsIn.Content = myReportContent;
                                            myClaimsIn.ValidationContent = submitResult.ISCContent;
                                            myClaimsIn.SubmittedFileName = fileName;
                                            myClaimsIn.FileSubmittedStatus = claimSubmissionStatus;

                                            _repository.Insert(myClaimsIn);
                                            _repository.Save();
                                            _repository.UpdateServiceRecordsWithClaimInId(myClaimsIn.ClaimsInId, _userId, changedServiceIds);
                                        }
                                        catch (Exception ex)
                                        {
                                            submitResponse.MSBMessage = submitResponse.WCBMessage = string.Format("Database issue occurred, please try again in 30 minutes. (Error: {0})", ex.Message);
                                        }
                                    }

                                    #endregion
                                }
                                else
                                {
                                    submitResponse.MSBMessage = string.Format("There is an issue connecting to the MSB, please make sure your Group Number and Group User Key are valid or try again in 30 minutes. (Error: {0})", checkDailyReturnResult.ErrorMessage);
                                }
                            }
                            catch (Exception ex)
                            {
                                submitResponse.MSBMessage = string.Format("Problem creating MSB submission records, please try again in 30 mins. (Error: {0}).", ex.Message);
                            }

                            #endregion
                        }
                    }
                    else
                    {
                        submitResponse.MSBMessage = "There is no claims to be submitted.";
                    }
                }
                else
                {
                    submitResponse.MSBMessage = "Group Number and Group User Key must be populated before submitting.";
                }
            }
            else
            {
                submitResponse.MSBMessage = "Your user profile is not filled, please fill it up before submitting.";
            }

            return this.Json(submitResponse);
        }

        private void SendMSBServerError(string message)
        {
            var mailSender = new MailSender();
            var sendMessage = mailSender.SendEmail(ConfigHelper.GetSupportEmail(), "Error on submitting claims", message);
        }

        #endregion

        #region JSON Service - WCB Resubmit

        public JsonResult ResubmitWCB()
        {
            var submitResponse = new SubmitResponse();
            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var userProfile = _repository.GetUserProfile(_userId);

            if (userProfile != null && !string.IsNullOrEmpty(userProfile.PhoneNumber) && userProfile.PhoneNumber.Trim().Length > 0)
            {
                var faxDelivers = _repository.WCBClaimsNeedToReSubmit(_userId, _timeZoneOffset);
                var serviceRecordIds = faxDelivers.Select(x => x.ServiceRecordId).Distinct();
                var myWCBRecords = _repository.GetWCBServiceRecords(serviceRecordIds);
                var myUnitRecords = _repository.GetUnitRecords(serviceRecordIds);

                #region Submit WCB Claims
               
                if (myWCBRecords.Count() > 0)
                {
                    var userName = ConfigHelper.GetInterfaxUserName();
                    var password = ConfigHelper.GetInterfaxPassword();
                    var faxNumber = ConfigHelper.GetWCBFaxNumber();

                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(faxNumber))
                    {
                        var faxIssueCounter = 0;
                        var successfulFaxes = new List<SuccessFax>();
                        var needToSave = false;

                        foreach (var record in myWCBRecords)
                        {
                            try
                            {
                                var recordFaxDelivers = faxDelivers.Where(x => x.ServiceRecordId == record.ServiceRecordId).ToList();
                                var creator = new WCBPdfCreator(Server.MapPath("App_Data/WCB_Billing_Form.pdf"));
                                if (!creator.IsFaxSuccessful(recordFaxDelivers.Select(x => x.TransactionId).ToList(), userName, password))
                                {
                                    var doctorName = string.Empty;
                                    if (!string.IsNullOrEmpty(record.ReferringDoctorNumber))
                                    {
                                        doctorName = GetRefDocName(record.ReferringDoctorNumber);
                                    }

                                    var wcbUnitRecords = myUnitRecords.Where(x => x.ServiceRecordId == record.ServiceRecordId).ToList();
                                    
                                    var transactionId = creator.SendPDF(userProfile, record, wcbUnitRecords, doctorName, userName, password, faxNumber);

                                    if (transactionId > 0)
                                    {
                                        needToSave = true;

                                        var faxDeliver = new FaxDeliver();
                                        faxDeliver.UserId = _userId;
                                        faxDeliver.ServiceRecordId = record.ServiceRecordId;
                                        faxDeliver.TransactionId = transactionId;
                                        faxDeliver.Status = (int)DeliverStatus.PENDING;
                                        
                                        _repository.InsertFax(faxDeliver);

                                        foreach (var deliver in recordFaxDelivers)
                                        {                                            
                                            _repository.DeleteDeliver(deliver);
                                        }
                                    }
                                    else
                                    {
                                        faxIssueCounter++;
                                    }
                                }
                                else
                                {
                                    needToSave = true;
                                    
                                    ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                                    myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                                    myClaimsInReturn.ReturnFooter = string.Empty;
                                    myClaimsInReturn.TotalApproved = Convert.ToDouble(record.ClaimAmount);
                                    myClaimsInReturn.TotalSubmitted = Convert.ToDouble(record.ClaimAmount);
                                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                                    myClaimsInReturn.UserId = record.UserId;
                                    myClaimsInReturn.ReturnFileType = (int)ReturnFileType.WCB;
                                    _repository.InsertClaimInReturn(myClaimsInReturn);

                                    PaidClaim myPaidClaim = new PaidClaim();
                                    myPaidClaim.PaidClaimId = Guid.NewGuid();
                                    myPaidClaim.CreatedDate = DateTime.UtcNow;
                                    myPaidClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;
                                    _repository.InsertPaidClaim(myPaidClaim);
                                    
                                    var faxDeliver = new FaxDeliver();
                                    faxDeliver.UserId = _userId;
                                    faxDeliver.ServiceRecordId = record.ServiceRecordId;
                                    faxDeliver.TransactionId = recordFaxDelivers.FirstOrDefault().TransactionId;
                                    faxDeliver.Status = (int)DeliverStatus.SUCCESS;

                                    _repository.InsertFax(faxDeliver);

                                    foreach (var deliver in recordFaxDelivers)
                                    {
                                        _repository.DeleteDeliver(deliver);
                                    }

                                    successfulFaxes.Add(new SuccessFax() 
                                    {
                                        ServiceRecordId = record.ServiceRecordId,
                                        PaidClaimId = myPaidClaim.PaidClaimId,
                                        PaidAmount = record.ClaimAmount
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                faxIssueCounter++;
                                submitResponse.WCBMessage = ex.Message;
                            }
                        }

                        if (faxIssueCounter == 0)
                        {
                            submitResponse.WCBSubmitted = true;
                            submitResponse.WCBMessage = "WCB claims had been re-submitted to our Fax Provider queue and will fax to WCB shortly.";
                        }
                        else
                        {
                            submitResponse.WCBMessage = string.Format("There are {0} / {1} number of claims cannot send to our fax provider.", faxIssueCounter, myWCBRecords.Count());
                        }                        

                        if (needToSave)
                        {
                            _repository.Save();

                            if (successfulFaxes.Count() > 0)
                            {
                                foreach (var success in successfulFaxes)
                                {
                                    _repository.ConvertWCBToPaid(success.ServiceRecordId, success.PaidClaimId, success.PaidAmount);
                                }
                            }
                        }
                    }
                    else
                    {
                        submitResponse.WCBMessage = "System is not configure to submit fax, please contact support";
                    }                    
                }
                else
                {
                    submitResponse.WCBMessage = "There is no claim need to re-submit to WCB";
                }

                #endregion                
            }
            else
            {
                submitResponse.WCBMessage = "Your user profile is not filled or Phone # is empty, please fill it up before submitting";
            }

            return this.Json(submitResponse);
        }

        #endregion

        #region JSON Service - Get Fax Receipt

        public JsonResult GetFaxReceipt(string id)
        {
            var response = new FaxReceiptResponse();
            response.IsSuccess = false;
            var serviceRecordId = Guid.Empty;

            Guid.TryParse(id, out serviceRecordId);
            if (serviceRecordId != Guid.Empty && _repository.IsServiceRecordBelongToUser(_userId, serviceRecordId))
            {
                try
                {
                    var faxDelivery = _repository.GetFaxDeliver(serviceRecordId);
                    if (faxDelivery != null)
                    {
                        try
                        {
                            var userName = ConfigHelper.GetInterfaxUserName();
                            var password = ConfigHelper.GetInterfaxPassword();

                            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                            var client = new InterfaxService.InterFaxSoapClient("InterFaxSoap12");
                            var totalCount = 0;
                            var listSize = 0;
                            var resultCode = 0;
                            var tranId = Convert.ToInt32(faxDelivery.TransactionId);
                            var items = client.FaxStatus(userName, password, (tranId + 1), 1, ref totalCount, ref listSize, ref resultCode);

                            var faxRecord = items.FirstOrDefault(x => x.TransactionID == tranId);
                            if (faxRecord != null)
                            {
                                response.IsSuccess = true;
                                response.FaxStatus = faxRecord.Status == 0 ? "Success" : "Failed";
                                response.CompletionTime = faxRecord.CompletionTime.AddHours(_timeZoneOffset).ToLongDateString() + " " + faxRecord.CompletionTime.AddHours(_timeZoneOffset).ToLongTimeString();
                                response.SubmissionTime = faxRecord.SubmitTime.AddHours(_timeZoneOffset).ToLongDateString() + " " + faxRecord.SubmitTime.AddHours(_timeZoneOffset).ToLongTimeString();
                                response.PageSent = faxRecord.PagesSent.ToString();                                
                            }                            
                        }
                        catch
                        {
                        }
                    }                    
                    
                }
                catch (Exception)
                {                    
                }
            }

            return this.Json(response);
        }

        #endregion
    }

    public class SuccessFax
    {
        public Guid ServiceRecordId { get; set; }

        public Guid PaidClaimId { get; set; }

        public double PaidAmount { get; set; }
    }

    public class FaxReceiptDetail
    {

    }
}