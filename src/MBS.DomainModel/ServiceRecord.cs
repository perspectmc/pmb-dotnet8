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
    
    public partial class ServiceRecord
    {
        public ServiceRecord()
        {
            this.UnitRecord = new HashSet<UnitRecord>();
        }
    
        public System.Guid ServiceRecordId { get; set; }
        public Nullable<System.Guid> ClaimsInId { get; set; }
        public int ClaimNumber { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientLastName { get; set; }
        public System.DateTime DateOfBirth { get; set; }
        public string Sex { get; set; }
        public string Province { get; set; }
        public string HospitalNumber { get; set; }
        public string ReferringDoctorNumber { get; set; }
        public System.DateTime ServiceDate { get; set; }
        public Nullable<System.TimeSpan> ServiceStartTime { get; set; }
        public Nullable<System.TimeSpan> ServiceEndTime { get; set; }
        public Nullable<System.DateTime> LastModifiedDate { get; set; }
        public string Comment { get; set; }
        public Nullable<System.Guid> PaidClaimId { get; set; }
        public Nullable<System.Guid> RejectedClaimId { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public double ClaimAmount { get; set; }
        public System.Guid UserId { get; set; }
        public double PaidAmount { get; set; }
        public int ClaimType { get; set; }
        public string WCBFaxStatus { get; set; }
        public string MessageFromICS { get; set; }
        public Nullable<System.DateTime> PaymentApproveDate { get; set; }
        public Nullable<System.DateTime> DischargeDate { get; set; }
        public int RollOverNumber { get; set; }
        public string ServiceLocation { get; set; }
        public string FacilityNumber { get; set; }
        public string Notes { get; set; }
        public string CPSClaimNumber { get; set; }
        public Nullable<bool> IsAdjustment { get; set; }
        public double VarianceAmount { get; set; }
        public bool ClaimToIgnore { get; set; }
        public int ClaimStatus { get; set; }
    
        public virtual ClaimsIn ClaimsIn { get; set; }
        public virtual PaidClaim PaidClaim { get; set; }
        public virtual RejectedClaim RejectedClaim { get; set; }
        public virtual Users Users { get; set; }
        public virtual ICollection<UnitRecord> UnitRecord { get; set; }
    }
}
