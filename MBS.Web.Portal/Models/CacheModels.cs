using System;
using System.Linq;

namespace MBS.Web.Portal.Models
{
    public class Fee
	{
		public string myCode { get; set; }
		public float myUnitAmount { get; set; }

		public Fee(string myCode, float myUnitAmount)
		{
			this.myCode = myCode;
			this.myUnitAmount = myUnitAmount;
		}
	}

	public class ExplainCode
	{
		public string myCode { get; set; }
		public string myDesc { get; set; } 

		public ExplainCode(string myCode, string myDesc)
		{
			this.myCode = myCode;
			this.myDesc = myDesc;
		}
	}

	public class ICD
	{
		public string myCode { get; set; }
		public string myDescription { get; set; }

		public ICD(string myCode, string myDescription)
		{
			this.myCode = myCode;
			this.myDescription = myDescription;
		}
	}

	public class RefDoc
	{
		public int myNumber { get; set; }
		public string myCity { get; set; }
		public string myName { get; set; }

		public RefDoc(int myNumber, string myFirstName, string myLastName, string myCity)
		{
			this.myNumber = myNumber;

			if (!string.IsNullOrEmpty(myFirstName))
			{
				myFirstName = myFirstName.Substring(0, 1) + myFirstName.Substring(1).ToLower();
			}

			myLastName = myLastName.Substring(0, 1) + myLastName.Substring(1).ToLower();
			this.myName = myLastName + ", " + myFirstName;

			this.myCity = myCity;
		}
	}
    
    public class PatientInfo
    {
        public string myLastName { get; set; }

        public string myFirstName { get; set; }
        
		public string myBirthDate { get; set; }
        
		public string myHospitalNumber { get; set; }
        
		public string myProvince { get; set; }
        
		public string mySex { get; set; }  
        
        public string myReferringDocNumber { get; set; }      
    }

    public class SimpleRecord
    {
        public Guid ServiceRecordId { get; set; }

        public int ClaimNumber { get; set; }

        public string PatientFirstName { get; set; }

        public string PatientLastName { get; set; }

        public string HospitalNumber { get; set; }

        public System.DateTime ServiceDate { get; set; }

        public double ClaimAmount { get; set; }

        public int ClaimType { get; set; }

        public string AllExplainCodes { get; set; }

        public string DistinctExplainCodes
        {
            get
            {
                return string.Join(", ", AllExplainCodes.Split(',').Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x));
            }
        }

        public string AllFeeCodes { get; set; }

        public string DistinctFeeCodes
        {
            get
            {
                return string.IsNullOrEmpty(AllFeeCodes) ? string.Empty : string.Join(", ", AllFeeCodes.Split(',').Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x));
            }
        }


        public DateTime? LastModifiedDate { get; set; }

        public DateTime? SubmissionDate { get; set; }

        public string FileSubmittedStatus { get; set; }

        public string WCBFaxStatus { get; set; }


        public bool IsSubmissionPending
        {
            get
            {
                return this.FileSubmittedStatus == "PENDING";
            }
        }

        public string CPSClaimNumber { get; set; }
    }
    
    public class SearchResult
    {
        public string DistinctName { get; set; }

        public string ReferringDocNumber { get; set; }
    }

    public class TotalInfo
    {
        public TotalItem UnSubmitted { get; set; }

        public TotalItem Submitted { get; set; }

        public TotalItem Pending { get; set; }

        public TotalItem Paid { get; set; }

        public TotalItem Rejected { get; set; }

        public TotalItem Expiring { get; set; } 
    }

    public class TotalItem
    {
        public double Amount { get; set; }

        public int NumberOfRecords { get; set; }
    }

    public class NextNumberModel
    {
        public int RollOverNumber { get; set; }

        public int NextClaimNumber { get; set; }
    }

    public class SimpleFeeModel
    {
        public string key { get; set; }

        public string orderKey
        {
            get
            {
                return this.key.Replace(" - WCB", string.Empty);
            }
        }

        public string label { get; set; }

        public bool requiredUnitTime { get; set; }

        public bool requiredReferDoc { get; set; }

        public string feeDeterminant { get; set; }

        public float value { get; set; }
    }

    public class SimpleClaimReturn
    {
        public Guid ClaimsInReturnId { get; set; }

        public DateTime UploadDate { get; set;  }

        public string RunCode { get; set; }

        public double TotalPaidAmount { get; set; }

        public string ReturnFileName { get; set; }

        public int ReturnFileType { get; set; }
    }

    public class SimplePaymentInfo
    {
        public int GroupIndex { get; set; }

        public string LineType { get; set; }

        public string Description { get; set; }

        public string FeeSubmittedString { get; set; }

        public string FeeApprovedString { get; set; }

        public string TotalPremiumAmountString { get; set; }

        public string TotalProgramAmountString { get; set; }

        public string TotalPaidAmountString { get; set; }
    }

    public class PaymentInfoJson
    {
        public string[][] data { get; set; }
    }
}
