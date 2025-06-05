using MBS.Common;
using MBS.DataCache;
using MBS.DomainModel;
using MBS.Web.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MBS.Web.Portal.Services
{
    /// <summary>
    /// Summary description for ReturnParser
    /// </summary>
    public class ReturnParser
    {
        private string myReturnContentForStorage;

        private string myDoctorNumber;
        private string myClinicNumber;
        private string myDiagCode;

        private List<string> myPremiumCodeList;

        private char myBreak = '\n';

        private IList<ReturnContent> myReturnContents = new List<ReturnContent>();

        private List<TotalLine> myTotalLines = new List<TotalLine>();

        private Guid myUserId = new Guid();

        private ReturnModel myReturnModel = new ReturnModel();

        private MedicalBillingSystemEntities myContext;

        private int _timeZoneOffset = -6;

        private bool _rejectedClaimNeeded = false;

        private bool _paidClaimNeeded = false;

        public ReturnParser(string myReturnContent, ReturnFileType returnFileType, UserProfiles userProfile, string previousReturnContent, MedicalBillingSystemEntities context)
        {
            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            myPremiumCodeList = StaticCodeList.MyPremiumCodeList;
            myDiagCode = userProfile.DiagnosticCode;
            myTotalLines.Clear();

            if (!string.IsNullOrEmpty(myReturnContent))
            {
                myReturnContentForStorage = myReturnContent;

                string[] myLines = myReturnContent.Split(myBreak);
                if (myLines.Count() > 0)
                {
                    myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                    myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                    var myContents = RemoveUnRelatedLines(myLines);
                    if (myContents.Count() > 0)
                    {
                        this.myUserId = userProfile.UserId;
                        myContext = context;

                        var myUnprocessLines = GetUnProcessLines(myContents, returnFileType, previousReturnContent);
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

        private IEnumerable<string> RemoveUnRelatedLines(string[] myLines)
        {
            //Header lines, Trailer Lines, MSB Message lines
            var result = new List<string>();
            var doctorNumberIndex = 0;
            var messageLineStart = "1" + myDoctorNumber.PadLeft(4, '0') + myClinicNumber.PadLeft(3, '0');
            var messageLinePattern1 = messageLineStart + "     M";
            var messageLinePattern2 = messageLineStart + "99999M";
            foreach (var line in myLines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    doctorNumberIndex = line.IndexOf(myDoctorNumber);
                    if (doctorNumberIndex == 1 || doctorNumberIndex == 2)
                    {
                        if (!line.StartsWith("10" + myDoctorNumber) && !line.StartsWith("90" + myDoctorNumber) && !line.StartsWith(messageLinePattern1) && !line.StartsWith(messageLinePattern2))
                        {
                            var paidClinicNumber = line.Substring(5, 3);
                            var returnClinicNumber = line.Substring(95, 3);

                            if (paidClinicNumber.Equals(myClinicNumber, StringComparison.OrdinalIgnoreCase) || returnClinicNumber.Equals(myClinicNumber, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(line.Trim());
                            }
                        }
                    }
                }
            }

            return result.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        private void ParseReturnLines(string[] myLines)
        {
            var myLine = string.Empty;
            var lineItems = new List<ReturnLineItem>();

            for (var i = 0; i < myLines.Count(); i++)
            {
                myLine = myLines[i];

                if (!string.IsNullOrEmpty(myLine))
                {
                    if (myLine.IndexOf("99999T") == 8 || myLine.IndexOf("     T") == 8)
                    {
                        var totalLine = new TotalLine();
                        totalLine.Line = myLine;
                        totalLine.LineNumber = i;
                        totalLine.FeeSubmitted = Convert.ToDouble(myLine.Substring(64, 9) + "." + myLine.Substring(73, 2));
                        totalLine.FeeApproved += Convert.ToDouble(myLine.Substring(77, 9) + "." + myLine.Substring(86, 2));
                        totalLine.PremiumAmount += Convert.ToDouble(myLine.Substring(98, 9) + "." + myLine.Substring(107, 2));
                        totalLine.ProgramAmount += Convert.ToDouble(myLine.Substring(109, 9) + "." + myLine.Substring(118, 2));
                        totalLine.PaidAmount += Convert.ToDouble(myLine.Substring(120, 9) + "." + myLine.Substring(129, 2));                     
                        totalLine.IsTotal = myLine.IndexOf("TOTAL     ") == 53;
                        totalLine.RunCode = myLine.Substring(253, 2);

                        myTotalLines.Add(totalLine);
                    }
                    else
                    {
                        var lineItem = CreateReturnLineItem(myLine);
                        if (lineItem.PaidType != PAID_TYPE.PENDING_CLAIMS)
                        {
                            lineItems.Add(lineItem);
                        }
                    }
                }
            }

            var returnContent = new ReturnContent();
            returnContent.PaidItems = new List<ClaimNumberGroup>();
            returnContent.ReturnClaimItems = new List<ClaimNumberGroup>();

            if (lineItems.Any())
            {
                var claimsGroupByCPSClaimNumber = lineItems.GroupBy(x => x.CPSClaimNumber)
                    .Select(x => new
                    {
                        CPSClaimNumber = x.Key,
                        ClaimNumber = x.FirstOrDefault().ClaimNumber,
                        Lines = x.ToList()
                    }).ToList();

                foreach (var claimsGroup in claimsGroupByCPSClaimNumber)
                {
                    var paidLineItems = claimsGroup.Lines.Where(x => x.PaidType == PAID_TYPE.PAID).OrderBy(x => x.ClaimAndSeqNumber).ToList();
                    if (paidLineItems.Any())
                    {
                        returnContent.PaidItems.Add(new ClaimNumberGroup()
                        {
                            ClaimNumber = claimsGroup.ClaimNumber,
                            PaidType = PAID_TYPE.PAID,
                            ReturnLineItems = paidLineItems,
                            FirstLineItem = paidLineItems.FirstOrDefault(),
                            ClaimPatientInfo = paidLineItems.FirstOrDefault().PatientInfo
                        });
                    }

                    //Visit will go first, then Hospital Case, then OOP, then Comment, then PAID
                    var rejectedLineItems = claimsGroup.Lines.Where(x => x.PaidType == PAID_TYPE.RETURNED_CLAIMS).OrderBy(x => x.ReturnedRecordType).ToList();
                    var containReturnLineItemsHospitalCareOrVisitProcedure = rejectedLineItems.Any(x =>
                                        (x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE));
                    if (rejectedLineItems.Any() && containReturnLineItemsHospitalCareOrVisitProcedure)
                    {
                        var patientInfo = new PatientInfo();

                        var temp = rejectedLineItems.FirstOrDefault().PatientInfo;
                        patientInfo.BirthDate = temp.BirthDate;
                        patientInfo.Sex = temp.Sex;
                        patientInfo.FirstName = temp.FirstName;
                        patientInfo.LastName = temp.LastName;
                        patientInfo.HospitalNumber = temp.HospitalNumber;
                        patientInfo.Province = temp.Province;

                        if (rejectedLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE))
                        {
                            var oopPatientInfo = rejectedLineItems.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE).PatientInfo;
                            patientInfo.Province = oopPatientInfo.Province;
                            patientInfo.FirstName = oopPatientInfo.FirstName;
                            patientInfo.LastName = oopPatientInfo.LastName;
                            patientInfo.HospitalNumber = oopPatientInfo.HospitalNumber;
                        }                      

                        returnContent.ReturnClaimItems.Add(new ClaimNumberGroup()
                        {
                            ClaimNumber = claimsGroup.ClaimNumber,
                            PaidType = PAID_TYPE.RETURNED_CLAIMS,
                            ReturnLineItems = rejectedLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber) && x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT).ToList(),
                            MSBComment = string.Join(string.Empty, rejectedLineItems
                                                .Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.COMMENT)
                                                .OrderBy(x => x.CommentLineNumber).Select(x => x.Comment)),
                            FirstLineItem = rejectedLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber) && x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT).OrderBy(x => x.ClaimAndSeqNumber).FirstOrDefault(),
                            ClaimPatientInfo = patientInfo                           
                        });
                    }
                }
            }

            myReturnContents.Add(returnContent);
        }
        
        private IEnumerable<string> GetUnProcessLines(IEnumerable<string> currentReturnLines, ReturnFileType returnFileType, string previousReturnContent)
        {
            if (previousReturnContent.Any())
            {
                var previousLines = previousReturnContent.Split(myBreak);
                if (previousLines.Count() > 0)
                {                    
                    var myPreviousLines = RemoveUnRelatedLines(previousLines);
                    if (myPreviousLines.Count() > 0)
                    {
                        return currentReturnLines.Distinct().Except(myPreviousLines.Distinct()).ToList();
                    }
                }
            }
            
            return currentReturnLines.Distinct().ToList();
        }
        
        private ReturnLineItem CreateReturnLineItem(string myLine)
        {
            ReturnLineItem myLineItem = new ReturnLineItem();
            myLineItem.LineContent = myLine;

            if (myLine.Substring(13, 2) == "P ")
            {
                myLineItem.ClaimNumber = int.Parse(myLine.Substring(8, 5));
                myLineItem.ClinicNumber = myLine.Substring(5, 3);
                myLineItem.SeqNumber = int.Parse(myLine.Substring(50, 1));
                myLineItem.PaidType = PAID_TYPE.PAID;
                myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.PAID;

                myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);

                myLineItem.ApprovedUnitNumber = Math.Abs(int.Parse(myLine.Substring(127, 3)));
                myLineItem.ApprovedUnitCode = myLine.Substring(76, 4).Trim().TrimStart('0');
                myLineItem.ApprovedUnitAmount = Convert.ToDouble(myLine.Substring(81, 5) + "." + myLine.Substring(86, 2));
                myLineItem.ApprovedLocationOfService = myLine.Substring(130, 1);

                myLineItem.SubmittedUnitNumber = Math.Abs(int.Parse(myLine.Substring(60, 3)));
                myLineItem.SubmittedUnitCode = myLine.Substring(63, 4).Trim().TrimStart('0');

                var submittedUnitAmount = 0d;
                Double.TryParse(myLine.Substring(68, 5) + "." + myLine.Substring(73, 2), out submittedUnitAmount);
                myLineItem.SubmittedUnitAmount = submittedUnitAmount;

                myLineItem.PremiumAmount = Convert.ToDouble(myLine.Substring(98, 5) + "." + myLine.Substring(103, 2));
                myLineItem.ProgramPayment = Convert.ToDouble(myLine.Substring(105, 5) + "." + myLine.Substring(110, 2));

                myLineItem.RunCode = myLine.Substring(95, 2).Trim().ToUpper();

                myLineItem.ExplainCode1 = myLine.Substring(89, 2).Trim();
                myLineItem.ExplainCode2 = myLine.Substring(123, 2).Trim();
                myLineItem.ExplainCode3 = myLine.Substring(125, 2).Trim();

                myLineItem.ServiceDate = new DateTime(
                            int.Parse("20" + myLine.Substring(58, 2)),
                            int.Parse(myLine.Substring(55, 2)),
                            int.Parse(myLine.Substring(52, 2)));

                myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

            }
            else if (myLine.StartsWith("50") || myLine.StartsWith("57") || myLine.StartsWith("89") || myLine.StartsWith("60"))
            {
                myLineItem.ClaimNumber = int.Parse(myLine.Substring(6, 5));
                myLineItem.ClinicNumber = myLine.Substring(95, 3);
                myLineItem.PaidType = PAID_TYPE.RETURNED_CLAIMS;

                switch (myLine.Substring(0,2))
                {
                    case "50":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.VISIT_PROCEDURE;
                        myLineItem.SeqNumber = int.Parse(myLine.Substring(11, 1));
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);

                        myLineItem.DiagnosticCode = myLine.Substring(51, 3).Trim().TrimStart('0');

                        myLineItem.SubmittedUnitNumber = Math.Abs(int.Parse(myLine.Substring(64, 2)));
                        myLineItem.SubmittedUnitCode = myLine.Substring(67, 4).Trim().TrimStart('0');
                        myLineItem.SubmittedUnitAmount = Convert.ToDouble(myLine.Substring(71, 4) + "." + myLine.Substring(75, 2));
                        myLineItem.SubmittedLocationOfService = myLine.Substring(66, 1);

                        myLineItem.RunCode = myLine.Substring(91, 2).Trim().ToUpper();

                        myLineItem.ExplainCode1 = myLine.Substring(89, 2).Trim();
                        myLineItem.ExplainCode2 = myLine.Substring(99, 2).Trim();
                        myLineItem.ExplainCode3 = myLine.Substring(101, 2).Trim();

                        myLineItem.ServiceDate = new DateTime(
                            int.Parse("20" + myLine.Substring(62, 2)),
                            int.Parse(myLine.Substring(60, 2)),
                            int.Parse(myLine.Substring(58, 2)));

                        myLineItem.ReferringDoctorNumber = myLine.Substring(54, 4).Trim().TrimStart('0');
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    case "57":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.HOSPITAL_CARE;
                        myLineItem.SeqNumber = int.Parse(myLine.Substring(11, 1));
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);

                        myLineItem.DiagnosticCode = myLine.Substring(51, 3).Trim().TrimStart('0');

                        myLineItem.SubmittedUnitNumber = Math.Abs(int.Parse(myLine.Substring(70, 2)));
                        myLineItem.SubmittedUnitCode = myLine.Substring(72, 4).Trim().TrimStart('0');
                        myLineItem.SubmittedUnitAmount = Convert.ToDouble(myLine.Substring(76, 4) + "." + myLine.Substring(80, 2));

                        myLineItem.RunCode = myLine.Substring(91, 2).ToUpper();

                        myLineItem.ExplainCode1 = myLine.Substring(93, 2).Trim();
                        myLineItem.ExplainCode2 = myLine.Substring(99, 2).Trim();
                        myLineItem.ExplainCode3 = myLine.Substring(101, 2).Trim();

                        myLineItem.ServiceDate = new DateTime(
                            int.Parse("20" + myLine.Substring(62, 2)),
                            int.Parse(myLine.Substring(60, 2)),
                            int.Parse(myLine.Substring(58, 2)));

                        myLineItem.LastServiceDate = new DateTime(
                            int.Parse("20" + myLine.Substring(68, 2)),
                            int.Parse(myLine.Substring(66, 2)),
                            int.Parse(myLine.Substring(64, 2)));

                        myLineItem.ReferringDoctorNumber = myLine.Substring(54, 4).Trim().TrimStart('0');
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    case "89":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.OUT_OF_PROVINCE;
                        myLineItem.SeqNumber = -1;
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    case "60":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.COMMENT;
                        myLineItem.SeqNumber = -1;
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);
                        myLineItem.CommentLineNumber = myLine.Substring(11, 1).Trim();
                        myLineItem.Comment = myLine.Substring(21, 74).Trim().ToUpper();
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    default:
                        break;
                }

                if (myLine.Substring(98, 1) == "P") //Status
                {
                    myLineItem.PaidType = PAID_TYPE.PENDING_CLAIMS;
                }
            }

            return myLineItem;
        }

        private void GeneratePaidClaimSerivceRecords(PaidClaim myPaidClaim, IList<ClaimNumberGroup> myClaimGroups)
        {
            foreach (var serviceRecord in ProcessList(myPaidClaim.PaidClaimId, ReturnFileType.BIWEEKLY, myClaimGroups))
            {
                myPaidClaim.ServiceRecord.Add(serviceRecord);
            }
        }

        private void GenerateReturnClaimSerivceRecords(RejectedClaim myRejectedClaim, ReturnFileType myReturnFileType, IList<ClaimNumberGroup> myClaimGroups)
        {
            foreach (var serviceRecord in ProcessList(myRejectedClaim.RejectedClaimId, myReturnFileType, myClaimGroups))
            {
                myRejectedClaim.ServiceRecord.Add(serviceRecord);
            }
        }

        private List<ServiceRecord> ProcessList(Guid myReturnClaimId, ReturnFileType myReturnFileType, IList<ClaimNumberGroup> myClaimGroups)
        {
            var myRecords = new List<ServiceRecord>();

            foreach (ClaimNumberGroup myClaimGroup in myClaimGroups)
            {
                if (!string.IsNullOrEmpty(myClaimGroup.ClaimPatientInfo.HospitalNumber))
                {
                    myRecords.AddRange(GenerateServiceRecord(myReturnClaimId, myClaimGroup));
                }
            }

            return myRecords;
        }

        private List<ClaimNumberAndSeqWithAdjustmentType> CheckAdjustmentsInReturn(ClaimNumberGroup claimGroup)
        {
            var result = new List<ClaimNumberAndSeqWithAdjustmentType>();

            if (claimGroup.PaidType == PAID_TYPE.PAID)
            {
                var claimNumberAndSeqs = claimGroup.ReturnLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber)).GroupBy(x => x.ClaimAndSeqNumber)
                        .Select(x => new { Key = x.Key, ReturnLineItems = x.ToList() }).ToList();

                foreach (var group in claimNumberAndSeqs)
                {
                    var item = new ClaimNumberAndSeqWithAdjustmentType();
                    item.AdjustmentType = AdjustmentType.NORMAL;
                    item.ExplainCode1 = string.Empty;
                    item.ExplainCode2 = string.Empty;
                    item.ExplainCode3 = string.Empty;

                    var lineItem1 = group.ReturnLineItems.ElementAt(0);

                    if (group.ReturnLineItems.Count() == 2)
                    {
                        var lineItem2 = group.ReturnLineItems.ElementAt(1);

                        if (IsAPaidLineContainNegativeApprovedFee(lineItem1) && IsAPaidLineContainPositiveDollarWithExplainCode(lineItem2))
                        {
                            item.LineItem = lineItem2;
                            item.AdjustmentType = AdjustmentType.CHANGE_IN_PAID_AMOUNT;
                            item.DrawbackAmount = lineItem1.ApprovePlusPremiumAmount;
                            item.ApprovedAmount = lineItem2.ApprovePlusPremiumAmount;
                            item.ExplainCode1 = lineItem2.ExplainCode1;
                            item.ExplainCode2 = lineItem2.ExplainCode2;
                            item.ExplainCode3 = lineItem2.ExplainCode3;

                            result.Add(item);
                        }
                        else if (IsAPaidLineContainNegativeApprovedFee(lineItem2) && IsAPaidLineContainPositiveDollarWithExplainCode(lineItem1))
                        {
                            item.LineItem = lineItem1;
                            item.AdjustmentType = AdjustmentType.CHANGE_IN_PAID_AMOUNT;
                            item.DrawbackAmount = lineItem2.ApprovePlusPremiumAmount;
                            item.ApprovedAmount = lineItem1.ApprovePlusPremiumAmount;
                            item.ExplainCode1 = lineItem1.ExplainCode1;
                            item.ExplainCode2 = lineItem2.ExplainCode2;
                            item.ExplainCode3 = lineItem2.ExplainCode3;

                            result.Add(item);
                        }
                    }
                    //else if (group.ReturnLineItems.Count() == 1)
                    //{
                    //    if (IsAPaidLineContainNegativeApprovedFee(lineItem1))
                    //    {
                    //        item.LineItem = lineItem1;
                    //        item.AdjustmentType = AdjustmentType.DRAW_BACK;
                    //        item.DrawbackAmount = lineItem1.ApprovePlusPremiumAmount;
                    //        item.ApprovedAmount = 0;
                    //        item.ExplainCode = string.Empty;
                    //        result.Add(item);
                    //    }
                    //}                   
                }
            }

            return result;
        }

        private bool IsAPaidLineContainNegativeApprovedFee(ReturnLineItem myLine)
        {
            return (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && myLine.ApprovedUnitAmount < 0);           
        }

        private bool IsAPaidLineContainPositiveDollarWithExplainCode(ReturnLineItem myLine)
        {
            return (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && myLine.ApprovedUnitAmount > 0 && !string.IsNullOrEmpty(myLine.ExplainCode1));
        }

        private IEnumerable<ServiceRecord> GenerateServiceRecord(Guid myReturnClaimId, ClaimNumberGroup myClaimNumberGroup)
        {
            //myClaimGroup will only contain either PAID claims OR RETURNED claims

            //ServiceRecord.ClaimAmount = How much in total I will get with Premium
            //ServiceRecord.PaidAmount = How much I will get Paid

            //UnitRecord.UnitAmount = Unit Number * $Fee per Unit
            //UnitRecord.PaidAmount = Unit Amount + Preimum Amount

            var needToCreateNewServiceRecord = false;
            var myNewServices = new List<ServiceRecord>();
            var returnFileUnitRecordList = new List<UnitRecord>();

            var myClaimNumberAndSeqAndAdjustmentList = CheckAdjustmentsInReturn(myClaimNumberGroup);
            
            if (myClaimNumberAndSeqAndAdjustmentList.Any())
            {
                foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList)
                {
                    returnFileUnitRecordList.Add(CreateReturnUnitRecord(myAdjustment.LineItem));
                }
            }
            else
            {
                foreach (var myLine in myClaimNumberGroup.ReturnLineItems.Where(x =>
                                            (x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE) && x.ApprovedUnitAmount >= 0d)) 
                {
                    returnFileUnitRecordList.Add(CreateReturnUnitRecord(myLine));
                }
            }

            var myMatchedClaimNumberServiceRecords = GetMatchServiceRecords(myUserId, myClaimNumberGroup.ClaimNumber);

            if (myMatchedClaimNumberServiceRecords.Any())
            {
                //Only care about Changes In Paid Amount
                //The Drawback adjustment will be taken care from the Rejected Claims code down below
                if (myClaimNumberAndSeqAndAdjustmentList.Any(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                {
                    #region Deal with Adjustment

                    returnFileUnitRecordList.Clear();
                    foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                    {
                        returnFileUnitRecordList.Add(CreateReturnUnitRecord(myAdjustment.LineItem));
                    }

                    //We got Adjustments, need to check previously Paid claims and change the Paid Amount accordingly.

                    var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.ClaimsInId.HasValue && !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                    //Get the most filter ones - Match Last Name (start) And Hospital Number
                    ServiceRecord matchedPaidServiceRecord = paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();

                    if (matchedPaidServiceRecord == null)
                    {
                        //Too restricted, then try either Last Name or Hospital Number
                        matchedPaidServiceRecord = paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();
                    }

                    //if (matchedPaidServiceRecord == null)
                    //{
                    //    //Still Nothing, then just use the matched Claim Number and Rejected
                    //    matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault();
                    //}

                    var foundUnitRecords = new List<UnitRecord>();
                    if (matchedPaidServiceRecord != null)
                    {
                        var adjustmentMessage = string.Empty;

                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord);
                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;

                                foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (returnUnitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = returnUnitRecord.UnitCode.Split(',');
                                    foundExistingUnitRecord.UnitCode = splitCode.First();

                                    adjustmentMessage += splitCode.Last() + " converted to " + splitCode.First() + ". ";
                                }

                                foundUnitRecords.Add(foundExistingUnitRecord);
                            }
                        }

                        if (foundUnitRecords.Any())
                        {
                            matchedPaidServiceRecord.LastModifiedDate = DateTime.UtcNow;
                            matchedPaidServiceRecord.PaidAmount = matchedPaidServiceRecord.UnitRecord.Sum(x => x.PaidAmount);
                            matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                            matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            adjustmentMessage += "System: Payment Adjustments.";
                            adjustmentMessage += " Change In Paid Amount.";
                            adjustmentMessage += $" Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => Math.Abs(x.DrawbackAmount)).ToString("C")}, Approved: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.ApprovedAmount).ToString("C")}";

                            matchedPaidServiceRecord.MessageFromICS = adjustmentMessage;

                            SetServiceRecordStateToModified(matchedPaidServiceRecord);
                            SetUnitRecordListStateToModified(foundUnitRecords);

                            _paidClaimNeeded = true;
                        }
                    }

                    if (matchedPaidServiceRecord == null || !foundUnitRecords.Any())
                    {
                        var adjustmentMessage = string.Empty;

                        var myNewPaidService = new ServiceRecord();
                        myNewPaidService.ServiceRecordId = Guid.NewGuid();

                        SetServiceRecordFieldsFromClaimGroup(myNewPaidService, myClaimNumberGroup);

                        var index = 1;
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            returnUnitRecord.RecordIndex = index;
                            returnUnitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId;

                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                            returnUnitRecord.UnitCode = returnUnitRecord.UnitCode.Split(',').First();

                            adjustmentMessage += splitCode.Last() + " converted to " + splitCode.First() + ". ";

                            myNewPaidService.UnitRecord.Add(returnUnitRecord);

                            index++;
                        }

                        //Paid Claims
                        myNewPaidService.PaidClaimId = myReturnClaimId;
                        myNewPaidService.PaymentApproveDate = DateTime.UtcNow;

                        myNewPaidService.ClaimAmount = myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.DrawbackAmount);
                        myNewPaidService.PaidAmount = myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.ApprovedAmount);

                        adjustmentMessage += "System: Payment Adjustments. ";

                        foreach (var item in myClaimNumberAndSeqAndAdjustmentList.Take(1))
                        {
                            if (item.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT)
                            {
                                adjustmentMessage += " Change In Paid Amount.";
                                adjustmentMessage += $" Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => Math.Abs(x.DrawbackAmount)).ToString("C")}, Approved: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.ApprovedAmount).ToString("C")}";
                            }
                            else if (item.AdjustmentType == AdjustmentType.DRAW_BACK)
                            {
                                adjustmentMessage += " Claim Payment Drawn Back.";
                                adjustmentMessage += $" Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.DrawbackAmount).ToString("C")}";
                            }
                            else if (item.AdjustmentType == AdjustmentType.PAID_FROM_PREVIOUS_REJECTED)
                            {
                                adjustmentMessage += " Getting paid from previously rejected claim.";
                                adjustmentMessage += $" Approved: {myClaimNumberAndSeqAndAdjustmentList.Sum(x => x.ApprovedAmount).ToString("C")}";
                            }
                        }

                        myNewPaidService.MessageFromICS = adjustmentMessage.Trim() + " ";

                        myNewServices.Add(myNewPaidService);
                    }

                    #endregion
                }
                else if (myClaimNumberGroup.PaidType == PAID_TYPE.PAID)
                {
                    #region Deal with Paid

                    //We got Paid claim, we need to check Submitted and Rejected

                    #region Rejected Claims

                    //Getting all the rejected claims
                    var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.ClaimsInId.HasValue && x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue);

                    var matchedRejectedServiceRecords = new List<ServiceRecord>();

                    //Get the most filter ones - Match Last Name (start) And Hospital Number
                    matchedRejectedServiceRecords.AddRange(rejectedServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));

                    if (!matchedRejectedServiceRecords.Any())
                    {
                        //Too restricted, then try either Last Name or Hospital Number
                        matchedRejectedServiceRecords.AddRange(rejectedServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));
                    }

                    //if (!matchedRejectedServiceRecords.Any())
                    //{
                    //    //Still Nothing, then just use the matched Claim Number and Rejected
                    //    matchedRejectedServiceRecords.AddRange(rejectedServiceRecords);
                    //}

                    //Check all the Rejected Claims and see if any of line items are paid
                    foreach (var rejectedServiceRecord in matchedRejectedServiceRecords)
                    {
                        var systemMessage = string.Empty;

                        var foundUnitRecords = new List<UnitRecord>();
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(rejectedServiceRecord.UnitRecord, returnUnitRecord);

                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;

                                foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;

                                if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                    Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                {
                                    systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                }

                                foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (returnUnitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = returnUnitRecord.UnitCode.Split(',');
                                    foundExistingUnitRecord.UnitCode = splitCode.First();

                                    systemMessage += splitCode.Last() + " converted to " + splitCode.First() + ". ";
                                }

                                foundUnitRecords.Add(foundExistingUnitRecord);
                            }
                        }

                        if (rejectedServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                        {
                            //All line items are PAID, return the service record with updated field

                            rejectedServiceRecord.PaidClaimId = myReturnClaimId;
                            rejectedServiceRecord.RejectedClaimId = null;
                            rejectedServiceRecord.PaidAmount = rejectedServiceRecord.UnitRecord.Sum(x => x.PaidAmount);
                            rejectedServiceRecord.PaymentApproveDate = DateTime.UtcNow;
                            rejectedServiceRecord.MessageFromICS += systemMessage;
                            rejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(rejectedServiceRecord);
                            SetUnitRecordListStateToModified(rejectedServiceRecord.UnitRecord);

                            _paidClaimNeeded = true;
                        }
                        else if (foundUnitRecords.Any())
                        {
                            //Different in line items, need to create new Service Record to hold the Paid Line Items and delete the line items from the Submitted Claim
                            //deal with paid claim in BiWeekly Return files
                            var myNewPaidService = new ServiceRecord();

                            CopyServiceRecordFields(rejectedServiceRecord, myNewPaidService, myClaimNumberGroup.ClaimNumber);
                            myNewPaidService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            var index = 1;
                            foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId; //remap the Unit Record to the New Service Record - Paid
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewPaidService.ClaimAmount = foundUnitRecords.Sum(x => x.PaidAmount);
                            myNewPaidService.PaidAmount = myNewPaidService.ClaimAmount;
                            myNewPaidService.MessageFromICS += systemMessage;

                            rejectedServiceRecord.ClaimAmount = rejectedServiceRecord.ClaimAmount - myNewPaidService.ClaimAmount;
                            rejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(rejectedServiceRecord);

                            index = 1;
                            foreach (var unitRecord in rejectedServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewPaidService.PaidClaimId = myReturnClaimId;

                            myNewServices.Add(myNewPaidService);
                        }
                    }

                    #endregion

                    #region Submitted Claim

                    var submittedClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                x.ClaimsInId.HasValue && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted                                           
                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault();

                    if (submittedClaim != null)
                    {
                        var systemMessage = string.Empty;
                        
                        //Check Unit Records to see which ones need to set to Paid from Submitted
                        var myNewPaidService = new ServiceRecord();

                        var foundUnitRecords = new List<UnitRecord>();
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(submittedClaim.UnitRecord, returnUnitRecord);

                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;

                                foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;

                                if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                    Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                {
                                    systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                }

                                foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;

                                if (returnUnitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = returnUnitRecord.UnitCode.Split(',');
                                    foundExistingUnitRecord.UnitCode = splitCode.First();

                                    systemMessage += splitCode.Last() + " converted to " + splitCode.First() + ". ";
                                }

                                foundUnitRecords.Add(foundExistingUnitRecord);
                            }
                        }

                        if (submittedClaim.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                        {
                            //All line items are PAID, return the service record with updated field

                            submittedClaim.PaidClaimId = myReturnClaimId;
                            submittedClaim.PaidAmount = submittedClaim.UnitRecord.Sum(x => x.PaidAmount);
                            submittedClaim.PaymentApproveDate = DateTime.UtcNow;
                            submittedClaim.MessageFromICS += systemMessage;
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(submittedClaim);
                            SetUnitRecordListStateToModified(submittedClaim.UnitRecord);

                            _paidClaimNeeded = true;
                        }
                        else if (foundUnitRecords.Any())
                        {
                            //Different in line items, need to create new Service Record to hold the Piad Line Items and delete the line items from the Submitted Claim
                            //deal with paid claim in BiWeekly Return files

                            CopyServiceRecordFields(submittedClaim, myNewPaidService, myClaimNumberGroup.ClaimNumber);

                            var index = 1;
                            foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId;
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewPaidService.ClaimAmount = foundUnitRecords.Sum(x => x.PaidAmount);
                            myNewPaidService.PaidAmount = myNewPaidService.ClaimAmount;
                            myNewPaidService.MessageFromICS += systemMessage;
                            myNewPaidService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            submittedClaim.ClaimAmount = submittedClaim.ClaimAmount - myNewPaidService.ClaimAmount;
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(submittedClaim);

                            index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewPaidService.PaidClaimId = myReturnClaimId;

                            myNewServices.Add(myNewPaidService);
                        }
                    }

                    #endregion

                    #region Paid Claims

                    //Getting all the rejected claims
                    var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.ClaimsInId.HasValue && !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue && 
                                                x.PaidClaimId != myReturnClaimId);

                    var matchedPaidServiceRecords = new List<ServiceRecord>();

                    //Get the most filter ones - Match Last Name (start) And Hospital Number
                    matchedPaidServiceRecords.AddRange(paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));

                    if (!matchedPaidServiceRecords.Any())
                    {
                        //Too restricted, then try either Last Name or Hospital Number
                        matchedPaidServiceRecords.AddRange(paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));
                    }

                    //if (!matchedPaidServiceRecords.Any())
                    //{
                    //    //Still Nothing, then just use the matched Claim Number and Rejected
                    //    matchedPaidServiceRecords.AddRange(paidServiceRecords);
                    //}

                    //Check all the Paid Claims and see if any of line items are paid
                    foreach (var paidServiceRecord in matchedPaidServiceRecords)
                    {
                        var systemMessage = string.Empty;

                        var foundUnitRecords = new List<UnitRecord>();
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(paidServiceRecord.UnitRecord, returnUnitRecord);

                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;

                                foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;

                                if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                    Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                {
                                    systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                }

                                foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (returnUnitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = returnUnitRecord.UnitCode.Split(',');
                                    foundExistingUnitRecord.UnitCode = splitCode.First();

                                    systemMessage += splitCode.Last() + " converted to " + splitCode.First() + ". ";
                                }

                                foundUnitRecords.Add(foundExistingUnitRecord);
                            }
                        }
                        
                        if (foundUnitRecords.Any())
                        {
                            //Just need to update the Unit Records In Paid Claim
                            paidServiceRecord.PaidClaimId = myReturnClaimId;
                            paidServiceRecord.PaidAmount = paidServiceRecord.UnitRecord.Sum(x => x.PaidAmount);
                            paidServiceRecord.PaymentApproveDate = DateTime.UtcNow;
                            paidServiceRecord.MessageFromICS += systemMessage;
                            paidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(paidServiceRecord);
                            SetUnitRecordListStateToModified(foundUnitRecords);

                            _paidClaimNeeded = true;
                        }
                    }

                    #endregion

                    if (submittedClaim == null && !matchedPaidServiceRecords.Any() && !matchedRejectedServiceRecords.Any())
                    {
                        needToCreateNewServiceRecord = true;
                    }
                    
                    #endregion
                }
                else if (myClaimNumberGroup.PaidType == PAID_TYPE.RETURNED_CLAIMS)
                {
                    #region Deal with Rejected

                    #region Submitted Claim

                    //We got Rejected Claim, we will check Submitted Claim
                    var submittedClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                                x.ClaimsInId.HasValue && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted                                           
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();

                    if (submittedClaim != null)
                    {
                        #region Found Submitted Claim

                        //Check Unit Records to see which ones need to set to Rejected

                        var foundUnitRecords = new List<UnitRecord>();
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(submittedClaim.UnitRecord, returnUnitRecord);
                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;
                                foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;

                                foundUnitRecords.Add(foundExistingUnitRecord);
                            }
                        }

                        if (submittedClaim.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                        {
                            //All line items are rejected, return the service record with updated field
                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                            {
                                submittedClaim.MessageFromICS = myClaimNumberGroup.MSBComment;
                            }

                            submittedClaim.RejectedClaimId = myReturnClaimId;
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(submittedClaim);
                            SetUnitRecordListStateToModified(submittedClaim.UnitRecord);

                            _rejectedClaimNeeded = true;
                        }
                        else if (foundUnitRecords.Any())
                        {
                            var previousRejectedServiceRecord = myMatchedClaimNumberServiceRecords.Where(x =>
                                                        x.ClaimsInId.HasValue && x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Rejected
                                                        x.ServiceDate.Date == myClaimNumberGroup.FirstLineItem.ServiceDate.Date && x.RejectedClaimId != myReturnClaimId &&
                                                        x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                        x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                                        .FirstOrDefault();

                            if (previousRejectedServiceRecord != null && (previousRejectedServiceRecord.UnitRecord.Count() + foundUnitRecords.Count()) < 8)
                            {
                                previousRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                SetServiceRecordStateToModified(previousRejectedServiceRecord);

                                foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                {
                                    unitRecord.ServiceRecordId = previousRejectedServiceRecord.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecord);
                                }

                                previousRejectedServiceRecord.ClaimAmount += foundUnitRecords.Sum(x => x.PaidAmount);
                                previousRejectedServiceRecord.LastModifiedDate = DateTime.UtcNow;
                                previousRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                {
                                    previousRejectedServiceRecord.MessageFromICS = myClaimNumberGroup.MSBComment;
                                }

                                submittedClaim.ClaimAmount -= foundUnitRecords.Sum(x => x.PaidAmount);
                                submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToModified(submittedClaim);

                                var index = 1;
                                foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.RecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                _rejectedClaimNeeded = true;
                            }
                            else
                            {
                                //Different in line items, need to create new Service Record to hold the Rejected Line Items and delete the line items from the Submitted Claim
                                //deal with rejected claim in Daily Return or BiWeekly Return files

                                var myNewRejectedService = new ServiceRecord();

                                CopyServiceRecordFields(submittedClaim, myNewRejectedService, myClaimNumberGroup.ClaimNumber);

                                var index = 1;
                                foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                {
                                    unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                myNewRejectedService.ClaimAmount = foundUnitRecords.Sum(x => x.PaidAmount);
                                myNewRejectedService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                submittedClaim.ClaimAmount = submittedClaim.ClaimAmount - myNewRejectedService.ClaimAmount;
                                submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                SetServiceRecordStateToModified(submittedClaim);

                                index = 1;
                                foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.RecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                {
                                    myNewRejectedService.MessageFromICS = myClaimNumberGroup.MSBComment;
                                }

                                myNewRejectedService.RejectedClaimId = myReturnClaimId;

                                myNewServices.Add(myNewRejectedService);
                            }
                        }

                        #endregion
                    }

                    #endregion

                    #region Paid Claim - Draw Back

                    var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.ClaimsInId.HasValue && !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                    var matchPaidServiceRecords = new List<ServiceRecord>();

                    //Get the most filter ones - Match Last Name (start) And Hospital Number
                    matchPaidServiceRecords.AddRange(paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));

                    if (!matchPaidServiceRecords.Any())
                    {
                        //Too restricted, then try either Last Name or Hospital Number
                        matchPaidServiceRecords.AddRange(paidServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));
                    }

                    //if (!matchPaidServiceRecords.Any())
                    //{
                    //    //Still Nothing, then just use the matched Claim Number and Rejected
                    //    matchPaidServiceRecords.AddRange(paidServiceRecords);
                    //}

                    if (matchPaidServiceRecords.Any())
                    {
                        #region Found Paid Service Records

                        foreach (var matchedPaidServiceRecord in matchPaidServiceRecords)
                        {
                            var foundUnitRecords = new List<UnitRecord>();

                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                    foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                    foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;
                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                }
                            }

                            if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                //Convert Paid to Rejected - If all lines items are there        
                                matchedPaidServiceRecord.PaidAmount = 0d;

                                matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back.";

                                matchedPaidServiceRecord.RejectedClaimId = myReturnClaimId;
                                matchedPaidServiceRecord.PaidClaimId = null;
                                matchedPaidServiceRecord.PaymentApproveDate = null;
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                {
                                    matchedPaidServiceRecord.MessageFromICS = myClaimNumberGroup.MSBComment;
                                }

                                SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                SetUnitRecordListStateToModified(foundUnitRecords);

                                _rejectedClaimNeeded = true;
                            }
                            else if (foundUnitRecords.Any())
                            {
                                var previousRejectedServiceRecord = myMatchedClaimNumberServiceRecords.Where(x =>
                                                            x.ClaimsInId.HasValue && x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Rejected
                                                            x.ServiceDate.Date == myClaimNumberGroup.FirstLineItem.ServiceDate.Date && x.RejectedClaimId != myReturnClaimId &&
                                                            x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                            x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase))
                                                            .FirstOrDefault();

                                if (previousRejectedServiceRecord != null && (previousRejectedServiceRecord.UnitRecord.Count() + foundUnitRecords.Count()) < 8)
                                {
                                    previousRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                    previousRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(previousRejectedServiceRecord);

                                    foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.ServiceRecordId = previousRejectedServiceRecord.ServiceRecordId;
                                        SetUnitRecordStateToModified(unitRecord);
                                    }

                                    previousRejectedServiceRecord.ClaimAmount += foundUnitRecords.Sum(x => x.PaidAmount);
                                    previousRejectedServiceRecord.LastModifiedDate = DateTime.UtcNow;

                                    matchedPaidServiceRecord.ClaimAmount = matchedPaidServiceRecord.ClaimAmount - foundUnitRecords.Sum(x => x.PaidAmount);
                                    matchedPaidServiceRecord.PaidAmount = matchedPaidServiceRecord.PaidAmount - foundUnitRecords.Sum(x => x.PaidAmount);
                                    matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back.";
                                    matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                    {
                                        matchedPaidServiceRecord.MessageFromICS += System.Environment.NewLine + myClaimNumberGroup.MSBComment;
                                    }

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    _rejectedClaimNeeded = true;
                                }
                                else
                                {
                                    var justAddedRejectedClaim = myNewServices.Where(x => x.RejectedClaimId == myReturnClaimId).FirstOrDefault();
                                    if (justAddedRejectedClaim != null && (justAddedRejectedClaim.UnitRecord.Count() + foundUnitRecords.Count()) < 8)
                                    {
                                        foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                        {
                                            unitRecord.ServiceRecordId = justAddedRejectedClaim.ServiceRecordId;
                                            SetUnitRecordStateToModified(unitRecord);
                                        }

                                        justAddedRejectedClaim.ClaimAmount += foundUnitRecords.Sum(x => x.PaidAmount);
                                        justAddedRejectedClaim.LastModifiedDate = DateTime.UtcNow;
                                        justAddedRejectedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        matchedPaidServiceRecord.ClaimAmount = matchedPaidServiceRecord.ClaimAmount - foundUnitRecords.Sum(x => x.PaidAmount);
                                        matchedPaidServiceRecord.PaidAmount = matchedPaidServiceRecord.PaidAmount - foundUnitRecords.Sum(x => x.PaidAmount);
                                        matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back.";
                                        matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                        {
                                            matchedPaidServiceRecord.MessageFromICS += System.Environment.NewLine + myClaimNumberGroup.MSBComment;
                                        }

                                        SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                        var index = 1;
                                        foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }
                                    }
                                    else
                                    {
                                        #region Create New Rejected Claim

                                        var myNewRejectedService = new ServiceRecord();

                                        CopyServiceRecordFields(matchedPaidServiceRecord, myNewRejectedService, myClaimNumberGroup.ClaimNumber);

                                        var index = 1;
                                        foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                        {
                                            unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        myNewRejectedService.ClaimAmount = foundUnitRecords.Sum(x => x.PaidAmount);
                                        myNewRejectedService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                        {
                                            myNewRejectedService.MessageFromICS = myClaimNumberGroup.MSBComment;
                                        }

                                        matchedPaidServiceRecord.ClaimAmount = matchedPaidServiceRecord.ClaimAmount - myNewRejectedService.ClaimAmount;
                                        matchedPaidServiceRecord.PaidAmount = matchedPaidServiceRecord.PaidAmount - myNewRejectedService.ClaimAmount;
                                        matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back.";
                                        matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                        SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                        index = 1;
                                        foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        myNewRejectedService.RejectedClaimId = myReturnClaimId;

                                        myNewServices.Add(myNewRejectedService);

                                        #endregion
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    #endregion

                    #region Rejected Claims - Update Explain Codes

                    //Getting all the rejected claims
                    var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue 
                                                && x.RejectedClaimId.HasValue && x.RejectedClaimId != myReturnClaimId);

                    var matchedRejectedServiceRecords = new List<ServiceRecord>();

                    //Get the most filter ones - Match Last Name (start) And Hospital Number
                    matchedRejectedServiceRecords.AddRange(rejectedServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));

                    if (!matchedRejectedServiceRecords.Any())
                    {
                        //Too restricted, then try either Last Name or Hospital Number
                        matchedRejectedServiceRecords.AddRange(rejectedServiceRecords.Where(x =>
                                                x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                x.PatientLastName.StartsWith(myClaimNumberGroup.ClaimPatientInfo.LastName, StringComparison.OrdinalIgnoreCase)));
                    }

                    //if (!matchedRejectedServiceRecords.Any())
                    //{
                    //    //Still Nothing, then just use the matched Claim Number and Rejected
                    //    matchedRejectedServiceRecords.AddRange(rejectedServiceRecords);
                    //}

                    //Check all the Rejected Claims and see if any of line items need to update the explain code
                    foreach (var rejectedServiceRecord in matchedRejectedServiceRecords)
                    {
                        foreach (var returnUnitRecord in returnFileUnitRecordList)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(rejectedServiceRecord.UnitRecord, returnUnitRecord);

                            if (foundExistingUnitRecord != null)
                            {
                                foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;

                                SetUnitRecordStateToModified(foundExistingUnitRecord);

                                _rejectedClaimNeeded = true;
                            }
                        }

                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                        {
                            rejectedServiceRecord.MessageFromICS = myClaimNumberGroup.MSBComment;
                            rejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(rejectedServiceRecord);
                            _rejectedClaimNeeded = true;
                        }
                    }

                    #endregion

                    if (submittedClaim == null && !matchPaidServiceRecords.Any() && !matchedRejectedServiceRecords.Any())
                    {
                        needToCreateNewServiceRecord = true;
                    }
                    
                    #endregion
                }
            }
            else
            {
                needToCreateNewServiceRecord = true;
            }
            
            if (needToCreateNewServiceRecord)
            {
                var myNewService = new ServiceRecord();
                myNewService.ServiceRecordId = Guid.NewGuid();
                myNewService.ClaimAmount = returnFileUnitRecordList.Sum(x => x.PaidAmount);

                SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                if (myClaimNumberGroup.PaidType == PAID_TYPE.PAID)
                {
                    myNewService.PaidClaimId = myReturnClaimId;
                    myNewService.PaidAmount = myNewService.ClaimAmount;
                    myNewService.PaymentApproveDate = DateTime.UtcNow;
                }
                else if (myClaimNumberGroup.PaidType == PAID_TYPE.RETURNED_CLAIMS)
                {
                    myNewService.RejectedClaimId = myReturnClaimId;
                    if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                    {
                        myNewService.MessageFromICS = myClaimNumberGroup.MSBComment;
                    }
                }

                foreach (var unitRecord in returnFileUnitRecordList)
                {
                    unitRecord.ServiceRecordId = myNewService.ServiceRecordId;

                    if (unitRecord.UnitCode.Contains(","))
                    {
                        var splitCode = unitRecord.UnitCode.Split(',');
                        unitRecord.UnitCode = splitCode.First();
                    }

                    myNewService.UnitRecord.Add(unitRecord);
                }

                myNewServices.Add(myNewService);
            }
            
            return myNewServices;
        }

        private UnitRecord GetMatchedUnitRecord(IEnumerable<UnitRecord> existingUnitRecords, UnitRecord returnUnitRecord)
        {
            List<UnitRecord> foundCodeMatchedUnitRecords = new List<UnitRecord>();

            var unitCodeSplitList = returnUnitRecord.UnitCode.Split(',');
            if (unitCodeSplitList.Length == 1)
            {
                foundCodeMatchedUnitRecords.AddRange(existingUnitRecords.Where(x => x.UnitCode == returnUnitRecord.UnitCode).OrderBy(x => x.RecordIndex));
            }
            else if (unitCodeSplitList.Length == 2)
            {
                foundCodeMatchedUnitRecords.AddRange(existingUnitRecords.Where(x => x.UnitCode == unitCodeSplitList.Last()).OrderBy(x => x.RecordIndex));
            }

            if (foundCodeMatchedUnitRecords.Any())
            {
                var foundIndexMatchedUnitRecord = foundCodeMatchedUnitRecords.FirstOrDefault(x =>
                                                        x.RecordIndex == returnUnitRecord.SubmittedRecordIndex ||
                                                        x.RecordIndex == returnUnitRecord.RecordIndex);
                if (foundIndexMatchedUnitRecord == null)
                {
                    return foundCodeMatchedUnitRecords.FirstOrDefault();
                }
                else
                {
                    return foundIndexMatchedUnitRecord;
                }
            }

            return null;
        }

        private void SetServiceRecordFieldsFromClaimGroup(ServiceRecord myNewService, ClaimNumberGroup myClaimNumberGroup)
        {
            myNewService.CreatedDate = DateTime.UtcNow;
            myNewService.UserId = myUserId;
            myNewService.ClaimNumber = myClaimNumberGroup.ClaimNumber; //Use the claim number in return, set in the above
            myNewService.RollOverNumber = GetCurrentRollOverNumber(myUserId);
            myNewService.PatientLastName = myClaimNumberGroup.ClaimPatientInfo.LastName;
            myNewService.PatientFirstName = myClaimNumberGroup.ClaimPatientInfo.FirstName;
            myNewService.HospitalNumber = myClaimNumberGroup.ClaimPatientInfo.HospitalNumber;
            myNewService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

            myNewService.Province = myClaimNumberGroup.ClaimPatientInfo.Province;

            myNewService.ServiceDate = myClaimNumberGroup.FirstLineItem.ServiceDate;

            myNewService.Sex = myClaimNumberGroup.ClaimPatientInfo.Sex;
            if (string.IsNullOrEmpty(myNewService.Sex))
            {
                myNewService.Sex = "F";
            }

            myNewService.DateOfBirth = DateTime.Now;

            if (myClaimNumberGroup.ClaimPatientInfo.BirthDate != null && myClaimNumberGroup.ClaimPatientInfo.BirthDate != DateTime.MinValue)
            {
                myNewService.DateOfBirth = myClaimNumberGroup.ClaimPatientInfo.BirthDate;
            }

            if (!string.IsNullOrEmpty(myClaimNumberGroup.FirstLineItem.ReferringDoctorNumber))
            {
                myNewService.ReferringDoctorNumber = myClaimNumberGroup.FirstLineItem.ReferringDoctorNumber;
            }

            if (myClaimNumberGroup.ReturnLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE))
            {
                myNewService.DischargeDate = myClaimNumberGroup.ReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                    .OrderBy(x => x.ClaimAndSeqNumber).FirstOrDefault().LastServiceDate;
            }
        }

        private PatientInfo GetPatientInfo(string myLine, RETURNED_RECORD_TYPE recordType)
        {
            var result = new PatientInfo();
            result.Province = "SK";

            if (recordType == RETURNED_RECORD_TYPE.PAID) //Paid Line
            {
                result.HospitalNumber = myLine.Substring(35, 9).Trim();

                if (string.IsNullOrEmpty(result.HospitalNumber))
                {
                    result.HospitalNumber = myLine.Substring(133, 12).Trim();
                    result.Province = myLine.Substring(131, 2).Trim();
                }

                var patientName = GetPatientName(myLine.Substring(15, 19).Trim());

                result.FirstName = patientName.Item1;
                result.LastName = patientName.Item2;
            }
            else if (recordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || recordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE)
            {
                result.HospitalNumber = myLine.Substring(12, 9).Trim();
                
                var patientName = GetPatientName(myLine.Substring(26, 25).Trim());

                result.FirstName = patientName.Item1;
                result.LastName = patientName.Item2;

                result.Sex = myLine.Substring(25, 1).Trim();

                result.BirthDate = new DateTime(
                                int.Parse(DateTime.Now.Year.ToString().Substring(0, 2) + myLine.Substring(23, 2)),
                                int.Parse(myLine.Substring(21, 2)),
                                1);
                if (result.BirthDate > DateTime.Now)
                {
                    result.BirthDate = result.BirthDate.AddYears(-100);
                }
            }
            else if (recordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE)
            {
                result.Province = myLine.Substring(21, 2).Trim();
                result.HospitalNumber = myLine.Substring(51, 12).Trim();
                result.LastName = myLine.Substring(23, 18).Trim();
                result.FirstName = myLine.Substring(41, 9).Trim();
            }
            else if (recordType == RETURNED_RECORD_TYPE.COMMENT)
            {
                result.HospitalNumber = myLine.Substring(12, 9).Trim();
            }

            return result;
        }
                
        /// <summary>
        /// Get Patient Name
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns>
        /// Item1 = First Name
        /// Item2 = Last Name
        /// </returns>
        private Tuple<string, string> GetPatientName(string fullName)
        {
            var myName = fullName.Split(',');
            var lastName = myName[0].Trim();
            var firstName = "Please fill in!";

            if (myName.Count() > 1)
            {
                var myTemp = myName[1].Trim();
                if (!string.IsNullOrEmpty(myTemp))
                {
                    firstName = myTemp;
                }
            }

            return Tuple.Create(firstName, lastName);
        }

        private UnitRecord CreateReturnUnitRecord(ReturnLineItem myLine)
        {
            UnitRecord myUnitRecord = new UnitRecord();
            myUnitRecord.UnitRecordId = Guid.NewGuid();

            if (!string.IsNullOrEmpty(myLine.ExplainCode1))
            {
                myUnitRecord.ExplainCode = myLine.ExplainCode1;
            }

            if (!string.IsNullOrEmpty(myLine.ExplainCode2))
            {
                myUnitRecord.ExplainCode2 = myLine.ExplainCode2;
            }

            if (!string.IsNullOrEmpty(myLine.ExplainCode3))
            {
                myUnitRecord.ExplainCode3 = myLine.ExplainCode3;
            }

            myUnitRecord.RunCode = myLine.RunCode;

            if (myLine.PaidType == PAID_TYPE.PAID)
            {
                myUnitRecord.UnitNumber = myLine.ApprovedUnitNumber;

                if (myLine.IsUnitCodeSubmittedEqualApproved)
                {
                    myUnitRecord.UnitCode = myLine.ApprovedUnitCode;
                }
                else
                {
                    myUnitRecord.UnitCode = myLine.ApprovedUnitCode + "," + myLine.SubmittedUnitCode;
                }

                //Unit Amount is the approved amount
                myUnitRecord.UnitAmount = myLine.ApprovedUnitAmount;

                myUnitRecord.SubmittedAmount = myLine.SubmittedUnitAmount;

                //Use Approve Amount + Premium Amount as Paid
                myUnitRecord.PaidAmount = myLine.ApprovePlusPremiumAmount;

                myUnitRecord.ProgramPayment = myLine.ProgramPayment;

                myUnitRecord.UnitPremiumCode = myLine.ApprovedLocationOfService;
            }
            else
            {
                //Return Claims
                myUnitRecord.UnitCode = myLine.SubmittedUnitCode;
                myUnitRecord.UnitNumber = myLine.SubmittedUnitNumber;
                myUnitRecord.UnitAmount = myLine.SubmittedUnitAmount;

                if (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE)
                {
                    myUnitRecord.UnitPremiumCode = myLine.SubmittedLocationOfService;
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount(myUnitRecord.UnitPremiumCode, myUnitRecord.UnitAmount);
                }
                else if (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                {
                    //Returned record
                    myUnitRecord.UnitPremiumCode = "2";
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount("2", myUnitRecord.UnitAmount);
                }
                else
                {
                    myUnitRecord.UnitPremiumCode = "2";
                }
            }

            myUnitRecord.RecordIndex = myLine.SeqNumber + 1;
            myUnitRecord.SubmittedRecordIndex = myLine.SeqNumber + 1;

            myUnitRecord.DiagCode = myDiagCode;
            if (!string.IsNullOrEmpty(myLine.DiagnosticCode))
            {
                //Diag code is in the line
                myUnitRecord.DiagCode = myLine.DiagnosticCode;
            }

            return myUnitRecord;
        }
        
        private double GetTotalWithPremiumAmount(string locationOfService, double unitAmount)
        {
            if (locationOfService.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                return unitAmount * 1.5;
            }
            else if (locationOfService.Equals("k", StringComparison.OrdinalIgnoreCase))
            {
                return unitAmount * 2.0;
            }

            return unitAmount;
        }
        
        private IList<ServiceRecord> GetMatchServiceRecords(Guid userId, int claimNumber)
        {
            return myContext.ServiceRecord.Include("UnitRecord").Where(x => x.ClaimNumber == claimNumber && x.UserId == userId)
                                        .OrderByDescending(x => x.CreatedDate).ToList();
        }

        public int GetCurrentRollOverNumber(Guid userId)
        {
            return myContext.ServiceRecord.Where(x => x.UserId == userId).Select(x => x.RollOverNumber).Max();
        }

        public NextNumberModel GetNextClaimNumber(Guid userId)
        {
            var model = new NextNumberModel();

            try
            {
                model.RollOverNumber = myContext.ServiceRecord.Where(x => x.UserId == userId).Select(x => x.RollOverNumber).Max();
                model.NextClaimNumber = myContext.ServiceRecord.Where(x => x.UserId == userId && x.RollOverNumber == model.RollOverNumber).Select(x => x.ClaimNumber).Max();
            }
            catch
            { }

            if (model.NextClaimNumber == 0)
            {
                model.NextClaimNumber = 10000;
            }
            else
            {
                model.NextClaimNumber++;
                if (model.NextClaimNumber > 99999)
                {
                    model.RollOverNumber++;
                    model.NextClaimNumber = 10000;
                }
            }

            return model;
        }

        private IEnumerable<UnitRecord> GetUnitRecords(Guid serviceRecordId)
        {
            return myContext.UnitRecord.Where(x => x.ServiceRecordId == serviceRecordId).ToList();
        }

        private void DeleteServiceRecord(ServiceRecord serviceRecord)
        {
            myContext.Entry(serviceRecord).State = System.Data.Entity.EntityState.Deleted;
        }

        private void DeleteUnitRecord(UnitRecord unitRecord)
        {
            myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
        }

        private void DeleteUnitRecords(IEnumerable<UnitRecord> unitRecords)
        {
            foreach (var item in unitRecords)
            {
                myContext.Entry(item).State = System.Data.Entity.EntityState.Deleted;
            }
        }

        private void SetServiceRecordStateToModified(ServiceRecord serviceRecord)
        {
            myContext.Entry(serviceRecord).State = System.Data.Entity.EntityState.Modified;
        }

        private void SetUnitRecordStateToModified(UnitRecord unitRecord)
        {
            myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
        }

        private void SetUnitRecordListStateToModified(IEnumerable<UnitRecord> unitRecords)
        {
            foreach (var unitRecord in unitRecords)
            {
                myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
            }
        }

        private void GetTotalAmountsFromTotalLines(ClaimsInReturn myClaimsInReturn)
        {
            if (myTotalLines.Any())
            {
                var groupTotalLines = myTotalLines.Where(x => x.IsTotal).GroupBy(x => new { x.FeeSubmitted, x.PremiumAmount, x.ProgramAmount })
                        .Select(x => new { Key = x.Key, TotalLines = x.ToList() });
                foreach (var myGroup in groupTotalLines)
                {
                    var myTotal = myGroup.TotalLines.OrderByDescending(x => x.LineNumber).FirstOrDefault();
                    
                    try
                    {
                        myClaimsInReturn.TotalSubmitted += myTotal.FeeSubmitted;
                        myClaimsInReturn.TotalApproved += myTotal.FeeApproved;
                        myClaimsInReturn.TotalPremiumAmount += myTotal.PremiumAmount;
                        myClaimsInReturn.TotalProgramAmount += myTotal.ProgramAmount;
                        myClaimsInReturn.TotalPaidAmount += myTotal.PaidAmount;
                        myClaimsInReturn.RunCode = myTotal.RunCode;
                    }
                    catch
                    {
                    }
                }

                myClaimsInReturn.OtherFeeAndPayment = myTotalLines.Where(x => !x.IsTotal).Sum(x => x.PaidAmount);
            }
        }

        private void CopyServiceRecordFields(ServiceRecord myOldService, ServiceRecord myNewService, int claimNumber)
        {
            myNewService.ClaimNumber = claimNumber;
            myNewService.RollOverNumber = myOldService.RollOverNumber;
            myNewService.DateOfBirth = myOldService.DateOfBirth;
            myNewService.ReferringDoctorNumber = myOldService.ReferringDoctorNumber;
            myNewService.ServiceDate = myOldService.ServiceDate;
            myNewService.DischargeDate = myOldService.DischargeDate;
            myNewService.ServiceEndTime = myOldService.ServiceEndTime;
            myNewService.ServiceStartTime = myOldService.ServiceStartTime;
            myNewService.Sex = myOldService.Sex;
            myNewService.ClaimsInId = myOldService.ClaimsInId;
            myNewService.PatientLastName = myOldService.PatientLastName;
            myNewService.PatientFirstName = myOldService.PatientFirstName;
            myNewService.Province = myOldService.Province;
            myNewService.ClaimAmount = myOldService.ClaimAmount;
            myNewService.HospitalNumber = myOldService.HospitalNumber;
            myNewService.Comment = myOldService.Comment;
            myNewService.Notes = myOldService.Notes;
            myNewService.FacilityNumber = myOldService.FacilityNumber;
            myNewService.ServiceLocation = myOldService.ServiceLocation;
            myNewService.CreatedDate = DateTime.UtcNow;
            myNewService.UserId = myOldService.UserId;
            myNewService.ServiceRecordId = Guid.NewGuid();
        }

        public ReturnModel InitialParseResult
        {
            get
            {
                return myReturnModel;
            }
        }

        public IEnumerable<ClaimsInReturn> GenerateReturnClaims(ReturnFileType returnFileType, string returnFileName)
        {
            var result = new List<ClaimsInReturn>();

            foreach (var returnContent in myReturnContents)
            {
                #region Create Claim return

                ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                myClaimsInReturn.ReturnFooter = string.Empty;
                myClaimsInReturn.ReturnFileType = (int)returnFileType;
                myClaimsInReturn.ReturnFileName = returnFileName;
                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.ReturnFileDate = ReturnRetrivalJob.GetFileDateTime(returnFileName);
                myClaimsInReturn.TotalApproved = 0;
                myClaimsInReturn.TotalSubmitted = 0;
                myClaimsInReturn.TotalPaidAmount = 0;
                myClaimsInReturn.RunCode = string.Empty;

                GetTotalAmountsFromTotalLines(myClaimsInReturn);

                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.UserId = myUserId;
                myClaimsInReturn.Content = myReturnContentForStorage;

                RejectedClaim myRejectedClaim = new RejectedClaim();
                myRejectedClaim.RejectedClaimId = Guid.NewGuid();
                myRejectedClaim.CreatedDate = DateTime.UtcNow;
                myRejectedClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;
                
                if (returnContent.ReturnClaimItems.Count() > 0)
                {
                    GenerateReturnClaimSerivceRecords(myRejectedClaim, returnFileType, returnContent.ReturnClaimItems);
                }

                if (myRejectedClaim.ServiceRecord.Count() > 0 || _rejectedClaimNeeded)
                {
                    myClaimsInReturn.RejectedClaim.Add(myRejectedClaim);
                }

                PaidClaim myPaidClaim = new PaidClaim();
                myPaidClaim.PaidClaimId = Guid.NewGuid();
                myPaidClaim.CreatedDate = DateTime.UtcNow;
                myPaidClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;

                if (returnContent.PaidItems.Count() > 0)
                {
                    GeneratePaidClaimSerivceRecords(myPaidClaim, returnContent.PaidItems);
                }

                if (myPaidClaim.ServiceRecord.Count() > 0 || _paidClaimNeeded)
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
    }

    public class TotalLine
    {
        public int LineNumber { get; set; }

        public bool IsTotal { get; set; }

        public string Line { get; set; }

        public double FeeSubmitted { get; set; }

        public double FeeApproved { get; set; }

        public double PremiumAmount { get; set; }

        public double ProgramAmount { get; set; }

        public double PaidAmount { get; set; }

        public string RunCode { get; set; }
    }
    
    public class PatientInfo
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Province { get; set; }

        public string HospitalNumber { get; set; }

        public DateTime BirthDate { get; set; }

        public string Sex { get; set;  }
    }

    public class ReturnLineItem
    {
        public int ClaimNumber { get; set; }

        public string ClaimAndSeqNumber
        {
            get
            {
                if (PaidType == PAID_TYPE.PAID || ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE)
                {
                    return ClaimNumber + SeqNumber.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public int SeqNumber { get; set;}
        
        public string ClinicNumber { get; set; }

        public string LineContent { get; set; }

        public PAID_TYPE PaidType { get; set; }

        public RETURNED_RECORD_TYPE ReturnedRecordType { get; set; }

        public DateTime ServiceDate { get; set; }

        public DateTime LastServiceDate { get; set; }

        public PatientInfo PatientInfo { get; set; }

        public string DiagnosticCode { get; set; }

        public string ApprovedUnitCode { get; set; }

        public int ApprovedUnitNumber { get; set; }

        public double ApprovedUnitAmount { get; set; }

        public string ApprovedLocationOfService { get; set; }

        public string SubmittedUnitCode { get; set; }

        public int SubmittedUnitNumber { get; set; }

        public double SubmittedUnitAmount { get; set; }

        public string SubmittedLocationOfService { get; set; }

        public string ExplainCode1 { get; set; }

        public string ExplainCode2 { get; set; }

        public string ExplainCode3 { get; set; }

        public string RunCode { get; set; }

        public double PremiumAmount { get; set; }

        public double ProgramPayment { get; set; }

        public double ApprovePlusPremiumAmount
        {
            get
            {
                return ApprovedUnitAmount + PremiumAmount;
            }
        }

        public string ReferringDoctorNumber { get; set; }

        public string CommentLineNumber { get; set; }

        public string Comment { get; set; }

        public bool IsUnitCodeSubmittedEqualApproved
        {
            get
            {
                return this.SubmittedUnitCode.Equals(this.ApprovedUnitCode, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string CPSClaimNumber { get; set; }
    }

    public class ClaimNumberGroup
    {
        public int ClaimNumber { get; set; }

        public IList<ReturnLineItem> ReturnLineItems { get; set;  }

        public PAID_TYPE PaidType { get; set; }
        
        public string[] ClaimLines
        {
            get
            {
                return this.ReturnLineItems.Select(x => x.LineContent).ToArray();
            }
        }

        public ReturnLineItem FirstLineItem { get; set; }

        public PatientInfo ClaimPatientInfo { get; set; }

        public string MSBComment { get; set; }

        public string CPSClaimNumber 
        { 
            get
            {
                return FirstLineItem.CPSClaimNumber;
            }
        }
    }

    public class ReturnContent
    {
        public decimal TotalApproved { get; set; }

        public decimal TotalSubmitted { get; set; }

        public IList<ClaimNumberGroup> PaidItems { get; set; }

        public IList<ClaimNumberGroup> ReturnClaimItems { get; set; }

        public string Footer { get; set; }
    }

    public enum AdjustmentType
    {
        DRAW_BACK,
        CHANGE_IN_PAID_AMOUNT,
        PAID_FROM_PREVIOUS_REJECTED,
        NORMAL
    }

    public enum PAID_TYPE
    {
        PAID,
        RETURNED_CLAIMS,
        PENDING_CLAIMS
    }
    
    public enum RETURNED_RECORD_TYPE
    {
        VISIT_PROCEDURE = 0,
        HOSPITAL_CARE = 1,
        OUT_OF_PROVINCE = 2,
        COMMENT = 3,
        PAID = 4
    }   

    public class ClaimNumberAndSeqWithAdjustmentType
    {
        public ReturnLineItem LineItem { get; set; }

        public AdjustmentType AdjustmentType { get; set; }

        public double ApprovedAmount { get; set; }

        public double DrawbackAmount { get; set;  }

        public string ExplainCode1 { get; set; }

        public string ExplainCode2 { get; set; }

        public string ExplainCode3 { get; set; }
    }
}
