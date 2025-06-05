using System;
using System.Web.Mvc;
using MBS.DomainModel;
using MBS.Common;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Services;

namespace MBS.Web.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    [AllowAnonymous]
    public class FeedbackController : Controller
    {
        private IFeedbackRepository _repository;

        // If you are using Dependency Injection, you can delete the following constructor
        public FeedbackController()
            : this(new FeedbackRepository())
        {
        }

        public FeedbackController(IFeedbackRepository repository)
        {
            _repository = repository;            
        }

        public ActionResult Interfax()
        {
            var response = GetResponse();

            if (response.TransactionId > 0 && response.Status != int.MinValue)
            {
                var faxDeliver = _repository.GetFaxDelivery(response.TransactionId);
                if (faxDeliver != null)
                {
                    var status = DeliverStatus.FAIL;
                    if (response.Status == 0)
                    {
                        status = DeliverStatus.SUCCESS;

                        CreateReturn(faxDeliver.ServiceRecordId);
                    }

                    _repository.SetRelatedFaxDeliver(faxDeliver.ServiceRecordId, status);
                }
            }

            return View();
        }

        private FeedBackResponse GetResponse()
        {
            var result = new FeedBackResponse();
            result.TransactionId = long.MinValue;
            result.Status = int.MinValue;

            if (!string.IsNullOrEmpty(Request["TransactionID"]))
            {
                try { result.TransactionId = long.Parse(Request["TransactionID"]); }
                catch (Exception) {}
            }

            if (!string.IsNullOrEmpty(Request["Status"]))
            {
                try { result.Status = int.Parse(Request["Status"]); }
                catch (Exception) { }
            }

            return result;
        }

        private void CreateReturn(Guid serviceRecordId)
        {
            var serviceRecord = _repository.GetServiceRecord(serviceRecordId);
            if (serviceRecord != null)
            {
                var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();

                ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                myClaimsInReturn.ReturnFooter = string.Empty;
                myClaimsInReturn.TotalApproved = Convert.ToDouble(serviceRecord.ClaimAmount);
                myClaimsInReturn.TotalSubmitted = Convert.ToDouble(serviceRecord.ClaimAmount);
                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.UserId = serviceRecord.UserId;
                myClaimsInReturn.ReturnFileType = (int)ReturnFileType.WCB;
                _repository.InsertClaimInReturn(myClaimsInReturn);

                PaidClaim myPaidClaim = new PaidClaim();
                myPaidClaim.PaidClaimId = Guid.NewGuid();
                myPaidClaim.CreatedDate = DateTime.UtcNow;
                myPaidClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;
                _repository.InsertPaidClaim(myPaidClaim);

                _repository.Save();

                _repository.ConvertWCBToPaid(serviceRecordId, myPaidClaim.PaidClaimId, serviceRecord.ClaimAmount);
            }            
        }
    }

    public class FeedBackResponse
    {
        public long TransactionId { get; set; }

        public int Status { get; set; }
    }
}
