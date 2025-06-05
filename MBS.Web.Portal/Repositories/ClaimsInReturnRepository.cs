using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using MBS.DomainModel;
using MBS.Web.Portal.Models;

namespace MBS.Web.Portal.Repositories
{
    public class ClaimsInReturnRepository : IClaimsInReturnRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public ClaimsInReturnRepository()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
        }

        public ClaimsInReturn Find(Guid id)
        {
			return context.ClaimsInReturn.Find(id);
        }
        
        public IEnumerable<SimpleClaimReturn> GetClaimsInReturnWithContentByUser(Guid userId)
        {
            return context.Database.SqlQuery<SimpleClaimReturn>(
                "SELECT ClaimsInReturnId, TotalPaidAmount, UploadDate, ReturnFileName, ReturnFileType, RunCode " +
                "FROM ClaimsInReturn WHERE UserId = '" + userId + "' AND Content IS NOT NULL AND Content != '' " + 
                "ORDER BY UploadDate DESC").ToList();
        }       		

        public bool IsAccessValidClaimsInReturn(Guid userId, Guid returnId)
        {
            return context.ClaimsInReturn.FirstOrDefault(x => x.UserId == userId && x.ClaimsInReturnId == returnId) != null;
        }

        public IEnumerable<ClaimsReturnPaymentSummary> GetPaymentSummary(Guid claimsInReturnId)
        {
            return context.ClaimsReturnPaymentSummary.Where(x => x.ClaimsInReturnId == claimsInReturnId).OrderBy(x => x.LineNumber).ToList();
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public interface IClaimsInReturnRepository : IDisposable
    {
		ClaimsInReturn Find(Guid id);

        IEnumerable<SimpleClaimReturn> GetClaimsInReturnWithContentByUser(Guid userId);     
				
        bool IsAccessValidClaimsInReturn(Guid userId, Guid returnId);

        IEnumerable<ClaimsReturnPaymentSummary> GetPaymentSummary(Guid claimsInReturnId);
    }
}