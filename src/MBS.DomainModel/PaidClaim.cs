//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MBS.DomainModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class PaidClaim
    {
        public PaidClaim()
        {
            this.ServiceRecord = new HashSet<ServiceRecord>();
        }
    
        public System.Guid PaidClaimId { get; set; }
        public System.Guid ClaimsInReturnId { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual ClaimsInReturn ClaimsInReturn { get; set; }
        public virtual ICollection<ServiceRecord> ServiceRecord { get; set; }
    }
}
