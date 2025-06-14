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
    
    public partial class ClaimsSearchView
    {
        public System.Guid UserId { get; set; }
        public string UserName { get; set; }
        public System.Guid ServiceRecordId { get; set; }
        public int ClaimStatus { get; set; }
        public Nullable<System.DateTime> SubmissionDate { get; set; }
        public int ClaimNumber { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientLastName { get; set; }
        public string HospitalNumber { get; set; }
        public System.DateTime ServiceDate { get; set; }
        public string CPSClaimNumber { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public System.Guid UnitRecordId { get; set; }
        public string RunCode { get; set; }
        public string DiagCode { get; set; }
        public string UnitCode { get; set; }
        public int UnitNumber { get; set; }
        public string UnitPremiumCode { get; set; }
        public string ExplainCode { get; set; }
        public string ExplainCode2 { get; set; }
        public string ExplainCode3 { get; set; }
        public Nullable<double> ClaimAmount { get; set; }
        public double PaidAmount { get; set; }
        public Nullable<double> VarianceAmount { get; set; }
        public string ClaimStatusString { get; set; }
        public string DoctorName { get; set; }
        public Nullable<long> SubmittedRecordIndex { get; set; }
    }
}
