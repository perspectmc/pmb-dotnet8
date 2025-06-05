using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using MBS.DomainModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MBS.Web.Portal.Constants;
using System.Data.SqlClient;
using MBS.Web.Portal.Models;

namespace MBS.Web.Portal.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public FaxDeliver GetFaxDelivery(long transactionId)
        {
            return context.FaxDeliver.FirstOrDefault(x => x.TransactionId == transactionId);
        }

        public void SetRelatedFaxDeliver(Guid serviceRecordId, DeliverStatus status)
        {
            var query = string.Format("UPDATE FaxDeliver Set Status = '{0}' WHERE ServiceRecordId = '{1}'", (int)status, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);
        }

        public ServiceRecord GetServiceRecord(Guid serviceRecordId)
        {
            return context.ServiceRecord.FirstOrDefault(x => x.ServiceRecordId == serviceRecordId);
        }

        public void InsertClaimInReturn(ClaimsInReturn claimsInReturn)
        {
            context.ClaimsInReturn.Add(claimsInReturn);
        }

        public void InsertPaidClaim(PaidClaim paidClaim)
        {
            context.PaidClaim.Add(paidClaim);
        }

        public void ConvertWCBToPaid(Guid serviceRecordId, Guid paidClaimId, double paidAmount)
        {
            var query = string.Format("UPDATE ServiceRecord Set PaidClaimId = '{0}', PaidAmount = '{1}', WCBFaxStatus = 'Received by WCB' WHERE ServiceRecordId = '{2}'", 
                            paidClaimId, paidAmount, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);
        }

        public void Save()
        {
            context.SaveChanges();
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public interface IFeedbackRepository : IDisposable
    {
        FaxDeliver GetFaxDelivery(long transactionId);

        ServiceRecord GetServiceRecord(Guid serviceRecordId);

        void SetRelatedFaxDeliver(Guid serviceRecordId, DeliverStatus status);

        void InsertClaimInReturn(ClaimsInReturn claimsInReturn);

        void InsertPaidClaim(PaidClaim paidClaim);

        void ConvertWCBToPaid(Guid serviceRecordId, Guid paidClaimId, double paidAmount);

        void Save();
    }
}