using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using MBS.DomainModel;

namespace MBS.Web.Portal.Models
{
    public class ClaimsReportViewModel
    {
		[Required]
		[DisplayName("Service Start Date")]
		public string ServiceStartDate { get; set; }

		[Required]
		[DisplayName("Service End Date")]
        public string ServiceEndDate { get; set; }

        [Required]
        [DisplayName("Claim Type")]
		public int ReportType { get; set; }
		
        public SelectList ReportTypeList { get; set; }

		public bool IsInfoFilled { get; set; }

        public int TotalNumberOfRecords { get; set; }

        public double TotalPaidAmount { get; set; }
    }	
}