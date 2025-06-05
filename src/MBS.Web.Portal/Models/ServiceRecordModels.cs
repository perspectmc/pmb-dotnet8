using System;
using System.ComponentModel.DataAnnotations;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using System.Collections.Generic;

namespace MBS.Web.Portal.Models
{
    public class ServiceRecordDetailModel
    {
        public string ReferFrom { get; set; }

        public string LastSelectedDiagCode { get; set; }

        public string ButtonUsedToSubmit { get; set; }

        public List<string> PremiumCodeList { get; set; }
    
        public List<string> HospitalCareServiceCodeList { get; set; }

        public List<string> RNPExcludeCodeList { get; set; }

        public ServiceRecord Record { get; set; }

        public string PremiumCode { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        public string BirthDateString { get; set; }

        [Required]
        [Display(Name = "Date Of Service")]
        public string ServiceDateString { get; set; }

        [Display(Name = "Date Of Discharge")]
        public string DischargeDateString { get; set; }

        [Display(Name = "Start Time")]
        public string ServiceStartTimeString { get; set; }
        
        [Display(Name = "End Time")]
        public string ServiceEndTimeString { get; set; }
        
        public string MostUsedCodes { get; set; }

        public string UnitCodeAmount1 { get; set; }
		public string UnitCode1 { get; set; }
		public string UnitNumber1 { get; set; }
		public string UnitAmount1 { get; set; }
		public string ExplainCode1 { get; set; }
        public string ExplainCode1b { get; set; }
        public string ExplainCode1c { get; set; }

        public string ExplainCodeDesc1 { get; set; }
        public string ExplainCodeDesc1b { get; set; }
        public string ExplainCodeDesc1c { get; set; }
        public string OriginalRunCode1 { get; set; }

        public string DiagCode1 { get; set; }
        public string RunCode1 { get; set; }
        public string RecordClaimType1 { get; set; }
        public string SpecialCircumstanceIndicator1 { get; set; }
        public string BilateralIndicator1 { get; set; }
        public string UnitStartTime1 { get; set; }
        public string UnitEndTime1 { get; set; }
        public string UnitPremiumCode1 { get; set; }
        public string UnitAmountDesc1 { get; set; }
        public string UnitSubmittedRecordIndex1 { get; set; }

        public string UnitCodeAmount2 { get; set; }
		public string UnitCode2 { get; set; }
		public string UnitNumber2 { get; set; }
		public string UnitAmount2 { get; set; }
		public string ExplainCode2 { get; set; }
        public string ExplainCode2b { get; set; }
        public string ExplainCode2c { get; set; }

        public string ExplainCodeDesc2 { get; set; }
        public string ExplainCodeDesc2b { get; set; }
        public string ExplainCodeDesc2c { get; set; }
        public string OriginalRunCode2 { get; set; }

        public string DiagCode2 { get; set; }
        public string RunCode2 { get; set; }
        public string RecordClaimType2 { get; set; }
        public string SpecialCircumstanceIndicator2 { get; set; }
        public string BilateralIndicator2 { get; set; }
        public string UnitStartTime2 { get; set; }
        public string UnitEndTime2 { get; set; }
        public string UnitPremiumCode2 { get; set; }
        public string UnitAmountDesc2 { get; set; }
        public string UnitSubmittedRecordIndex2 { get; set; }

        public string UnitCodeAmount3 { get; set; }
		public string UnitCode3 { get; set; }
		public string UnitNumber3 { get; set; }
		public string UnitAmount3 { get; set; }
		public string ExplainCode3 { get; set; }
        public string ExplainCode3b { get; set; }
        public string ExplainCode3c { get; set; }
        public string OriginalRunCode3 { get; set; }

        public string ExplainCodeDesc3 { get; set; }
        public string ExplainCodeDesc3b { get; set; }
        public string ExplainCodeDesc3c { get; set; }

        public string DiagCode3 { get; set; }
        public string RunCode3 { get; set; }
        public string RecordClaimType3 { get; set; }
        public string SpecialCircumstanceIndicator3 { get; set; }
        public string BilateralIndicator3 { get; set; }
        public string UnitStartTime3 { get; set; }
        public string UnitEndTime3 { get; set; }
        public string UnitPremiumCode3 { get; set; }
        public string UnitAmountDesc3 { get; set; }
        public string UnitSubmittedRecordIndex3 { get; set; }


        public string UnitCodeAmount4 { get; set; }
		public string UnitCode4 { get; set; }
		public string UnitNumber4 { get; set; }
		public string UnitAmount4 { get; set; }
		public string ExplainCode4 { get; set; }
        public string ExplainCode4b { get; set; }
        public string ExplainCode4c { get; set; }
        public string ExplainCodeDesc4 { get; set; }
        public string ExplainCodeDesc4b { get; set; }
        public string ExplainCodeDesc4c { get; set; }
        public string OriginalRunCode4 { get; set; }

        public string DiagCode4 { get; set; }
        public string RunCode4 { get; set; }
        public string RecordClaimType4 { get; set; }
        public string SpecialCircumstanceIndicator4 { get; set; }
        public string BilateralIndicator4 { get; set; }
        public string UnitStartTime4 { get; set; }
        public string UnitEndTime4 { get; set; }
        public string UnitPremiumCode4 { get; set; }
        public string UnitAmountDesc4 { get; set; }
        public string UnitSubmittedRecordIndex4 { get; set; }


        public string UnitCodeAmount5 { get; set; }
		public string UnitCode5 { get; set; }
		public string UnitNumber5 { get; set; }
		public string UnitAmount5 { get; set; }
		public string ExplainCode5 { get; set; }
        public string ExplainCode5b { get; set; }
        public string ExplainCode5c { get; set; }
        public string ExplainCodeDesc5 { get; set; }
        public string ExplainCodeDesc5b { get; set; }
        public string ExplainCodeDesc5c { get; set; }
        public string OriginalRunCode5 { get; set; }

        public string DiagCode5 { get; set; }
        public string RunCode5 { get; set; }
        public string RecordClaimType5 { get; set; }
        public string SpecialCircumstanceIndicator5 { get; set; }
        public string BilateralIndicator5 { get; set; }
        public string UnitStartTime5 { get; set; }
        public string UnitEndTime5 { get; set; }
        public string UnitPremiumCode5 { get; set; }
        public string UnitAmountDesc5 { get; set; }
        public string UnitSubmittedRecordIndex5 { get; set; }


        public string UnitCodeAmount6 { get; set; }
		public string UnitCode6 { get; set; }
		public string UnitNumber6 { get; set; }
		public string UnitAmount6 { get; set; }
		public string ExplainCode6 { get; set; }
        public string ExplainCode6b { get; set; }
        public string ExplainCode6c { get; set; } 

        public string ExplainCodeDesc6 { get; set; }
        public string ExplainCodeDesc6b { get; set; }
        public string ExplainCodeDesc6c { get; set; }
        public string OriginalRunCode6 { get; set; }

        public string DiagCode6 { get; set; }
        public string RunCode6 { get; set; }
        public string RecordClaimType6 { get; set; }
        public string SpecialCircumstanceIndicator6 { get; set; }
        public string BilateralIndicator6 { get; set; }
        public string UnitStartTime6 { get; set; }
        public string UnitEndTime6 { get; set; }
        public string UnitPremiumCode6 { get; set; }
        public string UnitAmountDesc6 { get; set; }
        public string UnitSubmittedRecordIndex6 { get; set; }

        public string UnitCodeAmount7 { get; set; }
        public string UnitCode7 { get; set; }
        public string UnitNumber7 { get; set; }
        public string UnitAmount7 { get; set; }
        public string ExplainCode7 { get; set; }
        public string ExplainCode7b { get; set; }
        public string ExplainCode7c { get; set; }
        public string ExplainCodeDesc7 { get; set; }
        public string ExplainCodeDesc7b { get; set; }
        public string ExplainCodeDesc7c { get; set; }
        public string OriginalRunCode7 { get; set; }

        public string DiagCode7 { get; set; }
        public string RunCode7 { get; set; }
        public string RecordClaimType7 { get; set; }
        public string SpecialCircumstanceIndicator7 { get; set; }
        public string BilateralIndicator7 { get; set; }
        public string UnitStartTime7 { get; set; }
        public string UnitEndTime7 { get; set; }
        public string UnitPremiumCode7 { get; set; }
        public string UnitAmountDesc7 { get; set; }
        public string UnitSubmittedRecordIndex7 { get; set; }

    }
}