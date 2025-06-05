using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MBS.DomainModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MBS.Web.Portal.Models
{
    public class ClaimsPDFReportViewModel
    {
        public string DoctorNumber { get; set; }

        public string ReportType { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
        
        public int NumberOfClaims { get; set; }

        public double TotalPaidAmount { get; set; }

        public int WCBNumberOfClaims { get; set; }

        public double WCBTotalPaidAmount { get; set; }

        public IList<SimpleServiceRecord> RecordList { get; set; }

        public IEnumerable<UnitCodeUsed> UnitRecordUsedList { get; set; }

        public int TimeZoneOffset { get; set; }
    }
    
    public class UnitCodeUsed
    {
        public string UnitCode { get; set;  }

        public int TotalUnitNumber { get; set; }
    }

    public class SimpleServiceRecord
    {
        public int ClaimNumber { get; set; }

        public string FirstName { get; set;  }

        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }
        
        public DateTime ServiceDate { get; set; }

        public string SubmissionDate { get; set; }

        public string UnitCode { get; set; }

        public int UnitNumber { get; set; }

        public double PaidAmount { get; set; }

        public string WCBStatus { get; set; }
    }
}