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
using MBS.Web.Portal.Models;

namespace MBS.Web.Portal.Repositories
{
    public class ClaimsReportRepository : IClaimsReportRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public ClaimsReportRepository()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
        }

		public IEnumerable<ServiceRecord> GetFilterServiceRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate)
		{		
			var serviceRecordList = context.ServiceRecord.Where(x => x.ServiceDate >= startDate && x.ServiceDate <= endDate && x.UserId == userId && x.ClaimType == 0 && !x.ClaimToIgnore);

			if (reportType == ReportType.Paid)
			{
				serviceRecordList = serviceRecordList.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue);		
			}
			else if (reportType == ReportType.Rejected)
			{
				serviceRecordList = serviceRecordList.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue);
			}
            else
            {
                serviceRecordList = serviceRecordList.Where(x => x.PaidClaimId.HasValue || x.RejectedClaimId.HasValue);
            }

            return serviceRecordList.ToList();
		}

        public IEnumerable<ServiceRecord> GetFilterServiceRecordsWithUnitRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate)
        {
            var serviceRecordList = context.ServiceRecord.Include("UnitRecord").Include("ClaimsIn").Where(x => x.ServiceDate >= startDate && x.ServiceDate <= endDate && 
                                    x.UserId == userId && x.ClaimType == 0 && !x.ClaimToIgnore);

            if (reportType == ReportType.UnitRecordWithPaidClaim)
            {
                serviceRecordList = serviceRecordList.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue);
            }
            else if (reportType == ReportType.UnitRecordWithRejectedClaim)
            {
                serviceRecordList = serviceRecordList.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue);
            }
            else
            {
                serviceRecordList = serviceRecordList.Where(x => x.PaidClaimId.HasValue || x.RejectedClaimId.HasValue);
            }

            return serviceRecordList.ToList();
        }

        public TotalItem GetTotalItemWithServiceRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate)
        {
            var sqlQuery =
                "SELECT COUNT(A.ServiceRecordId) AS NumberOfRecords, ISNULL(SUM(A.Amount), 0) AS Amount FROM " +
                "(SELECT ServiceRecordId, CASE WHEN RejectedClaimId IS NOT NULL THEN ClaimAmount ELSE PaidAmount END AS Amount FROM ServiceRecord WHERE UserId = '" + userId + "' AND ClaimType = 0 " +
                "AND ServiceDate >= '" + startDate.ToString("yyyy-MM-dd") + "' AND ServiceDate <= '" + endDate.ToString("yyyy-MM-dd") + "' AND ClaimToIgnore = 0";

            if (reportType == ReportType.Paid)
            {
                sqlQuery += "AND PaidClaimId IS NOT NULL AND RejectedClaimId IS NULL";
            }
            else if (reportType == ReportType.Rejected)
            {
                sqlQuery += "AND PaidClaimId IS NULL AND RejectedClaimId IS NOT NULL";
            }
            else
            {
                sqlQuery += "AND (PaidClaimId IS NOT NULL OR RejectedClaimId IS NOT NULL)";
            }

            sqlQuery += ") AS A";

            var result = context.Database.SqlQuery<TotalItem>(sqlQuery).FirstOrDefault();

            if (result == null)
            {
                result = new TotalItem() { Amount = 0, NumberOfRecords = 0 };
            }

            return result;
        }

        public TotalItem GetTotalItemWithUnitRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate)
        {
            var sqlQuery =
                "SELECT Count(ur.UnitRecordId) AS NumberOfRecords, ISNULL(Sum(ur.PaidAmount), 0) AS Amount FROM UnitRecord ur LEFT JOIN ServiceRecord sr ON ur.ServiceRecordId = sr.ServiceRecordId " +
                "WHERE sr.UserId =  '" + userId + "' and sr.ServiceDate >= '" + startDate.ToString("yyyy-MM-dd") + "' and sr.ServiceDate <= '" + endDate.ToString("yyyy-MM-dd") + "' AND sr.ClaimType = 0 AND sr.ClaimToIgnore = 0";

            if (reportType == ReportType.UnitRecordWithPaidClaim)
            {
                sqlQuery += "AND sr.PaidClaimId IS NOT NULL AND sr.RejectedClaimId IS NULL";
            }
            else if (reportType == ReportType.UnitRecordWithRejectedClaim)
            {
                sqlQuery += "AND sr.PaidClaimId IS NULL AND sr.RejectedClaimId IS NOT NULL";
            }
            else
            {
                sqlQuery += "AND (sr.PaidClaimId IS NOT NULL OR sr.RejectedClaimId IS NOT NULL)";
            }

            var result = context.Database.SqlQuery<TotalItem>(sqlQuery).FirstOrDefault();

            if (result == null)
            {
                result = new TotalItem() { Amount = 0, NumberOfRecords = 0 };
            }

            return result;
        }
                
		public UserProfiles GetUserProfile(Guid userId)
		{
			return context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
		}

        public void Dispose()
        {
            context.Dispose();
        }

    }

    public interface IClaimsReportRepository : IDisposable
    {
		IEnumerable<ServiceRecord> GetFilterServiceRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate);

        IEnumerable<ServiceRecord> GetFilterServiceRecordsWithUnitRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate);

        TotalItem GetTotalItemWithServiceRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate);

        TotalItem GetTotalItemWithUnitRecords(Guid userId, ReportType reportType, DateTime startDate, DateTime endDate);

		UserProfiles GetUserProfile(Guid userId);
    }
}