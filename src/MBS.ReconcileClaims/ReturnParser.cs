using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using MBS.DomainModel;
using MBS.Common;
using System.Text.RegularExpressions;

namespace MBS.ReconcileClaims
{
	/// <summary>
	/// Summary description for ReturnParser
	/// </summary>
	public class ReturnParser
	{
        private string myReturnContentForStorage;

		private string myDoctorNumber;
        private string myClinicNumber;
		private decimal mySubmittedTotal;
		private decimal myApprovedTotal;
		private string myReturnClaimsList = ",50,60,57,58,89,";
        private string myPremiumCodeList = string.Empty;

        private string myICSCommentLineStarter = string.Empty;

		private char myBreak = '\n';

        private IList<ReturnContent> myReturnContents = new List<ReturnContent>();
 
		private int myCurrentClaimNumber = 0;
		private Guid myUserId = new Guid();

        private ReturnModel myReturnModel = new ReturnModel();

        private MedicalBillingSystemEntities myContext;

        private bool _needToSave = false;

        public List<string> PaidLines = new List<string>();

		private struct LineItem
		{
			public string myClaimNumber;
			public string myLineContent;
		}

		private enum PAID_TYPE
		{
			PAID = 0,
			UNPAID = 1,
			UNKNOWN = 2,
			RETURNED_CLAIMS = 3
		}

		public ReturnParser(string myReturnContent, string myCurrentDoctorNumber, string myCurrentClinicNumber, Guid myUserId, MedicalBillingSystemEntities context)
		{
            myPremiumCodeList = GenericHelper.GetPremiumCodeList();
        
            if (!string.IsNullOrEmpty(myReturnContent))
            {
                myReturnContentForStorage = myReturnContent;
                
                string[] myLines = myReturnContent.Split(myBreak);
                if (myLines.Count() > 0)
                {
                    myDoctorNumber = myCurrentDoctorNumber.PadLeft(4, '0');
                    myClinicNumber = myCurrentClinicNumber.PadLeft(3, '0');

                    var myContents = IsContentBelongToMe(myLines);
                    if (myContents.Count() > 0)
                    {
                        this.myUserId = myUserId;
                        myContext = context;
                                                                       
                        var myUnprocessLines = GetUnProcessLines(myContents);
                        if (myUnprocessLines.Count() > 0)
                        {                            
                            ParseReturnLines(myUnprocessLines.ToArray());

                            myReturnModel.IsSuccess = true;
                        }
                        else
                        {
                            myReturnModel.ErrorType = ErrorType.EMPTY_CONTENT;
                        }
                    }
                    else
                    {
                        myReturnModel.ErrorType = ErrorType.EMPTY_CONTENT;
                    }
                }
            }
            else
            {
                myReturnModel.ErrorType = ErrorType.EMPTY_CONTENT;
            }
		}

        private IEnumerable<string> IsContentBelongToMe(string[] myLines)
        {
            var result = new List<string>();
            var pattern = string.Format("^1{0}(.*)99999M", myDoctorNumber);
            var regex = new Regex(pattern);

            foreach (var line in myLines)
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    var temp = line.Trim();
                    var ind = temp.IndexOf(myDoctorNumber);
                    if (ind == 1 || ind == 2)
                    {
                        if (!regex.IsMatch(temp))
                        {
                            result.Add(temp);
                        }
                    }
                }
            }

