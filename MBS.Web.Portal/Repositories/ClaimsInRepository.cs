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

namespace MBS.Web.Portal.Repositories
{
    public class ClaimsInRepository : IClaimsInRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public ClaimsInRepository()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
        }
              
        public bool IsAccessValidClaimsIn(Guid userId, Guid claimsInId)
        {
            return (context.ClaimsIn.FirstOrDefault(x => x.ClaimsInId == claimsInId && x.UserId == userId) != null);
        }

        public ClaimsIn GetClaimsIn(Guid claimsInId)
        {
            return context.ClaimsIn.FirstOrDefault(x => x.ClaimsInId == claimsInId);
        }

        public IEnumerable<ClaimsIn> GetClaimsInWithContentByUser(Guid userId)
        {
            return context.ClaimsIn.Where(x => x.UserId == userId && x.Content != null && x.Content != "").ToList();
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public interface IClaimsInRepository : IDisposable
    {
        ClaimsIn GetClaimsIn(Guid claimsInId);

        IEnumerable<ClaimsIn> GetClaimsInWithContentByUser(Guid userId);

        bool IsAccessValidClaimsIn(Guid userId, Guid claimsInId);
    }
}