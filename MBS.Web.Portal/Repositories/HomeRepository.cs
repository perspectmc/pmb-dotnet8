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
    public class HomeRepository : IHomeRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public UserProfiles GetUserProfile(Guid userId)
        {
            return context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }

        public TotalItem GetUnSubmittedInfo(Guid userId, DateTime endPeriod)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord WHERE UserId = '{0}' AND ServiceDate >= '{1}' 
                                        AND ClaimsInId IS NULL AND PaidClaimId IS NULL AND RejectedClaimId IS NULL AND CPSClaimNumber IS NULL AND ClaimToIgnore = 0",
                                    userId.ToString(), endPeriod.ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetSubmittedInfo(Guid userId, DateTime endPeriod)
        {
            //WCB Claims
            //NEW       WCBFaxStatus = NULL
            //SUBMITTED WCBFaxStatus = NULL OR XXXX AND ClaimsInId IS NOT NULL
            //PAID      WCBFaxStatus = EMPTY AND ClaimsInId != NULL AND PaidClaimId IS NOT NULL

            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord 
                                        WHERE UserId = '{0}' AND ServiceDate >= '{1}' AND CPSClaimNumber IS NULL AND ClaimsInId IS NOT NULL AND RejectedClaimId IS NULL AND ClaimToIgnore = 0 
                                        AND ((ClaimType = 0 AND PaidClaimId IS NULL) OR (ClaimType = 1 AND (WCBFaxStatus IS NULL OR WCBFaxStatus != '')))",
                                    userId.ToString(), endPeriod.ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetPendingInfo(Guid userId, DateTime endPeriod)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord 
                                        WHERE UserId = '{0}' AND ServiceDate >= '{1}' AND CPSClaimNumber IS NOT NULL AND RejectedClaimId IS NULL AND ClaimToIgnore = 0 
                                        AND ClaimType = 0 AND PaidClaimId IS NULL",
                                    userId.ToString(), endPeriod.ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetPaidInfo(Guid userId, DateTime endPeriod)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(PaidAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord 
                                        WHERE UserId = '{0}' AND ServiceDate >= '{1}' AND ClaimToIgnore = 0 AND ((ClaimType = 0 AND PaidClaimId IS NOT NULL) OR (ClaimType = 1 AND WCBFaxStatus = ''))",
                                    userId.ToString(), endPeriod.ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetRejectedInfo(Guid userId, DateTime endPeriod)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord WHERE UserId = '{0}' AND ServiceDate >= '{1}' 
                                    AND RejectedClaimId IS NOT NULL AND ClaimToIgnore = 0",
                                    userId.ToString(), endPeriod.ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetExpiringInfo(Guid userId, DateTime currentDate)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord WHERE UserId = '{0}'  
                                    AND ClaimStatus != 5 AND ClaimStatus != 3 AND ServiceDate >= '{1}' AND ServiceDate <= '{2}' AND ClaimToIgnore = 0",
                                    userId.ToString(), currentDate.AddMonths(-6).ToString("yyyy-MM-dd"), currentDate.AddMonths(-4).ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public TotalItem GetClaimsLostInfo(Guid userId, DateTime currentDate)
        {
            var sql = string.Format(@"SELECT ISNULL(SUM(ClaimAmount), 0) As Amount, Count(ServiceRecordId) As NumberOfRecords FROM ServiceRecord WHERE UserId = '{0}'  
                                    AND ClaimStatus != 5 AND ClaimStatus != 3 AND ServiceDate < '{1}' AND ClaimToIgnore = 0",
                                    userId.ToString(), currentDate.AddMonths(-6).ToString("yyyy-MM-dd"));
            return GetSqlResult(sql);
        }

        public bool NeedToReSubmitFax(Guid userId)
        {
            var targetDate = DateTime.UtcNow.AddDays(-1);
            var maxFailedDate = DateTime.UtcNow.Date.AddDays(-60);

            var serviceRecordIds = context.FaxDeliver.Where(x => x.UserId == userId && ((x.Status == (int)DeliverStatus.FAIL && x.CreatedDate > maxFailedDate) || (x.Status == (int)DeliverStatus.PENDING && x.CreatedDate < targetDate))).Select(x => x.ServiceRecordId).Distinct().ToList();

            return context.ServiceRecord.Where(x => serviceRecordIds.Contains(x.ServiceRecordId) && x.ClaimType == (int)ClaimType.WCB).Any();
        }

        public IEnumerable<ClaimsIn> GetRejectedOrRecentlyAcceptedClaimsIn(Guid userId)
        {
            var acceptedDateRange = DateTime.UtcNow.AddDays(-2);
            var rejectedDateRange = DateTime.UtcNow.AddDays(-3);

            return context.ClaimsIn.Where(x => x.UserId == userId && 
                    ((x.FileSubmittedStatus == "REJECTED" && x.CreatedDate > rejectedDateRange) || 
                    (x.FileSubmittedStatus == "ACCEPTED" && x.DateChangeToAccepted > acceptedDateRange))).ToList();
        }

        private TotalItem GetSqlResult(string sqlQuery)
        {
            var result = new TotalItem();

            var sqlResult = context.Database.SqlQuery<TotalSqlResult>(sqlQuery);

            if (sqlResult != null && sqlResult.Count() > 0)
            {
                var total = sqlResult.FirstOrDefault();
                if (total != null)
                {
                    result.NumberOfRecords = total.NumberOfRecords;
                    if (total.Amount.HasValue)
                    {
                        result.Amount = total.Amount.Value;
                    }
                }
            }

            return result;
        }
        
        public IEnumerable<Users> GetUsers()
        {
            return context.Users.ToList();
        }
        
        public bool IsUserProfileExisted(Guid userId)
        {
            return context.UserProfiles.Any(x => x.UserId == userId);
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public interface IHomeRepository : IDisposable
    {
        bool NeedToReSubmitFax(Guid userId);

        bool IsUserProfileExisted(Guid userId);

        TotalItem GetUnSubmittedInfo(Guid userId, DateTime endPeriod);

        TotalItem GetSubmittedInfo(Guid userId, DateTime endPeriod);

        TotalItem GetPendingInfo(Guid userId, DateTime endPeriod);

        TotalItem GetPaidInfo(Guid userId, DateTime endPeriod);

        TotalItem GetRejectedInfo(Guid userId, DateTime endPeriod);

        TotalItem GetExpiringInfo (Guid userId, DateTime currentDate);

        TotalItem GetClaimsLostInfo(Guid userId, DateTime currentDate);

        UserProfiles GetUserProfile(Guid userId);

        IEnumerable<ClaimsIn> GetRejectedOrRecentlyAcceptedClaimsIn(Guid userId);
    }

    public class TotalSqlResult
    {
        public double? Amount { get; set; }

        public int NumberOfRecords { get; set; }
    }
}