            return result.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }
        
        private void ParseReturnLines(string[] myLines)
		{
			string myTemp = string.Empty;

            var returnContent = GetDefaultReturnContent();
            var paidStarter = "1" + myDoctorNumber;

			for(var i = 0; i < myLines.Count(); i++)
			{
                myTemp = myLines[i].Trim();

				if (!string.IsNullOrEmpty(myTemp))
				{
					if (myTemp.Length > 105)
						myTemp = myTemp.Substring(0, 105);

					if (myTemp.IndexOf(myDoctorNumber) == 1 && myTemp.IndexOf("99999T                                       TOTAL      ") > 0)
					{
                        var totalTemp = myTemp.Substring(65, 11);
                        returnContent.TotalSubmitted += double.Parse(myTemp.Substring(65, 8).Trim() + "." + myTemp.Substring(73, 2));

                        totalTemp = myTemp.Substring(78, 11);
                        returnContent.TotalApproved += double.Parse(myTemp.Substring(78, 8).Trim() + "." + myTemp.Substring(86, 2));
                    }
					else
					{
						PAID_TYPE myType = IsPaidLine(myTemp);

						switch (myType)
						{
							case PAID_TYPE.PAID:
                                PaidLines.Add(myTemp);
                                returnContent.PaidItems.Add(CreateReturnLineItem(myTemp, 8));
								break;
							case PAID_TYPE.UNPAID:
                                returnContent.UnpaidItems.Add(CreateReturnLineItem(myTemp, 8));
								break;
							case PAID_TYPE.RETURNED_CLAIMS:
                                returnContent.ReturnClaimItems.Add(CreateReturnLineItem(myTemp, 6));                     
								break;
							default:
								break;
						}
					}
				}
			}

            myReturnContents.Add(returnContent);
		}

        private ReturnContent GetDefaultReturnContent()
        {
            var result = new ReturnContent();
            result.PaidItems = new List<ReturnLineItem>();
            result.UnpaidItems = new List<ReturnLineItem>();
            result.ReturnClaimItems = new List<ReturnLineItem>();
            
            return result;
        }
        
        private IEnumerable<string> GetUnProcessLines(IEnumerable<string> currentReturnLines)
        {            
            return currentReturnLines;
        }

        private ReturnLineItem CreateReturnLineItem(string myLine, int myClaimNumberIndex)
        {
            ReturnLineItem myLineItem = new ReturnLineItem();
            myLineItem.ClaimNumber = myLine.Substring(myClaimNumberIndex, 5);
            myLineItem.LineContent = myLine;
            return myLineItem;
        }		

		private PAID_TYPE IsPaidLine(string myLine)
		{
			PAID_TYPE myType = PAID_TYPE.UNKNOWN;

			if (myLine.IndexOf(myDoctorNumber) == 1 && myLine.IndexOf("P ") == 13)
			{                
                var paidAmount = myLine.Substring(81, 7).Trim();
                if (paidAmount.StartsWith("-") || paidAmount.Equals("000000"))
                {
                    myType = PAID_TYPE.UNPAID;
                }
                else
                {
                    myType = PAID_TYPE.PAID;
                }             
			}
			else
			{   
				string myRecordType = "," + myLine.Substring(0, 2) + ",";
				if (myReturnClaimsList.IndexOf(myRecordType) >= 0 && myLine.IndexOf(myDoctorNumber) == 2)
				{
					myType = PAID_TYPE.RETURNED_CLAIMS;                
				}
			}

			return myType;
		}

        public ReturnModel GetReturnModel()
        {
            return myReturnModel;
        }

		public IEnumerable<ClaimsInReturn> GenerateReturnClaims()
		{
            var result = new List<ClaimsInReturn>();

            foreach (var returnContent in myReturnContents)
            {                
                #region Create Claim return

                ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                myClaimsInReturn.ReturnFooter = string.Empty;
                myClaimsInReturn.TotalApproved = returnContent.TotalApproved;
                myClaimsInReturn.TotalSubmitted = returnContent.TotalSubmitted;
                myClaimsInReturn.UploadDate = DateTime.Now;
                myClaimsInReturn.UserId = myUserId;
                myClaimsInReturn.Content = myReturnContentForStorage;
                    
                RejectedClaim myRejectedClaim = new RejectedClaim();
                myRejectedClaim.RejectedClaimId = Guid.NewGuid();
                myRejectedClaim.CreatedDate = DateTime.Now;
                myRejectedClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;

                if (returnContent.UnpaidItems.Count() > 0)
                {                    
                    GenerateSerivceRecordFromUnpaidList(myRejectedClaim, returnContent.UnpaidItems);                      
                }

                if  (returnContent.ReturnClaimItems.Count() > 0)
                {
                    GenerateSerivceRecordFromReturnClaim(myRejectedClaim, returnContent.ReturnClaimItems);
                }

                if (myRejectedClaim.ServiceRecord.Count() > 0)
                {
                    myClaimsInReturn.RejectedClaim.Add(myRejectedClaim);
                }

                PaidClaim myPaidClaim = new PaidClaim();
                myPaidClaim.PaidClaimId = Guid.NewGuid();
                myPaidClaim.CreatedDate = DateTime.Now;
                myPaidClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;

                if (returnContent.PaidItems.Count() > 0)
                {                    
                    GenerateSerivceRecordFromPaidList(myPaidClaim, returnContent.PaidItems);                 
                }

                if (myPaidClaim.ServiceRecord.Count() > 0)
                {
                    myClaimsInReturn.PaidClaim.Add(myPaidClaim);
                }

                myClaimsInReturn.TotalPaid = myClaimsInReturn.PaidClaim.SelectMany(x => x.ServiceRecord).Count();
                myClaimsInReturn.TotalRejected = myClaimsInReturn.RejectedClaim.SelectMany(x => x.ServiceRecord).Count();

                result.Add(myClaimsInReturn);

                #endregion
            }

            return result;
		}

		private void GenerateSerivceRecordFromPaidList(PaidClaim myClaim, IEnumerable<ReturnLineItem> myPaidLines)
		{
            foreach (var serviceRecord in ProcessList(myClaim.PaidClaimId, myPaidLines, PAID_TYPE.PAID))
            {
                myClaim.ServiceRecord.Add(serviceRecord);
            }
		}

		private void GenerateSerivceRecordFromUnpaidList(RejectedClaim myClaim, IEnumerable<ReturnLineItem> myUnpaidLines)
		{
            foreach (var serviceRecord in ProcessList(myClaim.RejectedClaimId, myUnpaidLines, PAID_TYPE.UNPAID))
            {
                myClaim.ServiceRecord.Add(serviceRecord);
            }			
		}

		private void GenerateSerivceRecordFromReturnClaim(RejectedClaim myClaim, IEnumerable<ReturnLineItem> myReturnClaimLines)
		{
            foreach (var serviceRecord in ProcessList(myClaim.RejectedClaimId, myReturnClaimLines, PAID_TYPE.RETURNED_CLAIMS))
            {                
                myClaim.ServiceRecord.Add(serviceRecord);
            }
		}

        private List<ServiceRecord> ProcessList(Guid myClaimId, IEnumerable<ReturnLineItem> myLines, PAID_TYPE myType)
		{
			var myRecords = new List<ServiceRecord>();
			var myClaimNumbers = myLines.Select(r => r.ClaimNumber).Distinct().ToList();

			foreach (string myClaimNumber in myClaimNumbers)
			{                
                var myClaimLines = myLines.Where(x => x.ClaimNumber.Equals(myClaimNumber)).Select(x => x.LineContent).ToArray<string>();
                var myPatientInfo = GetPatientInfo(myClaimLines, myType);
                if (myPatientInfo != null)
                {
                    var record = GenerateServiceRecord(myClaimId, myClaimLines, int.Parse(myClaimNumber), myPatientInfo, myType);
                    if (record != null)
                    {                        
                        record.HospitalNumber = record.HospitalNumber;
                        record.PatientFirstName = record.PatientFirstName;
                        record.PatientLastName = record.PatientLastName;
                        
                        myRecords.Add(record);
                    }
                }
			}

			return myRecords;   
		}
        
		private ServiceRecord GenerateServiceRecord(Guid myClaimId, string[] myLines, int myClaimNumber, PatientInfo patientInfo , PAID_TYPE myType)
		{
            var myNewService = new ServiceRecord();
            myNewService.ServiceRecordId = Guid.NewGuid();
            myNewService.CreatedDate = DateTime.Now;
            myNewService.UserId = myUserId;
            myNewService.HospitalNumber = patientInfo.HospitalNumber;    
            myNewService.ClaimNumber = myClaimNumber;
            myNewService.DateOfBirth = DateTime.Now;
            myNewService.PatientLastName = patientInfo.LastName;
            myNewService.PatientFirstName = patientInfo.FirstName;
            myNewService.Province = patientInfo.Province;
            myNewService.ClaimAmount = 0;

            var myTemp = string.Empty;
            var myFirstLine = myLines.FirstOrDefault();
            if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
            {
                myNewService.ServiceDate = GetDateTimeFromPaidLine(myFirstLine);
                myNewService.Sex = "F";
            }
            else if (myType == PAID_TYPE.RETURNED_CLAIMS)
            {
                myNewService.Sex = myFirstLine[25].ToString();
                myNewService.DateOfBirth = GetDateTimeFromClaimsIn(myFirstLine.Substring(21, 4));

                myTemp = myFirstLine.Substring(54, 4).Trim();
                if (!string.IsNullOrEmpty(myTemp))
                {
                    myNewService.ReferringDoctorNumber = myTemp;
                }

                myNewService.ServiceDate = GetDateTimeFromClaimsIn(myFirstLine.Substring(58, 6));
            }

			if (myType == PAID_TYPE.RETURNED_CLAIMS)
			{
				myNewService.RejectedClaimId = myClaimId;				
                myNewService.MessageFromICS = GetMessageFromICS(myLines);
			}
            else if (myType == PAID_TYPE.UNPAID)
            {
                myNewService.RejectedClaimId = myClaimId;
            }
            else
            {
                myNewService.PaidClaimId = myClaimId;
            }

            var unitRecordList = new List<UnitRecord>();
            
			foreach (string myLine in myLines)
			{                
				if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
				{
                    CreateUnitRecord(myLine, myNewService.ServiceRecordId, myType, unitRecordList);
				}
				else if (myType == PAID_TYPE.RETURNED_CLAIMS)
				{
					if (myLine.StartsWith("50"))
					{
                        CreateUnitRecord(myLine, myNewService.ServiceRecordId, myType, unitRecordList);
					}
				}
			}

            var codes = unitRecordList.Where(x => myPremiumCodeList.IndexOf("," + x.UnitCode + ",") == -1).Select(x => x.UnitPremiumCode)
                            .Distinct().OrderByDescending(x => x).ToList();
            var maxCode = codes.FirstOrDefault();

            if (string.IsNullOrEmpty(maxCode))
            {
                maxCode = "2";
            }

            int i = 1;
            foreach(var unitRecord in unitRecordList.OrderBy(x => x.UnitCode))
            {
                unitRecord.RecordIndex = i;
                unitRecord.UnitPremiumCode = maxCode;
                myNewService.UnitRecord.Add(unitRecord);
                i++;
            }

            myNewService.PaidAmount = myNewService.UnitRecord.Sum(x => x.PaidAmount);

            if (myNewService.ClaimAmount == 0)
            {
                myNewService.ClaimAmount = myNewService.PaidAmount;
            }
           
			return myNewService;
		}

        private PatientInfo GetPatientInfo(string[] myLines, PAID_TYPE myType)
        {
            var result = new PatientInfo();
            result.Province = "SK";

            if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
            {
                var myFirstLine = myLines.FirstOrDefault();

                var number = 0;
                int.TryParse(myFirstLine.Substring(55, 2), out number);
                result.ServiceDateMonth = number;

                int.TryParse(myFirstLine.Substring(52, 2), out number);
                result.ServiceDateDay = number;
                
                result.HospitalNumber = myFirstLine.Substring(35, 9).Trim();

                result = GetPatientName(myFirstLine.Substring(15, 19).Trim(), result);
            }
            else if (myType == PAID_TYPE.RETURNED_CLAIMS)
            {
                var myFirstLine = GetClaimInLine(myLines, "50");

                if (string.IsNullOrEmpty(myFirstLine))
                {
                    return null;
                }

                result.ServiceDateDay = int.Parse(myFirstLine.Substring(58, 2));
                result.ServiceDateMonth = int.Parse(myFirstLine.Substring(60, 2));

                var myTemp = GetClaimInLine(myLines, "89");

                if (!string.IsNullOrEmpty(myTemp))
                {
                    result.HospitalNumber = myTemp.Substring(51, 12).Trim();
                    result.Province = myTemp.Substring(21, 2);
                    result.FirstName = myTemp.Substring(23, 18).Trim();
                    result.LastName = myTemp.Substring(41, 9).Trim();
                }
                else
                {
                    result.HospitalNumber = myFirstLine.Substring(12, 9).Trim();
                    result = GetPatientName(myFirstLine.Substring(26, 25).Trim(), result);
                }
            }

            return result;
        }

        private PatientInfo GetPatientName(string nameLine, PatientInfo info)
        {
            var myName = nameLine.Split(',');
            info.LastName = myName[0].Trim();
            info.FirstName = "Please fill in!";

            if (myName.Count() > 1)
            {
                var myTemp = myName[1].Trim();
                if (!string.IsNullOrEmpty(myTemp))
                {
                    info.FirstName = myTemp;
                }
            }

            return info;
        }
        
		private void CreateUnitRecord(string myLine, Guid serviceRecordId, PAID_TYPE myType, List<UnitRecord> unitRecordList)
		{
			int myUnitNumberIndex = 0; 
			int myUnitCodeIndex = 0;
			int myAmountIndex = 0;
			int myExplainCodeIndex = 89;
			int myAmountTenthIndex = 0;
            int mySecondAmountIndex = 0;
			if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
			{
				myUnitNumberIndex = 60;
				myUnitCodeIndex = 63;
				myAmountIndex = 68;
				myAmountTenthIndex = 5;
                mySecondAmountIndex = 81;
			}
			else if (myType == PAID_TYPE.RETURNED_CLAIMS)
			{
				myUnitNumberIndex = 64;
				myUnitCodeIndex = 67;
				myAmountIndex = 71;
				myAmountTenthIndex = 4;
			}

			UnitRecord myUnitRecord = new UnitRecord();
			myUnitRecord.ServiceRecordId = serviceRecordId;
			myUnitRecord.UnitRecordId = Guid.NewGuid();
			myUnitRecord.UnitNumber = int.Parse(myLine.Substring(myUnitNumberIndex, 2));
			myUnitRecord.UnitCode = myLine.Substring(myUnitCodeIndex, 4).TrimStart('0');
			myUnitRecord.ExplainCode = null;
			myUnitRecord.UnitAmount = Convert.ToDouble(myLine.Substring(myAmountIndex, myAmountTenthIndex) + "." + myLine.Substring(myAmountIndex + myAmountTenthIndex, 2));

			string myTemp = myLine.Substring(myExplainCodeIndex, 2).Trim();
            if (!string.IsNullOrEmpty(myTemp))
            {
                myUnitRecord.ExplainCode = myTemp;
                if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
                {
                    myUnitRecord.PaidAmount = Math.Abs(Convert.ToDouble(myLine.Substring(mySecondAmountIndex, myAmountTenthIndex) + "." + myLine.Substring(mySecondAmountIndex + myAmountTenthIndex, 2)));
                }
            }            

            if (myLine.StartsWith("50"))
			{
				myUnitRecord.UnitPremiumCode = myLine[66].ToString().ToLower();
			}
			else
			{
				myUnitRecord.UnitPremiumCode = "2";
			}
           
            if (myType == PAID_TYPE.UNPAID || myType == PAID_TYPE.PAID)
            {
                double result = Math.Abs(Convert.ToDouble(myLine.Substring(98, 5).Trim() + "." + myLine.Substring(103, 2)));
                if (result > 0)
                {
                    var ratio = string.Format("{0:F1}", (result / myUnitRecord.UnitAmount));
                    if (ratio == "0.5")
                    {
                        myUnitRecord.UnitPremiumCode = "b";
                    }
                    else if (ratio == "1.0")
                    {
                        myUnitRecord.UnitPremiumCode = "k";
                    }
                }

                myUnitRecord.PaidAmount = myUnitRecord.UnitAmount + result;
            }
            else
            {
                myUnitRecord.PaidAmount = myUnitRecord.UnitAmount;
            }

            unitRecordList.Add(myUnitRecord);
		}

		private string GetClaimInLine(string[] myLines, string myHeaderType)
		{
			return myLines.FirstOrDefault(s => s.StartsWith(myHeaderType)); 
		}

        private string GetMessageFromICS(string[] myLines)
        {
            var messageLines = myLines.Where(s => s.StartsWith("60"));
            var result = string.Empty;

            foreach (var message in messageLines)
            {
                if (message.Length > 94)
                    result += message.Substring(21, 74).Trim() + " ";
                else
                    result += message.Substring(21).Trim() + " ";
            }

            return result;
        }

		private DateTime GetDateTimeFromPaidLine(string myLine)    
		{
			int Day = 1;
			int Month = 1;
			int Year = 1;

			Day = int.Parse(myLine.Substring(52, 2));
			Month = int.Parse(myLine.Substring(55, 2));

			myLine = myLine.Substring(58, 1);        

			string myLastDigit = DateTime.Now.Year.ToString().Substring(DateTime.Now.Year.ToString().Length - 1);

			if (myLine == myLastDigit)
			{
				Year = DateTime.Now.Year;
			}
			else
			{
				Year = DateTime.Now.Year - 1;
				myLastDigit = Year.ToString().Substring(Year.ToString().Length - 1);
				while (!myLastDigit.Equals(myLine))
				{
					Year--;
					myLastDigit = Year.ToString().Substring(Year.ToString().Length - 1);

					if (DateTime.Now.Year - Year > 9)
					{                    
						break;
					}
				}
			}
        
			return new DateTime(Year, Month, Day);
		}

		private DateTime GetDateTimeFromClaimsIn(string myClaimsInDate)
		{
			int Day = 1;
			int Month = 1;
			int Year = 1;
			int Index = 0;
        
			if (myClaimsInDate.Length == 6)
			{
				Day = int.Parse(myClaimsInDate.Substring(0, 2));
				Index = 2;
			}
                
			Month = int.Parse(myClaimsInDate.Substring(Index, 2));
			Index = Index + 2;

            var firstTwoDigit = DateTime.Now.Year.ToString().Substring(0, 2);
            Year = int.Parse(firstTwoDigit + myClaimsInDate.Substring(Index, 2));
        
			var result = new DateTime(Year, Month, Day);

            if (myClaimsInDate.Length == 4) //Birthday
            {
                if (result > DateTime.UtcNow.AddHours(-6).Date)
                {
                    result = result.AddYears(-100);
                }
            }

            return result;
		}       
        
        public PaidItem GetPaidAmount(string myLine)
        {
            var serviceDate = GetDateTimeFromPaidLine(myLine);

            var submittedAmount = decimal.Parse(myLine.Substring(68, 5) + "." + myLine.Substring(68 + 5, 2));
            var approveAmount = decimal.Parse(myLine.Substring(81, 5) + "." + myLine.Substring(81 + 5, 2));
            var premium = decimal.Parse(myLine.Substring(98, 5).Trim() + "." + myLine.Substring(103, 2));
            var claimNumber = myLine.Substring(8, 5);
            var clinicNumber = myLine.Substring(5, 3);
            return new PaidItem()
            {
                ServiceDate = serviceDate,
                PaidAmount = approveAmount + premium,
                IsTheSame = submittedAmount == approveAmount,
                ClaimNumber = claimNumber,
                ClinicNumber = clinicNumber
            };
        }
    }

    public class PatientInfo
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Province { get; set; }

        public int ServiceDateMonth { get; set; }

        public int ServiceDateDay { get; set; }

        public string HospitalNumber { get; set; }
    }

    public class ReturnLineItem
    {
        public string ClaimNumber { get; set; }

        public string LineContent { get; set; }
    }

    public class ReturnContent
    {
        public double TotalApproved { get; set; }

        public double TotalSubmitted { get; set; }
        
        public IList<ReturnLineItem> PaidItems { get; set; }

        public IList<ReturnLineItem> UnpaidItems { get; set; }

        public IList<ReturnLineItem> ReturnClaimItems { get; set; }

        public string Footer { get; set; }
    }

    public class PaidItem
    {
        public DateTime ServiceDate { get; set; }

        public string ClaimNumber { get; set; }

        public string ClinicNumber { get; set; }

        public bool IsTheSame { get; set; }

        public decimal PaidAmount { get; set; }
    }
}
