using System.Collections.Generic;

namespace MBS.Web.Portal.Models
{
    public enum RecordType
    {
        NORMAL,
        COMMENT,
        RECIPROCAL
    }

    public class ClaimsInReportViewModel
    {
        public HeaderModel Header { get; set; }

        public FooterModel Footer { get; set; }

        public List<RecordItem> RecordList { get; set; }
    }

    public class HeaderModel
    {
        public string Name { get; set; }

        public string DocNumber { get; set; }

        public string ClinicNumber { get; set; }
        
        public string Mode { get; set; }
    }

    public class FooterModel
    {        
        public int NumberOfServiceRecords { get; set; }

        public int NumberOfCommentRecords { get; set; }

        public int TotalRecords { get; set; }

        public string TotalAmount { get; set; }
    }

    public class RecordItem
    {
        public RecordType Type { get; set; }

        public string HospitalNumber { get; set; }

        public string Sex { get; set; }

        public string BirthDate { get; set; }

        public string PatientName { get; set; }

        public string ClaimNumber { get; set; }

        public string SeqNumber { get; set; }

        public string Diag { get; set; }

        public string ServiceStartDate { get; set; }

        public string DischargeDate { get; set; }

        public string ServiceEndDate { get; set; }

        public string UnitNumber { get; set; }

        public string FeeCode { get; set; }

        public string Amount { get; set; }

        public string ReferringDoctor { get; set; }

        public string Location { get; set; }
    }
}