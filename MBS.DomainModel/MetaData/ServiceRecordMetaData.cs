using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MBS.DomainModel
{
	[MetadataType(typeof(ServiceRecordMetaData))]
	public partial class ServiceRecord
    {
    }

	public class ServiceRecordMetaData
    {
		[Display(Name = "Claim Number")]
		public int ClaimNumber { get; set; }

        [Display(Name = "Claim Type")]
        public int ClaimType { get; set; }

		[Required]
		[Display(Name = "First Name")]
		public string PatientFirstName { get; set; }

		[Required]
		[Display(Name = "Last Name")]
		public string PatientLastName { get; set; }

		[Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date Of Birth")]
		public System.DateTime DateOfBirth { get; set; }

		[Required]
		[Display(Name = "Sex")]
		public string Sex { get; set; }

		[Display(Name = "Province")]
		public string Province { get; set; }

		[Required]
		[Display(Name = "Hospital Number")]
		public string HospitalNumber { get; set; }

		[Display(Name = "Referring Doctor Number")]
		public Nullable<int> ReferringDoctorNumber { get; set; }

        [Required]
		[Display(Name = "Date Of Service")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:ddMMyy}")]		
        public System.DateTime ServiceDate { get; set; }

        [Display(Name = "Date Of Discharge")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:ddMMyy}")]
        public System.DateTime DischargeDate { get; set; }

        [Display(Name = "Start Time")]
		public Nullable<System.TimeSpan> ServiceStartTime { get; set; }

		[Display(Name = "End Time")]
		public Nullable<System.TimeSpan> ServiceEndTime { get; set; }

		[Display(Name = "Comment")]
		public string Comment { get; set; }

        [Display(Name = "Personal Notes")]
        public string Notes { get; set; }

        [Display(Name = "Service Location")]
        public string ServiceLocation { get; set; }

        [Display(Name = "Facility Number")]
        public string FacilityNumber { get; set; }
    }
}
