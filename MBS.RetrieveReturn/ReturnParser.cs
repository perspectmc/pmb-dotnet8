using MBS.Common;
using MBS.DataCache;
using MBS.DomainModel;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MBS.RetrieveReturn
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
        private string myServiceLocation;

        private char myBreak = '\n';

        private IList<ReturnContent> myReturnContents = new List<ReturnContent>();

        private List<ClaimsReturnPaymentSummary> myTotalLines = new List<ClaimsReturnPaymentSummary>();

        private Guid myUserId = new Guid();

        private ReturnModel myReturnModel = new ReturnModel();

        private MedicalBillingSystemEntities myContext;

        private int _timeZoneOffset = -6;

        private bool _rejectedClaimNeeded = false;

        private bool _paidClaimNeeded = false;

        private readonly string[] _premiumLocationOfService = new [] { "B", "K", "F" };

        public ReturnParser(string myReturnContent, ReturnFileType returnFileType, UserProfiles userProfile, string previousReturnContent, MedicalBillingSystemEntities context)
        {
            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            myDiagCode = userProfile.DiagnosticCode;
            myServiceLocation = userProfile.DefaultServiceLocation;

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

                            //Deal with Payment Draw Back Adjustment claims
                            //To get the explain code from Rejected lines and put them in the Paid Claims and removed any return claims based on the CPS Claims, no need to process them
                            foreach(var returnContent in myReturnContents)
                            {
                                foreach(var paidItem in returnContent.PaidItems)
                                {
                                    if (paidItem.ReturnLineItems.Any(x => x.ApprovedUnitAmount < 0 && x.ApprovePlusPremiumAmount < 0))
                                    {
                                        var correspondingRejectedClaimLines = myContents.Where(x => (x.StartsWith("50") || x.StartsWith("57")) && x.EndsWith(paidItem.CPSClaimNumber));
                                        if (correspondingRejectedClaimLines.Any())
                                        {
                                            var convertedCorrespondingRejectedClaimLines = correspondingRejectedClaimLines.Select(x => CreateReturnLineItem(x));
                                            foreach (var returnLineItem in paidItem.ReturnLineItems)
                                            {
                                                var found = convertedCorrespondingRejectedClaimLines.FirstOrDefault(x => x.SeqNumber == returnLineItem.SeqNumber && x.SubmittedUnitCode == returnLineItem.SubmittedUnitCode);
                                                if (found != null)
                                                {
                                                    returnLineItem.ExplainCode1 = found.ExplainCode1;
                                                    returnLineItem.ExplainCode2 = found.ExplainCode2;
                                                    returnLineItem.ExplainCode3 = found.ExplainCode3;
                                                }
                                            }
                                        }

                                        var returnClaimsToRemoved = returnContent.ReturnClaimItems.FirstOrDefault(x => x.CPSClaimNumber == paidItem.CPSClaimNumber);
                                        if (returnClaimsToRemoved != null)
                                        {
                                            returnContent.ReturnClaimItems.Remove(returnClaimsToRemoved);                                       
                                        }
                                    }
                                }                              

                            }

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
            var messageLineStart = myDoctorNumber.PadLeft(4, '0') + myClinicNumber.PadLeft(3, '0');
            var messageLinePattern1 = messageLineStart + "     M";
            var messageLinePattern2 = messageLineStart + "99999M";
            foreach (var line in myLines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    doctorNumberIndex = line.IndexOf(myDoctorNumber);
                    if (doctorNumberIndex == 1 || doctorNumberIndex == 2)
                    {
                        if (!line.StartsWith("10" + myDoctorNumber)  && !line.StartsWith("90" + myDoctorNumber) && line.IndexOf(messageLinePattern1) != 1 && line.IndexOf(messageLinePattern2) != 1)
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
            var groupIndex = 1;
            for (var i = 0; i < myLines.Count(); i++)
            {
                myLine = myLines[i];

                if (!string.IsNullOrEmpty(myLine))
                {
                    if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                    {
                        var totalLine = new ClaimsReturnPaymentSummary();
                        totalLine.UserId = myUserId;
                        totalLine.RecordId = Guid.NewGuid();
                        totalLine.GroupIndex = groupIndex;
                        totalLine.LineNumber = i;
                        totalLine.TotalLineType = myLine.Substring(53, 10).ToUpper().Trim();
                        totalLine.FeeSubmitted = Convert.ToDouble(myLine.Substring(64, 9) + "." + myLine.Substring(73, 2));
                        totalLine.FeeApproved = Convert.ToDouble(myLine.Substring(77, 9) + "." + myLine.Substring(86, 2));
                        totalLine.TotalPremiumAmount = Convert.ToDouble(myLine.Substring(98, 9) + "." + myLine.Substring(107, 2));
                        totalLine.TotalProgramAmount = Convert.ToDouble(myLine.Substring(109, 9) + "." + myLine.Substring(118, 2));
                        totalLine.TotalPaidAmount = Convert.ToDouble(myLine.Substring(120, 9) + "." + myLine.Substring(129, 2));
                        totalLine.RunCode = myLine.Substring(253, 2);

                        if (totalLine.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase))
                        {
                            groupIndex++;
                        }

                        myTotalLines.Add(totalLine);
                    }
                    else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                    {
                        //Ignore the lines
                    }
                    else
                    {
                        var lineItem = CreateReturnLineItem(myLine);
                        lineItems.Add(lineItem);                       
                    }
                }
            }

            var returnContent = new ReturnContent();
            returnContent.PaidItems = new List<ClaimNumberGroup>();
            returnContent.ReturnClaimItems = new List<ClaimNumberGroup>();
            returnContent.PendingClaimItems = new List<ClaimNumberGroup>();

            if (lineItems.Any())
            {
                #region Remove DrawBack - Paid and Rejected Lines

                var groupByClaimNumberAndSeqToCheckAdjustment = lineItems.GroupBy(x => x.ClaimAndSeqNumber).Select(x => new { ClaimNumberAndSeq = x.Key, Records = x.ToList() }).ToList();

                foreach(var group in groupByClaimNumberAndSeqToCheckAdjustment)
                {
                    var adjustmentPaid = group.Records.FirstOrDefault(x => x.PaidType == PAID_TYPE.PAID && x.ApprovedUnitAmount < 0);
                    var containRejectedOrPendingClaims = group.Records.Any(x => x.PaidType == PAID_TYPE.PENDING_CLAIMS || x.PaidType == PAID_TYPE.RETURNED_CLAIMS);

                    if (adjustmentPaid != null && containRejectedOrPendingClaims)
                    {
                        lineItems.Remove(adjustmentPaid);
                    }
                }

                #endregion

                #region Pending

                var pendingClaimsGroupByCPSClaimNumber = lineItems.Where(x => x.PaidType == PAID_TYPE.PENDING_CLAIMS || x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE)
                            .GroupBy(x => x.CPSClaimNumber)
                            .Select(x => new
                            {
                                CPSClaimNumber = x.Key,
                                ClaimNumber = x.FirstOrDefault().ClaimNumber,
                                Lines = x.ToList()
                            }).ToList();

                foreach(var claimsGroup in pendingClaimsGroupByCPSClaimNumber.OrderBy(x => x.CPSClaimNumber))
                {
                    var pendingLineItems = claimsGroup.Lines.OrderBy(x => x.ClaimAndSeqNumber).ToList();
                    var containReturnLineItemsHospitalCareOrVisitProcedure = pendingLineItems.Any(x =>
                                            (x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE));

                    if (pendingLineItems.Any() && containReturnLineItemsHospitalCareOrVisitProcedure)
                    {
                        var firstLineItem = pendingLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber) && x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT)
                        .OrderBy(x => x.ClaimAndSeqNumber).FirstOrDefault();

                        var patientInfo = new PatientInfo();

                        var temp = firstLineItem.PatientInfo;
                        patientInfo.BirthDate = temp.BirthDate;
                        patientInfo.Sex = temp.Sex;
                        patientInfo.FirstName = temp.FirstName;
                        patientInfo.LastName = temp.LastName;
                        patientInfo.HospitalNumber = temp.HospitalNumber;
                        patientInfo.Province = temp.Province;

                        if (pendingLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE))
                        {
                            var oopPatientInfo = pendingLineItems.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE).PatientInfo;
                            patientInfo.Province = oopPatientInfo.Province;
                            patientInfo.FirstName = oopPatientInfo.FirstName;
                            patientInfo.LastName = oopPatientInfo.LastName;
                            patientInfo.HospitalNumber = oopPatientInfo.HospitalNumber;
                        }

                        returnContent.PendingClaimItems.Add(new ClaimNumberGroup()
                        {
                            ClaimNumber = claimsGroup.ClaimNumber,
                            CPSClaimNumber = claimsGroup.CPSClaimNumber,
                            PaidType = PAID_TYPE.PENDING_CLAIMS,
                            ReturnLineItems = pendingLineItems.Where(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT && !string.IsNullOrEmpty(x.ClaimAndSeqNumber)).ToList(),
                            FirstLineItem = firstLineItem,
                            ClaimPatientInfo = patientInfo
                        });
                    }
                }
                
                #endregion

                #region Paid and Rejected

                var paidAndRejectedClaimsGroupByCPSClaimNumber = lineItems.Where(x => x.PaidType != PAID_TYPE.PENDING_CLAIMS).GroupBy(x => x.CPSClaimNumber)
                    .Select(x => new
                    {
                        CPSClaimNumber = x.Key,
                        ClaimNumber = x.FirstOrDefault().ClaimNumber,
                        Lines = x.ToList()
                    }).ToList();

                foreach (var claimsGroup in paidAndRejectedClaimsGroupByCPSClaimNumber.OrderBy(x => x.CPSClaimNumber))
                {
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
                            CPSClaimNumber = claimsGroup.CPSClaimNumber,
                            PaidType = PAID_TYPE.RETURNED_CLAIMS,
                            ReturnLineItems = rejectedLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber) && x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT).ToList(),
                            MSBComment = string.Join(string.Empty, rejectedLineItems
                                                .Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.COMMENT)
                                                .OrderBy(x => x.CommentLineNumber).Select(x => x.Comment)),
                            FirstLineItem = rejectedLineItems.Where(x => !string.IsNullOrEmpty(x.ClaimAndSeqNumber) && x.ReturnedRecordType != RETURNED_RECORD_TYPE.COMMENT).OrderBy(x => x.ClaimAndSeqNumber).FirstOrDefault(),
                            ClaimPatientInfo = patientInfo
                        });
                    }

                    var paidLineItems = claimsGroup.Lines.Where(x => x.PaidType == PAID_TYPE.PAID).OrderBy(x => x.ClaimAndSeqNumber).ToList();
                    if (paidLineItems.Any())
                    {
                        returnContent.PaidItems.Add(new ClaimNumberGroup()
                        {
                            ClaimNumber = claimsGroup.ClaimNumber,
                            CPSClaimNumber = claimsGroup.CPSClaimNumber,
                            PaidType = PAID_TYPE.PAID,
                            ReturnLineItems = paidLineItems,
                            FirstLineItem = paidLineItems.FirstOrDefault(),
                            ClaimPatientInfo = paidLineItems.FirstOrDefault().PatientInfo
                        });
                    }
                }

                #endregion
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
                        var distinctCurrentLines = currentReturnLines.Distinct();
                        var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();
                        var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                        exceptLines.AddRange(keep89Lines);
                        return exceptLines;
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

                myLineItem.OriginalRunCode = myLine.Substring(243, 2).ToUpper().Trim();
                myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

            }
            else if (myLine.StartsWith("50") || myLine.StartsWith("57") || myLine.StartsWith("89") || myLine.StartsWith("60"))
            {
                myLineItem.ClaimNumber = int.Parse(myLine.Substring(6, 5));
                myLineItem.ClinicNumber = myLine.Substring(95, 3);
                myLineItem.PaidType = PAID_TYPE.RETURNED_CLAIMS;

                switch (myLine.Substring(0, 2))
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
                        myLineItem.OriginalRunCode = myLine.Substring(243, 2).ToUpper().Trim();
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
                        myLineItem.OriginalRunCode = myLine.Substring(243, 2).ToUpper().Trim();
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    case "89":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.OUT_OF_PROVINCE;
                        myLineItem.SeqNumber = -1;
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);
                        myLineItem.OriginalRunCode = myLine.Substring(243, 2).ToUpper().Trim();
                        myLineItem.CPSClaimNumber = myLine.Substring(245, 10).Trim();

                        break;
                    case "60":
                        myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.COMMENT;
                        myLineItem.SeqNumber = -1;
                        myLineItem.PatientInfo = GetPatientInfo(myLine, myLineItem.ReturnedRecordType);
                        myLineItem.CommentLineNumber = myLine.Substring(11, 1).Trim();
                        myLineItem.Comment = myLine.Substring(21, 74).Trim().ToUpper();
                        myLineItem.OriginalRunCode = myLine.Substring(243, 2).ToUpper().Trim();
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

        private void GeneratePaidClaimSerivceRecords(PaidClaim myPaidClaim, IList<ClaimNumberGroup> myClaimGroups, RejectedClaim myRejectedClaim, DateTime returnFileUTCDate)
        {
            foreach (var serviceRecord in ProcessList(myPaidClaim.PaidClaimId, ReturnFileType.BIWEEKLY, myClaimGroups, myRejectedClaim.RejectedClaimId, returnFileUTCDate))
            {
                if (serviceRecord.PaidClaimId == null && serviceRecord.RejectedClaimId == myRejectedClaim.RejectedClaimId)
                {
                    myRejectedClaim.ServiceRecord.Add(serviceRecord);
                }
                else
                {
                    myPaidClaim.ServiceRecord.Add(serviceRecord);
                }
            }
        }

        private void GenerateReturnClaimSerivceRecords(RejectedClaim myRejectedClaim, ReturnFileType myReturnFileType, IList<ClaimNumberGroup> myClaimGroups, DateTime returnFileUTCDate)
        {
            foreach (var serviceRecord in ProcessList(myRejectedClaim.RejectedClaimId, myReturnFileType, myClaimGroups, myRejectedClaim.RejectedClaimId, returnFileUTCDate))
            {
                myRejectedClaim.ServiceRecord.Add(serviceRecord);
            }
        }

        private List<ServiceRecord> ProcessList(Guid myReturnClaimId, ReturnFileType myReturnFileType, IList<ClaimNumberGroup> myClaimGroups, Guid myRejectedClaimId, DateTime returnFileUTCDate)
        {
            var myRecords = new List<ServiceRecord>();

            foreach (ClaimNumberGroup myClaimGroup in myClaimGroups)
            {
                if (!string.IsNullOrEmpty(myClaimGroup.ClaimPatientInfo.HospitalNumber))
                {
                    myRecords.AddRange(GenerateServiceRecord(myReturnClaimId, myClaimGroup, myRejectedClaimId, returnFileUTCDate));
                }
            }

            return myRecords;
        }


        private bool SetServiceRecordToPending(ClaimNumberGroup myClaimNumberGroup)
        {
            var needToPerformSaveChanges = false;
            var myNewServices = new List<ServiceRecord>();
            var needToCreateNewServiceRecord = false;
            var returnFileUnitRecordList = new List<UnitRecord>();

            //Console.WriteLine(myClaimNumberGroup.ClaimNumber);
            //if (myClaimNumberGroup.ClaimNumber == 80922)
            //{
            //    Console.WriteLine("found it");
            //}

            var myMatchedClaimNumberServiceRecords = GetMatchServiceRecords(myUserId, myClaimNumberGroup.ClaimNumber);

            var foundClaimNotCreateAgain = myMatchedClaimNumberServiceRecords
                                            .Where(x => x.ClaimToIgnore && x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    x.ServiceDate == myClaimNumberGroup.FirstLineItem.ServiceDate).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();

            if (foundClaimNotCreateAgain != null)
            {
                return false;
            }

            var matchedPreviousPaidClaims = GetMatchPaidServiceRecords(myUserId, myClaimNumberGroup);

            var resubmitClaimWithLineItems = GetMatchedClaimResubmitted(myUserId, myClaimNumberGroup);

            foreach (var myLine in myClaimNumberGroup.ReturnLineItems)
            {
                returnFileUnitRecordList.Add(CreateReturnUnitRecord(myLine));
            }

            //Only Care About Pending Claims

            myMatchedClaimNumberServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.ClaimToIgnore && myContext.Entry(x).State != System.Data.Entity.EntityState.Deleted).ToList();

            if (!string.IsNullOrEmpty(myClaimNumberGroup.CPSClaimNumber))
            {
                var matchedRecords = myMatchedClaimNumberServiceRecords.Where(x => x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                   IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                //Too restrictive
                if (!matchedRecords.Any())
                {
                    //Uses HSN or Patient Last Name
                    matchedRecords = myMatchedClaimNumberServiceRecords.Where(x => x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                   IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                }

                //Check the CPS Claim Number, we are only interested in the smallest CPS Claim Number, if there are duplicates. The largest CPS Claim Number will always rejected.

                var cpsClaimNumbers = matchedRecords.Where(x => !string.IsNullOrEmpty(x.CPSClaimNumber)).Select(x => x.CPSClaimNumber).Distinct().OrderBy(x => x).ToList();

                if (cpsClaimNumbers.Count() > 0)
                {
                    //If the first one in the list is the same as the return claim, then return the sevice records with that CPS Claim Number and no CPS Claim Number
                    if (cpsClaimNumbers.FirstOrDefault() == myClaimNumberGroup.CPSClaimNumber)
                    {
                        myMatchedClaimNumberServiceRecords = matchedRecords.Where(x => string.IsNullOrEmpty(x.CPSClaimNumber) || x.CPSClaimNumber == myClaimNumberGroup.CPSClaimNumber).ToList();
                    }
                    else
                    {
                        //If not the first one, SKIP this return claim, this is a duplicated.
                        return false;
                    }
                }
                else
                {
                    myMatchedClaimNumberServiceRecords = matchedRecords.Where(x => string.IsNullOrEmpty(x.CPSClaimNumber) || x.CPSClaimNumber == myClaimNumberGroup.CPSClaimNumber).ToList();
                }
            }
            
            if (myMatchedClaimNumberServiceRecords.Any() && returnFileUnitRecordList.Any())
            {
                var unitRecordUsedIds = new List<Guid>();

                #region Check Submitted / Pending Claim

                var submittedClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                        !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted                                           
                                        x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                        IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName))
                                        .FirstOrDefault();

                if (submittedClaim == null)
                {
                    //Too restrictive, match only HSN
                    submittedClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                        !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted                                           
                                        x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                }

                if (submittedClaim != null) //Submitted Claim
                {
                    UpdateServiceRecordFieldFromReturn(submittedClaim, myClaimNumberGroup);

                    if (string.IsNullOrEmpty(submittedClaim.CPSClaimNumber))
                    {
                        needToPerformSaveChanges = true;
                        submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                        SetServiceRecordStateToModified(submittedClaim);
                    }

                    var foundUnitRecords = new List<UnitRecord>();
                    foreach (var returnUnitRecord in returnFileUnitRecordList)
                    {
                        var foundExistingUnitRecord = GetMatchedUnitRecord(submittedClaim.UnitRecord, returnUnitRecord, foundUnitRecords);

                        if (foundExistingUnitRecord != null)
                        {
                            foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                            foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                            foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                            foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                            foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                            if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                            {
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                {
                                    foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                returnUnitRecord.UnitPremiumCode,
                                                                                foundExistingUnitRecord.UnitAmount,
                                                                                returnUnitRecord.UnitCode,
                                                                                submittedClaim.ServiceLocation);
                                }
                            }

                            foundUnitRecords.Add(foundExistingUnitRecord);
                            unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                        }
                    }

                    if (foundUnitRecords.Any()) //Update the unit records
                    {
                        needToPerformSaveChanges = true;

                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                               UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                            {
                                submittedClaim.UnitRecord.Remove(unitRecord);
                                SetUnitRecordStateToDeleted(unitRecord);
                            }
                            else
                            {
                                SetUnitRecordStateToModified(unitRecord);
                            }
                        }

                        if (submittedClaim.UnitRecord.Any())
                        {
                            var index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(submittedClaim);
                        }
                        else
                        {
                            DeleteServiceRecord(submittedClaim);
                        }
                    }
                }

                #endregion

                #region Check Rejected Claims
                //Never get pending claim from. If the claim is paid, it will do a draw back, and then pending. No rejected record.

                //Getting all the rejected claims
                var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue);

                //Get the most filter ones - Match Last Name (start) And Hospital Number
                var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                            x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                            IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                if (matchedRejectedServiceRecord == null)
                {
                    //Too restricted, then try either Last Name or Hospital Number
                    matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                            x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                            IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                }

                if (matchedRejectedServiceRecord != null)
                {
                    UpdateServiceRecordFieldFromReturn(matchedRejectedServiceRecord, myClaimNumberGroup);

                    #region Found Rejected Claims

                    var foundUnitRecords = new List<UnitRecord>();
                    foreach (var returnUnitRecord in returnFileUnitRecordList)
                    {
                        var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                        if (foundExistingUnitRecord != null)
                        {
                            foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                            foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                            foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                            foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                            foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                            if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                            {
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                {
                                    foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                returnUnitRecord.UnitPremiumCode,
                                                                                foundExistingUnitRecord.UnitAmount,
                                                                                returnUnitRecord.UnitCode,
                                                                                matchedRejectedServiceRecord.ServiceLocation);
                                }
                            }

                            foundUnitRecords.Add(foundExistingUnitRecord);
                            unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                        }
                    }

                    if (matchedRejectedServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                    {
                        #region All Line Items

                        needToPerformSaveChanges = true;

                        //All lines items are there

                        if (submittedClaim == null)
                        {
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                            }

                            if (matchedRejectedServiceRecord.UnitRecord.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }


                                //Convert the Rejected claim to Pending
                                matchedRejectedServiceRecord.RejectedClaimId = null;
                                matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                SetServiceRecordStateToModified(matchedRejectedServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedRejectedServiceRecord);
                            }
                        }
                        else
                        {
                            //There is submited claim, need to map them to the Submitted Claim
                            foreach(var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    unitRecord.ServiceRecordId = submittedClaim.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecord);
                                }
                            }

                            var index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(submittedClaim);

                            DeleteServiceRecord(matchedRejectedServiceRecord);
                        }

                        #endregion
                    }
                    else if (foundUnitRecords.Any())
                    {
                        #region Partial Line Items - Rejected Claim

                        needToPerformSaveChanges = true;

                        //Partial line match on Rejected, only remap the matched

                        if (submittedClaim != null)
                        {
                            #region Existed Submit Claim - Rejected Claim

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    unitRecord.ServiceRecordId = submittedClaim.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecord);
                                }
                            }

                            var index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(submittedClaim);

                            if (matchedRejectedServiceRecord.UnitRecord.Any())
                            {
                                index = 1;
                                foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                SetServiceRecordStateToModified(matchedRejectedServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedRejectedServiceRecord);
                            }

                            #endregion
                        }
                        else
                        {
                            #region Create New Pending Claim

                            //Create new pending claim to hold the matched line items

                            var notMatchUnitRecords = new List<UnitRecord>();

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    notMatchUnitRecords.Add(unitRecord);
                                }
                            }

                            if (notMatchUnitRecords.Any())
                            {
                                var myNewPendingService = new ServiceRecord();

                                CopyServiceRecordFields(matchedRejectedServiceRecord, myNewPendingService, myClaimNumberGroup.ClaimNumber);

                                var index = 1;
                                foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.ServiceRecordId = myNewPendingService.ServiceRecordId;
                                    unitRecord.RecordIndex = index;
                                    myNewPendingService.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                myNewPendingService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                myNewPendingService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToAdded(myNewPendingService);
                                myNewServices.Add(myNewPendingService);
                            }

                            if (matchedRejectedServiceRecord.UnitRecord.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToModified(matchedRejectedServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedRejectedServiceRecord);
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #endregion
                }

                #endregion

                #region Check Paid Claims
                //Never get pending claim from. If the claim is paid, it will do a draw back, and then pending. No rejected record.

                //Getting all the rejected claims
                var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                //Get the most filter ones - Match Last Name (start) And Hospital Number
                var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                            x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                            IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                if (matchedPaidServiceRecord == null)
                {
                    //Too restricted, then try either Last Name or Hospital Number
                    matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                            x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                            IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                }

                if (matchedPaidServiceRecord != null)
                {
                    UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);

                    #region Found Paid Claims

                    var foundUnitRecords = new List<UnitRecord>();
                    foreach (var returnUnitRecord in returnFileUnitRecordList)
                    {
                        var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                        if (foundExistingUnitRecord != null)
                        {
                            foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                            foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                            foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                            foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                            foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                            if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                            {
                                foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                {
                                    foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                returnUnitRecord.UnitPremiumCode,
                                                                                foundExistingUnitRecord.UnitAmount,
                                                                                returnUnitRecord.UnitCode,
                                                                                matchedPaidServiceRecord.ServiceLocation);
                                }
                            }

                            foundUnitRecords.Add(foundExistingUnitRecord);
                            unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                        }
                    }

                    if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                    {
                        #region All Line Items - Paid Claim

                        needToPerformSaveChanges = true;

                        //All lines items are there

                        if (submittedClaim == null)
                        {
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                            }

                            if (matchedPaidServiceRecord.UnitRecord.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                //Not Submitted Claim, Simply convert the Paid claim to Pending
                                matchedPaidServiceRecord.RejectedClaimId = null;
                                matchedPaidServiceRecord.PaidClaimId = null;
                                matchedPaidServiceRecord.PaymentApproveDate = null;
                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.PaidAmount = 0d;
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToModified(matchedPaidServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPaidServiceRecord);
                            }
                        }
                        else
                        {
                            //There is submitted claim, need to map them to the submitted / pending Claim
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    unitRecord.ServiceRecordId = submittedClaim.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecord);
                                }
                            }

                            var index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(submittedClaim);

                            DeleteServiceRecord(matchedPaidServiceRecord);
                        }

                        #endregion
                    }
                    else if (foundUnitRecords.Any())
                    {
                        #region Partial Line Items - Paid Claim

                        needToPerformSaveChanges = true;

                        //Partial line match on Paid, only remap the matched

                        if (submittedClaim != null)
                        {
                            #region Existed Submit Claim - Paid Claim

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    unitRecord.ServiceRecordId = submittedClaim.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecord);
                                }
                            }

                            var index = 1;
                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(submittedClaim);

                            if (matchedPaidServiceRecord.UnitRecord.Any())
                            {
                                index = 1;
                                foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                SetServiceRecordStateToModified(matchedPaidServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPaidServiceRecord);
                            }

                            #endregion
                        }
                        else
                        {
                            #region Create New Pending Claim - Paid Claim

                            //Create new pending claim to hold the matched line items

                            var notMatchUnitRecords = new List<UnitRecord>();

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                {
                                    matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    notMatchUnitRecords.Add(unitRecord);
                                }
                            }

                            if (notMatchUnitRecords.Any())
                            {
                                var myNewPendingService = new ServiceRecord();

                                CopyServiceRecordFields(matchedPaidServiceRecord, myNewPendingService, myClaimNumberGroup.ClaimNumber);

                                var index = 1;
                                foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.ServiceRecordId = myNewPendingService.ServiceRecordId;
                                    unitRecord.RecordIndex = index;
                                    myNewPendingService.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                myNewPendingService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                myNewPendingService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToAdded(myNewPendingService);
                                myNewServices.Add(myNewPendingService);
                            }

                            if (matchedPaidServiceRecord.UnitRecord.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToModified(matchedPaidServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPaidServiceRecord);
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #endregion
                }

                #endregion
                
                #region Deal with Not Found Line Items (Remaining)

                var notUsedReturnRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId) && 
                                                !UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, x) && !UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, x)).ToList();

                var pendingDuplicateClaims = new List<ServiceRecord>();

                //Check Number of Pending or Rejected Claims, and merge
                pendingDuplicateClaims.AddRange(myMatchedClaimNumberServiceRecords.Where(x => x.CPSClaimNumber == myClaimNumberGroup.CPSClaimNumber &&
                                            !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

                pendingDuplicateClaims.AddRange(myNewServices);

                if (pendingDuplicateClaims.Any())
                {
                    #region Deal With Not Found List Items

                    if (notUsedReturnRecords.Any())
                    {
                        needToPerformSaveChanges = true;

                        //Get the first claim in the list and add not used line items to it
                        var firstPending = pendingDuplicateClaims.FirstOrDefault();

                        foreach (var unitRecord in notUsedReturnRecords)
                        {
                            unitRecord.ServiceRecordId = firstPending.ServiceRecordId;
                            firstPending.UnitRecord.Add(unitRecord);
                            SetUnitRecordStateToAdded(unitRecord);
                        }

                        firstPending.ClaimAmount = GetUnitRecordPaidAmountSum(firstPending.UnitRecord);
                        firstPending.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                    }

                    #endregion

                    #region Merge Duplicate Claims

                    if (pendingDuplicateClaims.Count() > 1)
                    {
                        //Need to merge the Paid Claims together
                        var recordToKeep = pendingDuplicateClaims.ElementAt(0);

                        if (string.IsNullOrEmpty(recordToKeep.MessageFromICS))
                        {
                            var recordThatHaveMessageICS = pendingDuplicateClaims.FirstOrDefault(x => !string.IsNullOrEmpty(x.MessageFromICS));
                            if (recordThatHaveMessageICS != null)
                            {
                                recordToKeep.MessageFromICS = recordThatHaveMessageICS.MessageFromICS;
                            }
                        }

                        for (var i = 1; i < pendingDuplicateClaims.Count(); i++)
                        {
                            var recordToMerge = pendingDuplicateClaims.ElementAt(i);

                            if (recordToMerge.UnitRecord.Count > 0)
                            {
                                do
                                {
                                    var unitRecordToChange = recordToMerge.UnitRecord.FirstOrDefault();
                                    unitRecordToChange.ServiceRecordId = recordToKeep.ServiceRecordId;
                                    SetUnitRecordStateToModified(unitRecordToChange);

                                }
                                while (recordToMerge.UnitRecord.Count() > 0);
                            }

                            if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Modified)
                            {
                                //When state is modified, then the claim is in the database already
                                DeleteServiceRecord(recordToMerge);
                            }
                            else if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Added)
                            {
                                myContext.Entry(recordToMerge).State = System.Data.Entity.EntityState.Unchanged;
                                myNewServices.Clear();
                            }
                        }

                        RemoveDuplicateUnitRecords(recordToKeep);

                        recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                    }
                    else
                    {
                        var recordToKeep = pendingDuplicateClaims.FirstOrDefault();
                        RemoveDuplicateUnitRecords(recordToKeep);
                        recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                    }

                    #endregion
                }
                else if (notUsedReturnRecords.Any() && submittedClaim != null)
                {
                    needToPerformSaveChanges = true;

                    //Got Pending Claim come in and got Existed Pending Claim but not match on the unit record.
                    //This could mean the line item was deleted somehow.
                    foreach (var unitRecord in notUsedReturnRecords)
                    {
                        unitRecord.ServiceRecordId = submittedClaim.ServiceRecordId;
                        submittedClaim.UnitRecord.Add(unitRecord);
                        SetUnitRecordStateToAdded(unitRecord);
                    }

                    var index = 1;
                    foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                    {
                        unitRecord.RecordIndex = index;
                        SetUnitRecordStateToModified(unitRecord);

                        index++;
                    }

                    submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                    submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                    SetServiceRecordStateToModified(submittedClaim);
                }
                else if (notUsedReturnRecords.Any())
                {
                    //Need to create Paid Claim to hold the not used Line Items
                    returnFileUnitRecordList.Clear();
                    returnFileUnitRecordList.AddRange(notUsedReturnRecords);

                    needToCreateNewServiceRecord = true;
                }

                #endregion

                #region Deal with New Service Record with Old Unit Record -- STUPID!!!!

                if (myNewServices.Any())
                {
                    var record = myNewServices.FirstOrDefault();

                    var newUnitRecordList = new List<UnitRecord>();

                    do
                    {
                        var unitRecord = record.UnitRecord.FirstOrDefault();

                        if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                            myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                        {
                            newUnitRecordList.Add(CloneUnitRecord(unitRecord));
                            SetUnitRecordStateToDeleted(unitRecord);
                        }
                        else if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Added)
                        {
                            newUnitRecordList.Add(unitRecord);
                            record.UnitRecord.Remove(unitRecord);
                        }
                    }
                    while (record.UnitRecord.Count > 0);

                    record.UnitRecord.Clear();

                    foreach (var newUnitRecord in newUnitRecordList)
                    {
                        record.UnitRecord.Add(newUnitRecord);
                    }
                }

                #endregion
            }
            else
            {
                needToCreateNewServiceRecord = true;
            }

            if (needToCreateNewServiceRecord && returnFileUnitRecordList.Any())
            {
                #region Not Found Any Exisitng Claim with Claim Number

                var notMatchResubmittedLineItems = GetNotMatchResubmitClaimLineItems(resubmitClaimWithLineItems, returnFileUnitRecordList);
                var notMatchLineItems = GetNotMatchPreviousPaidClaimLineItems(matchedPreviousPaidClaims, notMatchResubmittedLineItems);

                if (notMatchLineItems.Any())
                {
                    var myNewService = new ServiceRecord();
                    myNewService.ServiceRecordId = Guid.NewGuid();
                    myNewService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchLineItems);

                    SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                    foreach (var unitRecord in notMatchLineItems)
                    {
                        unitRecord.ServiceRecordId = myNewService.ServiceRecordId;

                        myNewService.UnitRecord.Add(unitRecord);
                        SetUnitRecordStateToAdded(unitRecord);
                    }

                    SetServiceRecordStateToAdded(myNewService);
                    myNewServices.Add(myNewService);
                }

                #endregion
            }

            if (myNewServices.Any())
            {
                needToPerformSaveChanges = true;
            }

            return needToPerformSaveChanges;
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
                    item.ReturnLineContent = new List<string>();

                    var lineItem1 = group.ReturnLineItems.ElementAt(0);

                    if (group.ReturnLineItems.Count() == 2)
                    {
                        var lineItem2 = group.ReturnLineItems.ElementAt(1);

                        if (((lineItem1.ApprovedUnitCode == lineItem2.ApprovedUnitCode) ||
                            (lineItem1.SubmittedUnitCode == lineItem2.SubmittedUnitCode && lineItem2.SubmittedUnitCode == lineItem1.ApprovedUnitCode)) &&
                            IsAPaidLineContainNegativeApprovedFee(lineItem1) && IsAPaidLineContainPositiveDollarWithExplainCode(lineItem2))
                        {
                            item.LineItem = lineItem2;
                            item.AdjustmentType = AdjustmentType.CHANGE_IN_PAID_AMOUNT;
                            item.DrawbackAmount = Math.Abs(lineItem1.ApprovePlusPremiumAmount);
                            item.ApprovedAmount = Math.Abs(lineItem2.ApprovePlusPremiumAmount);
                            item.ExplainCode1 = lineItem2.ExplainCode1;
                            item.ExplainCode2 = lineItem2.ExplainCode2;
                            item.ExplainCode3 = lineItem2.ExplainCode3;
                            item.ReturnLineContent.AddRange(group.ReturnLineItems.Select(x => x.LineContent));

                            result.Add(item);
                        }
                        else if (((lineItem1.ApprovedUnitCode == lineItem2.ApprovedUnitCode) ||
                            (lineItem1.SubmittedUnitCode == lineItem2.SubmittedUnitCode && lineItem1.SubmittedUnitCode == lineItem2.ApprovedUnitCode)) &&
                            IsAPaidLineContainNegativeApprovedFee(lineItem2) && IsAPaidLineContainPositiveDollarWithExplainCode(lineItem1))
                        {                            
                            item.LineItem = lineItem1;
                            item.AdjustmentType = AdjustmentType.CHANGE_IN_PAID_AMOUNT;
                            item.DrawbackAmount = Math.Abs(lineItem2.ApprovePlusPremiumAmount);
                            item.ApprovedAmount = Math.Abs(lineItem1.ApprovePlusPremiumAmount);
                            item.ExplainCode1 = lineItem1.ExplainCode1;
                            item.ExplainCode2 = lineItem2.ExplainCode2;
                            item.ExplainCode3 = lineItem2.ExplainCode3;
                            item.ReturnLineContent.AddRange(group.ReturnLineItems.Select(x => x.LineContent));

                            result.Add(item);
                        }
                    }
                    else if (group.ReturnLineItems.Count() == 1)
                    {
                        if (IsAPaidLineContainNegativeApprovedFee(lineItem1))
                        {
                            item.LineItem = lineItem1;
                            item.AdjustmentType = AdjustmentType.DRAW_BACK;
                            item.DrawbackAmount = Math.Abs(lineItem1.ApprovePlusPremiumAmount);
                            item.ApprovedAmount = 0;
                            item.ExplainCode1 = string.Empty;
                            item.ExplainCode2 = string.Empty;
                            item.ExplainCode3 = string.Empty;
                            item.ReturnLineContent.AddRange(group.ReturnLineItems.Select(x => x.LineContent));

                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }

        private bool IsAPaidLineContainNegativeApprovedFee(ReturnLineItem myLine)
        {
            return (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && myLine.ApprovedUnitAmount < 0 && myLine.ApprovePlusPremiumAmount < 0);
        }

        private bool IsAPaidLineContainPositiveDollarWithExplainCode(ReturnLineItem myLine)
        {
            //&& !string.IsNullOrEmpty(myLine.ExplainCode1)
            return (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && myLine.ApprovedUnitAmount > 0);
        }

        private IEnumerable<ServiceRecord> GenerateServiceRecord(Guid myReturnClaimId, ClaimNumberGroup myClaimNumberGroup, Guid myRejectedClaimId, DateTime returnFileUTCDate)
        {
            //myClaimGroup will only contain either PAID claims OR RETURNED claims

            //myReturnClaimId will be RejectedClaimId when looping through Rejected Claims. myRejectedClaimId is the same as myReturnClaimId
            //myReturnClaimId will be PaidClaimId when looping through Paid Claims. myRejectedClaimId is the RejectedClaimId from previously processed rejected claims

            //ServiceRecord.ClaimAmount = How much in total I will get with Premium
            //ServiceRecord.PaidAmount = How much I will get Paid

            //UnitRecord.UnitAmount = Unit Number * $Fee per Unit
            //UnitRecord.PaidAmount = Unit Amount + Preimum Amount

            //Console.WriteLine(myClaimNumberGroup.ClaimNumber);
            //if (myClaimNumberGroup.ClaimNumber == 80922)
            //{
            //    Console.WriteLine("found it");
            //}s

            var needToCreateNewServiceRecord = false;
            var myNewServices = new List<ServiceRecord>();
            var returnFileUnitRecordList = new List<UnitRecord>();

            var myClaimNumberAndSeqAndAdjustmentList = CheckAdjustmentsInReturn(myClaimNumberGroup);

            if (!myClaimNumberAndSeqAndAdjustmentList.Any())
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

            var foundClaimNotCreateAgain = myMatchedClaimNumberServiceRecords
                                            .Where(x => x.ClaimToIgnore && x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase)
                                                        && x.ServiceDate == myClaimNumberGroup.FirstLineItem.ServiceDate).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();

            myMatchedClaimNumberServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.ClaimToIgnore).ToList();

            #region Deal with Multi CPS Claim Number

            if (!string.IsNullOrEmpty(myClaimNumberGroup.CPSClaimNumber))
            {
                var matchedRecords = myMatchedClaimNumberServiceRecords.Where(x => x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                   IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                //Too restrictive
                if (!matchedRecords.Any())
                {
                    //Uses HSN or Patient Last Name
                    matchedRecords = myMatchedClaimNumberServiceRecords.Where(x => x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                   IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                }

                //Check the CPS Claim Number, we are only interested in the smallest CPS Claim Number, if there are duplicates. The largest CPS Claim Number will always rejected.
                
                var cpsClaimNumbers = matchedRecords.Where(x => !string.IsNullOrEmpty(x.CPSClaimNumber)).Select(x => x.CPSClaimNumber).Distinct().OrderBy(x => x).ToList();

                if (cpsClaimNumbers.Count() > 0)
                {
                    //If the first one in the list is the same as the return claim, then return the sevice records with that CPS Claim Number and no CPS Claim Number
                    if (cpsClaimNumbers.FirstOrDefault() == myClaimNumberGroup.CPSClaimNumber)
                    {
                        myMatchedClaimNumberServiceRecords = matchedRecords.Where(x => string.IsNullOrEmpty(x.CPSClaimNumber) || x.CPSClaimNumber == myClaimNumberGroup.CPSClaimNumber).ToList();
                    }
                    else
                    {
                        //If not the first one, SKIP this return claim, this is a duplicated.
                        return myNewServices;
                    }
                }
                else
                {
                    myMatchedClaimNumberServiceRecords = matchedRecords.Where(x => string.IsNullOrEmpty(x.CPSClaimNumber) || x.CPSClaimNumber == myClaimNumberGroup.CPSClaimNumber).ToList();
                }
            }

            #endregion

            var matchedPreviousPaidClaims = GetMatchPaidServiceRecords(myUserId, myClaimNumberGroup);

            var resubmitClaimWithLineItems = GetMatchedClaimResubmitted(myUserId, myClaimNumberGroup);

            var continueToProcessClaim = true;
            if (foundClaimNotCreateAgain != null) //Claim Marked As Ignore
            {
                continueToProcessClaim = false;
            }

            if (myMatchedClaimNumberServiceRecords.Any())
            {
                //Deal with Adjustment Claims first 
                if (myClaimNumberAndSeqAndAdjustmentList.Any())
                {
                    var adjustmentUnitRecords = new List<UnitRecord>();
                    var adjustmentLineContent = new List<string>();

                    //Only care about Changes In Paid Amount
                    //The Drawback adjustment will be taken care from the Rejected Claims code down below
                    if (myClaimNumberAndSeqAndAdjustmentList.Any(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                    {
                        #region Deal with Adjustment - Change in Paid Amount
                    
                        foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                        {
                            if (myAdjustment.DrawbackAmount != myAdjustment.ApprovedAmount)
                            {
                                adjustmentUnitRecords.Add(CreateReturnUnitRecord(myAdjustment.LineItem));
                            }

                            adjustmentLineContent.AddRange(myAdjustment.ReturnLineContent);
                        }

                        if (adjustmentUnitRecords.Any())
                        {
                            //We got Adjustments, need to check previously Paid claims and change the Paid Amount accordingly.

                            var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                            //Get the most filter ones - Match Last Name (start) And Hospital Number
                            ServiceRecord matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                        x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                        IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                            if (matchedPaidServiceRecord == null)
                            {
                                //Too restricted, then try either Last Name or Hospital Number
                                matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                        x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                            }

                            if (matchedPaidServiceRecord != null)
                            {
                                UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);

                                #region Deal with Existed Paid Claim

                                var adjustmentMessage = string.Empty;
                                var foundUnitRecords = new List<UnitRecord>();
                                foreach (var returnUnitRecord in adjustmentUnitRecords)
                                {
                                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                    if (foundExistingUnitRecord != null)
                                    {
                                        foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                        foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                        foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                        foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                        foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                        foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                        foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                        if (returnUnitRecord.SubmittedAmount > 0d)                                        
                                        {
                                            foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                        }

                                        if (returnUnitRecord.UnitAmount > 0d)
                                        {
                                            foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                        }

                                        if (returnUnitRecord.UnitCode.Contains(","))
                                        {
                                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                                            foundExistingUnitRecord.UnitCode = splitCode.First();
                                        }

                                        if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                        {
                                            foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                            if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                            {
                                                foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                            returnUnitRecord.UnitPremiumCode,
                                                                                            foundExistingUnitRecord.UnitAmount,
                                                                                            returnUnitRecord.UnitCode,
                                                                                            matchedPaidServiceRecord.ServiceLocation);
                                            }
                                        }

                                        foundUnitRecords.Add(foundExistingUnitRecord);
                                        SetUnitRecordStateToModified(foundExistingUnitRecord);
                                    }
                                    else
                                    {
                                        returnUnitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                        if (returnUnitRecord.UnitCode.Contains(","))
                                        {
                                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                                            returnUnitRecord.UnitCode = splitCode.First();
                                        }

                                        SetUnitRecordStateToAdded(returnUnitRecord);
                                    }
                                }

                                matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;

                                adjustmentMessage += "System: Payment Adjustments. ";
                                adjustmentMessage += "Change In Paid Amount. ";
                                adjustmentMessage += $"Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.DrawbackAmount).ToString("C")}, Approved: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.ApprovedAmount).ToString("C")}";

                                matchedPaidServiceRecord.MessageFromICS = adjustmentMessage;

                                RemoveDuplicateUnitRecords(matchedPaidServiceRecord);

                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                _paidClaimNeeded = true;

                                #endregion
                            }
                            else
                            {
                                #region Create Paid Claim

                                var adjustmentMessage = string.Empty;

                                var myNewPaidService = new ServiceRecord();
                                myNewPaidService.ServiceRecordId = Guid.NewGuid();

                                SetServiceRecordFieldsFromClaimGroup(myNewPaidService, myClaimNumberGroup);

                                var index = 1;
                                foreach (var returnUnitRecord in adjustmentUnitRecords)
                                {
                                    returnUnitRecord.RecordIndex = index;
                                    returnUnitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId;

                                    var splitCode = returnUnitRecord.UnitCode.Split(',');
                                    returnUnitRecord.UnitCode = returnUnitRecord.UnitCode.Split(',').First();

                                    SetUnitRecordStateToAdded(returnUnitRecord);
                                    myNewPaidService.UnitRecord.Add(returnUnitRecord);

                                    index++;
                                }

                                //Paid Claims
                                myNewPaidService.PaidClaimId = myReturnClaimId;
                                myNewPaidService.PaymentApproveDate = returnFileUTCDate;

                                myNewPaidService.ClaimAmount = GetUnitRecordSubmittedAmountSum(myNewPaidService.UnitRecord, myNewPaidService.ServiceLocation);
                                myNewPaidService.PaidAmount = GetUnitRecordPaidAmountSum(myNewPaidService.UnitRecord);

                                adjustmentMessage += "System: Payment Adjustments. ";
                                adjustmentMessage += "Change In Paid Amount. ";
                                adjustmentMessage += $"Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.DrawbackAmount).ToString("C")}, Approved: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.ApprovedAmount).ToString("C")}";

                                myNewPaidService.MessageFromICS = adjustmentMessage.Trim() + " ";

                                SetServiceRecordStateToAdded(myNewPaidService);
                                myNewServices.Add(myNewPaidService);

                                #endregion
                            }
                        }

                        #endregion
                    }

                    if (myClaimNumberAndSeqAndAdjustmentList.Any(x => x.AdjustmentType == AdjustmentType.DRAW_BACK))
                    {
                        #region Deal with Adjustment - Draw Back

                        adjustmentUnitRecords.Clear();
                        foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.DRAW_BACK))
                        {
                            var unitRecord = CreateReturnUnitRecord(myAdjustment.LineItem);
                            unitRecord.PaidAmount = Math.Abs(unitRecord.PaidAmount);
                            unitRecord.UnitAmount = Math.Abs(unitRecord.UnitAmount);

                            if (unitRecord.SubmittedAmount.HasValue)
                            {
                                unitRecord.SubmittedAmount = Math.Abs(unitRecord.SubmittedAmount.Value);
                            }

                            adjustmentUnitRecords.Add(unitRecord);
                            adjustmentLineContent.AddRange(myAdjustment.ReturnLineContent);
                        }

                        //We got Adjustments, need to check previously Paid claims and rejected claims

                        //Getting the rejected claim
                        var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue);

                        var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedRejectedServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                        }

                        //Getting all the paid claim
                        var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedPaidServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (matchedRejectedServiceRecord == null && matchedPaidServiceRecord == null)
                        {
                            needToCreateNewServiceRecord = true;
                        }
                        else if (matchedRejectedServiceRecord != null && matchedPaidServiceRecord != null)
                        {
                            UpdateServiceRecordFieldFromReturn(matchedRejectedServiceRecord, myClaimNumberGroup);
                            UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);

                            #region Deal with existed Rejected and existed Paid

                            //Check Paid Claim First
                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in adjustmentUnitRecords)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (returnUnitRecord.SubmittedAmount > 0d)                                  
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedPaidServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);

                                    //Map the found unit record to the Rejected Claim
                                    foundExistingUnitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                    SetUnitRecordStateToModified(foundExistingUnitRecord);
                                }
                            }

                            if (foundUnitRecords.Any())
                            {
                                _paidClaimNeeded = true;

                                if (!matchedPaidServiceRecord.UnitRecord.Any())
                                {
                                    //No longer need the paid claim, moved the unit record to rejected already. Delete the paid claim
                                    DeleteServiceRecord(matchedPaidServiceRecord);
                                }
                                else
                                {
                                    //Not all unit records in paid claim are moved, need to update the paid claim info
                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                    matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    var adjustmentMessage = string.Empty;
                                    adjustmentMessage += "System: Payment Adjustments. ";
                                    adjustmentMessage += $"Claim Payment Draw Back: {foundUnitRecords.Sum(x => x.PaidAmount).ToString("C")}";

                                    matchedPaidServiceRecord.MessageFromICS = adjustmentMessage;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                }
                            }

                            foundUnitRecords.Clear();
                            foreach (var returnUnitRecord in adjustmentUnitRecords)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;

                                    if (returnUnitRecord.SubmittedAmount > 0d)                                   
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedRejectedServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);

                                    //Save the updated info to DB
                                    SetUnitRecordStateToModified(foundExistingUnitRecord);
                                }
                                else
                                {
                                    //Add the not found one to the Rejected Claim
                                    returnUnitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        returnUnitRecord.UnitCode = splitCode.First();
                                    }

                                    SetUnitRecordStateToAdded(returnUnitRecord);
                                }
                            }

                            RemoveDuplicateUnitRecords(matchedRejectedServiceRecord);

                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.PaidAmount = 0d;
                            matchedRejectedServiceRecord.PaymentApproveDate = null;
                            matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            matchedRejectedServiceRecord.PaidClaimId = null;
                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;

                            SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                            _rejectedClaimNeeded = true;

                            #endregion
                        }
                        else if (matchedRejectedServiceRecord != null && matchedPaidServiceRecord == null) //rejected claim only
                        {
                            UpdateServiceRecordFieldFromReturn(matchedRejectedServiceRecord, myClaimNumberGroup);

                            #region Deal with Draw Back Payment Rejected Claims

                            var foundUnitRecords = new List<UnitRecord>();
                            var needToAddUnitRecords = new List<UnitRecord>();

                            //Match unit recrod from Rejected claims to current unit record in return
                            foreach (var returnUnitRecord in adjustmentUnitRecords)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (returnUnitRecord.SubmittedAmount > 0d)                                 
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }
                                    
                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        foundExistingUnitRecord.UnitCode,
                                                                                        matchedRejectedServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                }
                                else
                                {
                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        returnUnitRecord.UnitCode = splitCode.First();
                                    }

                                    needToAddUnitRecords.Add(returnUnitRecord);
                                }
                            }

                            //Update found existing unit records for the claim
                            foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToModified(unitRecord);
                            }

                            //Add not found unit records to the claim
                            foreach (var unitRecord in needToAddUnitRecords.OrderBy(x => x.RecordIndex))
                            {
                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToAdded(unitRecord);
                            }

                            var index = 1;
                            foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);
                                
                                index++;
                            }

                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.PaidAmount = 0d;
                            matchedRejectedServiceRecord.PaymentApproveDate = null;
                            matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            matchedRejectedServiceRecord.PaidClaimId = null;
                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;

                            SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                            _rejectedClaimNeeded = true;

                            #endregion
                        }
                        else if (matchedRejectedServiceRecord == null && matchedPaidServiceRecord != null) //There is paid claim only
                        {
                            UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);

                            #region Deal with Paid Claim Only

                            var foundUnitRecords = new List<UnitRecord>();
                            var needToAddUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in adjustmentUnitRecords)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (returnUnitRecord.SubmittedAmount > 0d)
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        foundExistingUnitRecord.UnitCode,
                                                                                        matchedPaidServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                }
                                else
                                {
                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        returnUnitRecord.UnitCode = splitCode.First();
                                    }

                                    needToAddUnitRecords.Add(returnUnitRecord);
                                }
                            }

                            var adjustmentMessage = string.Empty;
                            if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All Line Items

                                if (!continueToProcessClaim)
                                {
                                    DeleteUnitRecords(foundUnitRecords);
                                    DeleteServiceRecord(matchedPaidServiceRecord);
                                }
                                else
                                {
                                    if (needToAddUnitRecords.Any())
                                    {                                                                                
                                        foreach (var unitRecord in needToAddUnitRecords)
                                        {
                                            if (!UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) && 
                                                !UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                                SetUnitRecordStateToAdded(unitRecord);
                                            }
                                        }
                                    }

                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                            UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                        {
                                            matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                            SetUnitRecordStateToDeleted(unitRecord);
                                        }                                   
                                    }

                                    if (matchedPaidServiceRecord.UnitRecord.Any())
                                    {
                                        var index = 1;
                                        foreach(var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);
                                            index++;
                                        }

                                        //All of the line items are draw back, convert the paid to rejected
                                        matchedPaidServiceRecord.PaidClaimId = null;
                                        matchedPaidServiceRecord.RejectedClaimId = myRejectedClaimId;
                                        matchedPaidServiceRecord.PaidAmount = 0d;
                                        matchedPaidServiceRecord.PaymentApproveDate = null;
                                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);

                                        adjustmentMessage += "System: Payment Adjustments. ";
                                        adjustmentMessage += $"Claim Payment Draw Back: {matchedPaidServiceRecord.ClaimAmount.ToString("C")}";

                                        matchedPaidServiceRecord.MessageFromICS += adjustmentMessage;
                                        matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                    }
                                    else
                                    {
                                        DeleteServiceRecord(matchedPaidServiceRecord);
                                    }
                                }

                                _rejectedClaimNeeded = true;

                                #endregion
                            }
                            else
                            {
                                _rejectedClaimNeeded = true;

                                #region Partial Line Items

                                //Only Some of the line items are draw back
                                if (!continueToProcessClaim)
                                {
                                    adjustmentMessage += "System: Payment Adjustments. ";
                                    adjustmentMessage += $"Claim Payment Draw Back: {GetUnitRecordPaidAmountSum(foundUnitRecords).ToString("C")}";

                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                        SetUnitRecordStateToDeleted(unitRecord);
                                    }

                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    matchedPaidServiceRecord.MessageFromICS = adjustmentMessage;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                }
                                else
                                {
                                    var notMatch = new List<UnitRecord>();
                                    foreach(var unitRecord in foundUnitRecords)
                                    {
                                        if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                            UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                        {
                                            matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                            SetUnitRecordStateToDeleted(unitRecord);
                                        }
                                        else
                                        {
                                            notMatch.Add(unitRecord);
                                        }
                                    }

                                    if (notMatch.Any() || needToAddUnitRecords.Any())
                                    {
                                        //Create Rejected Claim for the draw back line items
                                        var myNewRejectedService = new ServiceRecord();
                                        CopyServiceRecordFields(matchedPaidServiceRecord, myNewRejectedService, myClaimNumberGroup.ClaimNumber);

                                        foreach (var unitRecord in notMatch.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                            myNewRejectedService.UnitRecord.Add(unitRecord);
                                            SetUnitRecordStateToModified(unitRecord);
                                        }

                                        if (needToAddUnitRecords.Any())
                                        {
                                            foreach (var unitRecord in needToAddUnitRecords)
                                            {
                                                if (!UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) &&
                                                    !UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                                {
                                                    unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                                    myNewRejectedService.UnitRecord.Add(unitRecord);
                                                    SetUnitRecordStateToAdded(unitRecord);
                                                }
                                            }
                                        }

                                        var index = 1;
                                        foreach (var unitRecord in myNewRejectedService.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        myNewRejectedService.PaidClaimId = null;
                                        myNewRejectedService.RejectedClaimId = myRejectedClaimId;
                                        myNewRejectedService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                        myNewRejectedService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatch) + GetUnitRecordPaidAmountSum(needToAddUnitRecords);
                                        myNewRejectedService.PaidAmount = 0d;

                                        SetServiceRecordStateToAdded(myNewRejectedService);
                                        myNewServices.Add(myNewRejectedService);
                                    }

                                    if (matchedPaidServiceRecord.UnitRecord.Any())
                                    {
                                        //Now deal with the current Paid Claims                 
                                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                        matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                        matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                        matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                        matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        var index = 1;
                                        foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                    }
                                    else
                                    {
                                        DeleteServiceRecord(matchedPaidServiceRecord);
                                    }
                                }

                                _paidClaimNeeded = true;

                                #endregion
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #region Remove used Line items and continue process the rest

                    returnFileUnitRecordList.Clear();

                    foreach(var returnLineItem in myClaimNumberGroup.ReturnLineItems.Where(x =>
                                            (x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE) && x.ApprovedUnitAmount >= 0d))
                    {
                        if (!adjustmentLineContent.Contains(returnLineItem.LineContent))
                        {
                            returnFileUnitRecordList.Add(CreateReturnUnitRecord(returnLineItem));
                        }
                    }

                    #endregion
                }

                if (returnFileUnitRecordList.Any())
                {
                    if (myClaimNumberGroup.PaidType == PAID_TYPE.PAID)
                    {
                        #region Deal with Paid                  

                        //We got Paid claim, we need to check Rejected and Submitted

                        var unitRecordUsedIds = new List<Guid>();

                        //Get existed paid claim
                        var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                   x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                   IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedPaidServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (matchedPaidServiceRecord != null)
                        {
                            UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);
                        }

                        #region Check Rejected Claims

                        //Getting all the rejected claims
                        var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedRejectedServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                        }

                        if (matchedRejectedServiceRecord != null)
                        {
                            UpdateServiceRecordFieldFromReturn(matchedRejectedServiceRecord, myClaimNumberGroup);

                            var systemMessage = string.Empty;

                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (returnUnitRecord.SubmittedAmount > 0d)
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }
                                    
                                    if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                        Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                    {
                                        systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedRejectedServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (matchedRejectedServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All line items

                                //All line items are PAID, return the service record with updated field, convert the rejected to paid

                                _paidClaimNeeded = true;

                                if (matchedPaidServiceRecord == null)
                                {
                                    SetUnitRecordListStateToModified(matchedRejectedServiceRecord.UnitRecord);

                                    //Convert the Rejected to Paid
                                    matchedRejectedServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedRejectedServiceRecord.RejectedClaimId = null;
                                    matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedRejectedServiceRecord.UnitRecord, matchedRejectedServiceRecord.ServiceLocation);
                                    matchedRejectedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                    matchedRejectedServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                    matchedRejectedServiceRecord.MessageFromICS += systemMessage;
                                    matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(matchedRejectedServiceRecord);
                                }
                                else
                                {
                                    //Have Paid Claim for the claim number before, add the unit records to the Paid Claim
                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                        SetUnitRecordStateToModified(unitRecord);
                                    }

                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    DeleteServiceRecord(matchedRejectedServiceRecord);
                                }

                                #endregion
                            }
                            else if (foundUnitRecords.Any())
                            {
                                #region Different Line Items

                                //Different in line items, need to create new Service Record to hold the Paid Line Items and delete the line items from the Rejected Claim
                                //deal with paid claim in BiWeekly Return files

                                if (matchedPaidServiceRecord == null)
                                {
                                    var myNewPaidService = new ServiceRecord();

                                    CopyServiceRecordFields(matchedRejectedServiceRecord, myNewPaidService, myClaimNumberGroup.ClaimNumber);
                                    myNewPaidService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    var index = 1;
                                    foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId; //remap the Unit Record to the New Service Record - Paid
                                        unitRecord.RecordIndex = index;
                                        myNewPaidService.UnitRecord.Add(unitRecord);
                                        SetUnitRecordStateToModified(unitRecord);
                                        index++;
                                    }

                                    myNewPaidService.PaidClaimId = myReturnClaimId;
                                    myNewPaidService.ClaimAmount = GetUnitRecordSubmittedAmountSum(foundUnitRecords, myNewPaidService.ServiceLocation);
                                    myNewPaidService.PaidAmount = GetUnitRecordPaidAmountSum(foundUnitRecords);
                                    myNewPaidService.MessageFromICS += systemMessage;
                                    myNewPaidService.PaymentApproveDate = returnFileUTCDate;

                                    index = 1;
                                    foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                    matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                    SetServiceRecordStateToAdded(myNewPaidService);
                                    myNewServices.Add(myNewPaidService);
                                }
                                else
                                {
                                    //Map the found unit records to matched paid claim
                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                        matchedPaidServiceRecord.UnitRecord.Add(unitRecord);                                        
                                        SetUnitRecordStateToModified(unitRecord);
                                    }

                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                    matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                    index = 1;
                                    foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    _paidClaimNeeded = true;
                                }

                                #endregion
                            }
                        }

                        #endregion

                        #region Submitted / Pending

                        var submittedClaim = myMatchedClaimNumberServiceRecords.FirstOrDefault(x =>
                                    (x.ClaimsInId.HasValue || !string.IsNullOrEmpty(x.CPSClaimNumber)) && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted or Pending                                        
                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (submittedClaim == null)
                        {
                            submittedClaim = myMatchedClaimNumberServiceRecords.FirstOrDefault(x =>
                                    (x.ClaimsInId.HasValue || !string.IsNullOrEmpty(x.CPSClaimNumber)) && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted or Pending                                         
                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (submittedClaim != null)
                        {
                            UpdateServiceRecordFieldFromReturn(submittedClaim, myClaimNumberGroup);

                            var systemMessage = string.Empty;

                            //Check Unit Records to see which ones need to set to Paid from Submitted
                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(submittedClaim.UnitRecord, returnUnitRecord, foundUnitRecords);

                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;

                                    if (returnUnitRecord.SubmittedAmount > 0d)                            
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }
                                                                      
                                    if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                        Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                    {
                                        systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        submittedClaim.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (submittedClaim.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All Items

                                //All line items are PAID, return the service record with updated field
                                _paidClaimNeeded = true;

                                if (matchedPaidServiceRecord == null) //Does not have any paid claim previous with partially paid
                                {
                                    SetUnitRecordListStateToModified(submittedClaim.UnitRecord);

                                    submittedClaim.PaidClaimId = myReturnClaimId;
                                    submittedClaim.ClaimAmount = GetUnitRecordSubmittedAmountSum(submittedClaim.UnitRecord, submittedClaim.ServiceLocation);
                                    submittedClaim.PaidAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                    submittedClaim.PaymentApproveDate = returnFileUTCDate;
                                    submittedClaim.MessageFromICS += systemMessage;
                                    submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(submittedClaim);
                                }
                                else
                                {
                                    //Have Paid Claim for the claim number before, add the unit records to the Paid Claim
                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                        SetUnitRecordStateToModified(unitRecord);
                                    }

                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    DeleteServiceRecord(submittedClaim);
                                }

                                #endregion
                            }
                            else if (foundUnitRecords.Any())
                            {
                                #region Partial Line Items

                                //Different in line items, need to create new Service Record to hold the Piad Line Items and delete the line items from the Submitted Claim
                                //deal with paid claim in BiWeekly Return files

                                if (matchedPaidServiceRecord == null)
                                {
                                    var myNewPaidService = new ServiceRecord();

                                    CopyServiceRecordFields(submittedClaim, myNewPaidService, myClaimNumberGroup.ClaimNumber);

                                    var index = 1;
                                    foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.ServiceRecordId = myNewPaidService.ServiceRecordId;
                                        unitRecord.RecordIndex = index;
                                        myNewPaidService.UnitRecord.Add(unitRecord);
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    myNewPaidService.PaidClaimId = myReturnClaimId;
                                    myNewPaidService.ClaimAmount = GetUnitRecordSubmittedAmountSum(foundUnitRecords, myNewPaidService.ServiceLocation);
                                    myNewPaidService.PaidAmount = GetUnitRecordPaidAmountSum(foundUnitRecords);
                                    myNewPaidService.MessageFromICS += systemMessage;
                                    myNewPaidService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                    myNewPaidService.PaymentApproveDate = returnFileUTCDate;

                                    submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                    submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(submittedClaim);

                                    index = 1;
                                    foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    SetServiceRecordStateToAdded(myNewPaidService);
                                    myNewServices.Add(myNewPaidService);
                                }
                                else
                                {
                                    //Map the found unit records to matched paid claim
                                    foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                                    {
                                        unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                        SetUnitRecordStateToModified(unitRecord);
                                    }

                                    matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    index = 1;
                                    foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                    submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                    SetServiceRecordStateToModified(submittedClaim);
                                    
                                    _paidClaimNeeded = true;

                                }

                                #endregion
                            }
                        }

                        #endregion

                        #region Paid Claims

                        //Check all the Paid Claims and see if any of line items are paid
                        if (matchedPaidServiceRecord != null)
                        {
                            var systemMessage = string.Empty;

                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                                    foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;
                                    foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;

                                    if (returnUnitRecord.SubmittedAmount > 0d)                                  
                                    {
                                        foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                                    }
                                    
                                    if (Math.Round(returnUnitRecord.UnitAmount, 2, MidpointRounding.AwayFromZero) ==
                                        Math.Round(foundExistingUnitRecord.UnitAmount * 0.75d, 2, MidpointRounding.AwayFromZero))
                                    {
                                        systemMessage += foundExistingUnitRecord.UnitCode + " paid 75%. ";
                                    }

                                    if (returnUnitRecord.UnitAmount > 0d)
                                    {
                                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                                    }

                                    if (returnUnitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = returnUnitRecord.UnitCode.Split(',');
                                        foundExistingUnitRecord.UnitCode = splitCode.First();
                                    }

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedPaidServiceRecord.ServiceLocation);
                                        }
                                    }
                               
                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (foundUnitRecords.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);
                                    index++;
                                }

                                //Just need to update the Unit Records In Paid Claim
                                matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                matchedPaidServiceRecord.MessageFromICS += systemMessage;
                                matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                _paidClaimNeeded = true;
                            }
                        }

                        #endregion

                        var notUsedReturnRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId)).ToList();

                        var paidDuplicateModifiedClaims = new List<ServiceRecord>();

                        //Check Number of Paid / Rejected Claims, and merge
                        paidDuplicateModifiedClaims.AddRange(myMatchedClaimNumberServiceRecords.Where(x => x.PaidClaimId == myReturnClaimId &&
                                                    myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

                        paidDuplicateModifiedClaims.AddRange(myNewServices.Where(x => x.PaidClaimId == myReturnClaimId));

                        if (paidDuplicateModifiedClaims.Any())
                        {
                            #region Deal With Not Found List Items

                            _paidClaimNeeded = true;

                            if (notUsedReturnRecords.Any())
                            {
                                //Get the first claim in the list and add not used line items to it
                                var firstPaid = paidDuplicateModifiedClaims.FirstOrDefault();

                                foreach (var unitRecord in notUsedReturnRecords)
                                {
                                    unitRecord.ServiceRecordId = firstPaid.ServiceRecordId;
                                    if (unitRecord.UnitCode.Contains(","))
                                    {
                                        var splitCode = unitRecord.UnitCode.Split(',');
                                        unitRecord.UnitCode = splitCode.First();
                                    }

                                    firstPaid.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToAdded(unitRecord);
                                }

                                firstPaid.PaidClaimId = myReturnClaimId;
                                firstPaid.ClaimAmount = GetUnitRecordSubmittedAmountSum(firstPaid.UnitRecord, firstPaid.ServiceLocation);
                                firstPaid.PaidAmount = GetUnitRecordPaidAmountSum(firstPaid.UnitRecord);
                                firstPaid.PaymentApproveDate = returnFileUTCDate;
                                firstPaid.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            }

                            #endregion

                            #region Merge Duplicate Claims

                            if (paidDuplicateModifiedClaims.Count() > 1)
                            {
                                //Need to merge the Paid Claims together
                                var recordToKeep = paidDuplicateModifiedClaims.ElementAt(0);

                                if (string.IsNullOrEmpty(recordToKeep.MessageFromICS))
                                {
                                    var recordThatHaveMessageICS = paidDuplicateModifiedClaims.FirstOrDefault(x => !string.IsNullOrEmpty(x.MessageFromICS));
                                    if (recordThatHaveMessageICS != null)
                                    {
                                        recordToKeep.MessageFromICS = recordThatHaveMessageICS.MessageFromICS;
                                    }
                                }

                                for (var i = 1; i < paidDuplicateModifiedClaims.Count(); i++)
                                {
                                    var recordToMerge = paidDuplicateModifiedClaims.ElementAt(i);

                                    if (recordToMerge.UnitRecord.Count > 0)
                                    {
                                        do
                                        {
                                            var unitRecordToChange = recordToMerge.UnitRecord.FirstOrDefault();
                                            unitRecordToChange.ServiceRecordId = recordToKeep.ServiceRecordId;
                                            SetUnitRecordStateToModified(unitRecordToChange);

                                        }
                                        while (recordToMerge.UnitRecord.Count() > 0);
                                    }

                                    if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Modified)
                                    {
                                        //When state is modified, then the claim is in the database already
                                        DeleteServiceRecord(recordToMerge);
                                    }
                                    else if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Added)
                                    {
                                        myContext.Entry(recordToMerge).State = System.Data.Entity.EntityState.Unchanged;
                                        myNewServices.Clear();
                                    }
                                }

                                RemoveDuplicateUnitRecords(recordToKeep);

                                recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord, recordToKeep.ServiceLocation);
                                recordToKeep.PaidAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                                recordToKeep.PaymentApproveDate = returnFileUTCDate;
                            }
                            else
                            {
                                var recordToKeep = paidDuplicateModifiedClaims.FirstOrDefault();
                                RemoveDuplicateUnitRecords(recordToKeep);
                                recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord, recordToKeep.ServiceLocation);
                                recordToKeep.PaidAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                                recordToKeep.PaymentApproveDate = returnFileUTCDate;
                            }

                            #endregion
                        }
                        else if (notUsedReturnRecords.Any() && matchedPaidServiceRecord != null)
                        {
                            //Got Rejected Claim come in and got Existed Rejected Claim but not match on the unit record.
                            //This could mean the line item was deleted somehow.
                            foreach (var unitRecord in notUsedReturnRecords)
                            {
                                unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                                if (unitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = unitRecord.UnitCode.Split(',');
                                    unitRecord.UnitCode = splitCode.First();
                                }

                                matchedPaidServiceRecord.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToAdded(unitRecord);
                            }

                            var index = 1;
                            foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                            matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                            matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                            matchedPaidServiceRecord.PaidClaimId = myReturnClaimId;
                            matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                            SetServiceRecordStateToModified(matchedPaidServiceRecord);

                            _paidClaimNeeded = true;
                        }
                        else if (notUsedReturnRecords.Any())
                        {
                            //Need to create Paid Claim to hold the not used Line Items
                            returnFileUnitRecordList.Clear();
                            returnFileUnitRecordList.AddRange(notUsedReturnRecords);

                            needToCreateNewServiceRecord = true;
                        }

                        #endregion
                    }
                    else if (myClaimNumberGroup.PaidType == PAID_TYPE.RETURNED_CLAIMS)
                    {
                        #region Deal with Rejected

                        var unitRecordUsedIds = new List<Guid>();

                        //Getting all the rejected claims
                        var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedRejectedServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) ||
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));
                        }

                        if (matchedRejectedServiceRecord != null)
                        {
                            UpdateServiceRecordFieldFromReturn(matchedRejectedServiceRecord, myClaimNumberGroup);
                        }

                        #region Submitted Claim

                        //We got Rejected Claim, we will check Submitted Claim
                        var submittedClaim = myMatchedClaimNumberServiceRecords.FirstOrDefault(x =>
                                                    (x.ClaimsInId.HasValue || !string.IsNullOrEmpty(x.CPSClaimNumber)) && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted or Pending                                           
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (submittedClaim == null)
                        {
                            //Too restrictive.... just use the HSN
                            submittedClaim = myMatchedClaimNumberServiceRecords.FirstOrDefault(x =>
                                                    (x.ClaimsInId.HasValue || !string.IsNullOrEmpty(x.CPSClaimNumber)) && !x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && //Submitted or Pending                                         
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (submittedClaim != null)
                        {
                            UpdateServiceRecordFieldFromReturn(submittedClaim, myClaimNumberGroup);

                            #region Found Submitted Claim

                            //Check Unit Records to see which ones need to set to Rejected

                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(submittedClaim.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                                    foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                                    foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;
                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        submittedClaim.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (submittedClaim.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All Items Match - Submitted Claim

                                //All line items are rejected, return the service record with updated field

                                _rejectedClaimNeeded = true;

                                if (!continueToProcessClaim)
                                {
                                    DeleteUnitRecords(foundUnitRecords);
                                    DeleteServiceRecord(submittedClaim);
                                }
                                else
                                {
                                    if (matchedRejectedServiceRecord == null)
                                    {
                                        //Convert the submitted claim to rejected, since no rejected claim existed
                                        var index = 1;
                                        foreach(var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) || 
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                submittedClaim.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);
                                                index++;
                                            }
                                        }

                                        if (submittedClaim.UnitRecord.Any())
                                        {                                      
                                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                            {
                                                submittedClaim.MessageFromICS = myClaimNumberGroup.MSBComment;
                                            }

                                            submittedClaim.RejectedClaimId = myReturnClaimId;
                                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);

                                            SetServiceRecordStateToModified(submittedClaim);
                                        }
                                        else
                                        {
                                            DeleteServiceRecord(submittedClaim);
                                        }
                                    }
                                    else
                                    {
                                        //Have existed Rejected Claim, move the unit records to the Rejected Claim
                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                submittedClaim.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                                SetUnitRecordStateToModified(unitRecord);
                                            }
                                        }

                                        var index = 1;
                                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                        matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                        {
                                            matchedRejectedServiceRecord.MessageFromICS += myClaimNumberGroup.MSBComment;
                                        }

                                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                        DeleteServiceRecord(submittedClaim);
                                    }
                                }

                                #endregion
                            }
                            else if (foundUnitRecords.Any())
                            {
                                #region Partial Line Items - Submitted Claim

                                //Only partial line items match, need to map them to the Rejected Claim

                                if (!continueToProcessClaim)
                                {
                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        submittedClaim.UnitRecord.Remove(unitRecord);
                                        SetUnitRecordStateToDeleted(unitRecord);
                                    }

                                    var index = 1;
                                    foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                    submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                    SetServiceRecordStateToModified(submittedClaim);

                                    _rejectedClaimNeeded = true;
                                }
                                else
                                {
                                    if (matchedRejectedServiceRecord != null)
                                    {
                                        #region Existed Rejected Claim - Submitted Claim

                                        _rejectedClaimNeeded = true;

                                        //There is existed Rejected Claim

                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                submittedClaim.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                                SetUnitRecordStateToModified(unitRecord);
                                            }
                                        }

                                        var index = 1;
                                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                        matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                        {
                                            matchedRejectedServiceRecord.MessageFromICS += myClaimNumberGroup.MSBComment;
                                        }

                                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                        if (submittedClaim.UnitRecord.Any())
                                        {
                                            index = 1;
                                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                            SetServiceRecordStateToModified(submittedClaim);
                                        }
                                        else
                                        {
                                            DeleteServiceRecord(submittedClaim);
                                        }

                                        #endregion
                                    }
                                    else
                                    {
                                        #region Create New Rejected Claim - Submitted Claim

                                        //Need to create new Service Record to hold the Rejected Line Items and delete the line items from the Submitted Claim
                                        //deal with rejected claim in Daily Return or BiWeekly Return files

                                        var notMatchUnitRecords = new List<UnitRecord>();

                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                submittedClaim.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                notMatchUnitRecords.Add(unitRecord);
                                            }
                                        }

                                        if (notMatchUnitRecords.Any())
                                        {
                                            var myNewRejectedService = new ServiceRecord();

                                            CopyServiceRecordFields(submittedClaim, myNewRejectedService, myClaimNumberGroup.ClaimNumber);

                                            var index = 1;
                                            foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                                unitRecord.RecordIndex = index;
                                                myNewRejectedService.UnitRecord.Add(unitRecord);
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            myNewRejectedService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                            myNewRejectedService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                            myNewRejectedService.RejectedClaimId = myReturnClaimId;

                                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                            {
                                                myNewRejectedService.MessageFromICS = myClaimNumberGroup.MSBComment;
                                            }

                                            SetServiceRecordStateToAdded(myNewRejectedService);
                                            myNewServices.Add(myNewRejectedService);
                                        }

                                        if (submittedClaim.UnitRecord.Any())
                                        {
                                            var index = 1;
                                            foreach (var unitRecord in submittedClaim.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            submittedClaim.ClaimAmount = GetUnitRecordPaidAmountSum(submittedClaim.UnitRecord);
                                            submittedClaim.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                            SetServiceRecordStateToModified(submittedClaim);
                                        }
                                        else
                                        {
                                            DeleteServiceRecord(submittedClaim);
                                        }

                                        #endregion
                                    }
                                }

                                #endregion
                            }

                            #endregion
                        }

                        #endregion

                        #region Paid Claim
                        //WE got rejected claim, and check any existed paid claim. This is draw back claim from paid to rejected

                        var paidServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    IsStartWith(x.PatientLastName, myClaimNumberGroup.ClaimPatientInfo.LastName));

                        if (matchedPaidServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x =>
                                                    x.HospitalNumber.Equals(myClaimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (matchedPaidServiceRecord != null)
                        {
                            UpdateServiceRecordFieldFromReturn(matchedPaidServiceRecord, myClaimNumberGroup);

                            #region Found Paid Service Records

                            var foundUnitRecords = new List<UnitRecord>();

                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedPaidServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All Line Items - Paid Claim

                                //All lines items are there

                                _rejectedClaimNeeded = true;

                                if (!continueToProcessClaim)
                                {
                                    DeleteUnitRecords(foundUnitRecords);
                                    DeleteServiceRecord(matchedPaidServiceRecord);
                                }
                                else
                                {
                                    if (matchedRejectedServiceRecord == null) //No Existing Rejected Claim
                                    {
                                        //Convert the paid claim to rejected, since no rejected claim existed
                                        var index = 1;
                                        foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);
                                                index++;
                                            }
                                        }

                                        if (matchedPaidServiceRecord.UnitRecord.Any())
                                        {
                                            //There are still line items left and need to convert the claim from Paid to Rejected
                                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                            matchedPaidServiceRecord.PaidAmount = 0d;
                                            matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back. ";

                                            matchedPaidServiceRecord.RejectedClaimId = myReturnClaimId;
                                            matchedPaidServiceRecord.PaidClaimId = null;
                                            matchedPaidServiceRecord.PaymentApproveDate = null;
                                            matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                            {
                                                matchedPaidServiceRecord.MessageFromICS += myClaimNumberGroup.MSBComment;
                                            }

                                            SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                        }
                                        else
                                        {
                                            //No line items left, delete the claim
                                            DeleteServiceRecord(matchedPaidServiceRecord);
                                        }
                                    }
                                    else
                                    {
                                        //There is existed Rejected Claim, move the line items there

                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                                SetUnitRecordStateToModified(unitRecord);
                                            }
                                        }

                                        var index = 1;
                                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                                        matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                        matchedRejectedServiceRecord.MessageFromICS = "System: Payment Adjustments - Claim Payment Drawn Back.";

                                        if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                        {
                                            matchedRejectedServiceRecord.MessageFromICS += myClaimNumberGroup.MSBComment;
                                        }

                                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                        DeleteServiceRecord(matchedPaidServiceRecord);
                                    }
                                }

                                #endregion
                            }
                            else if (foundUnitRecords.Any())
                            {
                                #region Partial Line Items - Paid Claim

                                //Partial line match on Paid

                                if (!continueToProcessClaim) //Claim was set to ignored
                                {
                                    matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments. Claim Payment Drawn Back: " + foundUnitRecords.Sum(x => x.PaidAmount).ToString("C");

                                    foreach (var unitRecord in foundUnitRecords)
                                    {
                                        matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                        SetUnitRecordStateToDeleted(unitRecord);
                                    }

                                    var index = 1;
                                    foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                    matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                                    SetServiceRecordStateToModified(matchedPaidServiceRecord);

                                    _rejectedClaimNeeded = true;
                                }
                                else
                                { 
                                    if (matchedRejectedServiceRecord != null)
                                    {
                                        #region Existed Reject Claim - Paid Claim

                                        //move the line items to existed rejected claims

                                        var drawBackAmoount = 0d;
                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                                SetUnitRecordStateToModified(unitRecord);
                                                drawBackAmoount += unitRecord.PaidAmount;
                                            }
                                        }

                                        var index = 1;
                                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                        matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);

                                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                        if (matchedPaidServiceRecord.UnitRecord.Any())
                                        {
                                            index = 1;
                                            foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                            matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                            matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments. Claim Payment Drawn Back: " + drawBackAmoount.ToString("C");
                                            matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                            matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                            {
                                                matchedPaidServiceRecord.MessageFromICS += System.Environment.NewLine + myClaimNumberGroup.MSBComment;
                                            }

                                            SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                        }
                                        else
                                        {
                                            DeleteServiceRecord(matchedPaidServiceRecord);
                                        }

                                        _rejectedClaimNeeded = true;

                                        #endregion
                                    }
                                    else
                                    {
                                        #region Create New Rejected Claim - Paid Claim

                                        //Create new rejected claim to hold the line items

                                        var notMatchUnitRecords = new List<UnitRecord>();

                                        foreach (var unitRecord in foundUnitRecords)
                                        {
                                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                                UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                                            {
                                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                                SetUnitRecordStateToDeleted(unitRecord);
                                            }
                                            else
                                            {
                                                notMatchUnitRecords.Add(unitRecord);
                                            }
                                        }

                                        if (notMatchUnitRecords.Any())
                                        {                                       
                                            var myNewRejectedService = new ServiceRecord();

                                            CopyServiceRecordFields(matchedPaidServiceRecord, myNewRejectedService, myClaimNumberGroup.ClaimNumber);

                                            var index = 1;
                                            foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.ServiceRecordId = myNewRejectedService.ServiceRecordId;
                                                unitRecord.RecordIndex = index;
                                                myNewRejectedService.UnitRecord.Add(unitRecord);
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            myNewRejectedService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                            myNewRejectedService.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                                            myNewRejectedService.RejectedClaimId = myReturnClaimId;

                                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                            {
                                                myNewRejectedService.MessageFromICS = myClaimNumberGroup.MSBComment;
                                            }

                                            SetServiceRecordStateToAdded(myNewRejectedService);
                                            myNewServices.Add(myNewRejectedService);
                                        }

                                        if (matchedPaidServiceRecord.UnitRecord.Any())
                                        {
                                            var index = 1;
                                            foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                            {
                                                unitRecord.RecordIndex = index;
                                                SetUnitRecordStateToModified(unitRecord);

                                                index++;
                                            }

                                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord, matchedPaidServiceRecord.ServiceLocation);
                                            matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                            matchedPaidServiceRecord.MessageFromICS = "System: Payment Adjustments. Claim Payment Drawn Back: " + 
                                                                                          GetUnitRecordPaidAmountSum(notMatchUnitRecords).ToString("C");
                                            matchedPaidServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                            SetServiceRecordStateToModified(matchedPaidServiceRecord);
                                        }
                                        else
                                        {
                                            DeleteServiceRecord(matchedPaidServiceRecord);
                                        }

                                        #endregion
                                    }
                                }

                                #endregion
                            }

                            #endregion
                        }

                        #endregion

                        #region Rejected Claims - Update Explain Codes

                        //Check all the Rejected Claims and see if any of line items need to update the explain code
                        if (matchedRejectedServiceRecord != null)
                        {
                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in returnFileUnitRecordList)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                                    foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                                    foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                                    foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                                    foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                                    if (foundExistingUnitRecord.UnitPremiumCode != returnUnitRecord.UnitPremiumCode)
                                    {
                                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                                        if (_premiumLocationOfService.Contains(returnUnitRecord.UnitPremiumCode.ToUpper()))
                                        {
                                            foundExistingUnitRecord.PaidAmount = GetTotalWithPremiumAmount(
                                                                                        returnUnitRecord.UnitPremiumCode,
                                                                                        foundExistingUnitRecord.UnitAmount,
                                                                                        returnUnitRecord.UnitCode,
                                                                                        matchedRejectedServiceRecord.ServiceLocation);
                                        }
                                    }

                                    foundUnitRecords.Add(foundExistingUnitRecord);
                                    unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                                }
                            }

                            if (foundUnitRecords.Any())
                            {
                                SetUnitRecordListStateToModified(foundUnitRecords);

                                matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                                matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;

                                if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                                {
                                    matchedRejectedServiceRecord.MessageFromICS += " " + myClaimNumberGroup.MSBComment;
                                }

                                SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                                _rejectedClaimNeeded = true;
                            }
                        }

                        #endregion

                        var notUsedReturnRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId) && 
                                                    !UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, x) && !UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, x)).ToList();

                        var rejectedDuplicateModifiedClaims = new List<ServiceRecord>();

                        //Check Number of Paid / Rejected Claims, and merge
                        rejectedDuplicateModifiedClaims.AddRange(myMatchedClaimNumberServiceRecords.Where(x => x.RejectedClaimId == myReturnClaimId &&
                                                    myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

                        rejectedDuplicateModifiedClaims.AddRange(myNewServices.Where(x => x.RejectedClaimId == myReturnClaimId));

                        if (rejectedDuplicateModifiedClaims.Any())
                        {
                            #region Deal with Not Found Line Items

                            _rejectedClaimNeeded = true;

                            if (notUsedReturnRecords.Any())
                            {
                                //Get the first claim in the list and add not used line items to it
                                var firstRejected = rejectedDuplicateModifiedClaims.FirstOrDefault();

                                foreach (var unitRecord in notUsedReturnRecords)
                                {
                                    unitRecord.ServiceRecordId = firstRejected.ServiceRecordId;
                                    firstRejected.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToAdded(unitRecord);
                                }

                                firstRejected.ClaimAmount = GetUnitRecordPaidAmountSum(firstRejected.UnitRecord);
                                firstRejected.RejectedClaimId = myReturnClaimId;
                                firstRejected.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            }

                            #endregion

                            #region Merge Any Duplicate Rejected Claim

                            if (rejectedDuplicateModifiedClaims.Count() > 1)
                            {
                                //Need to merge the Rejected Claims together
                                var recordToKeep = rejectedDuplicateModifiedClaims.ElementAt(0);

                                if (string.IsNullOrEmpty(recordToKeep.MessageFromICS))
                                {
                                    var recordThatHaveMessageICS = rejectedDuplicateModifiedClaims.FirstOrDefault(x => !string.IsNullOrEmpty(x.MessageFromICS));
                                    if (recordThatHaveMessageICS != null)
                                    {
                                        recordToKeep.MessageFromICS = recordThatHaveMessageICS.MessageFromICS;
                                    }
                                }

                                for (var i = 1; i < rejectedDuplicateModifiedClaims.Count(); i++)
                                {
                                    var recordToMerge = rejectedDuplicateModifiedClaims.ElementAt(i);

                                    if (recordToMerge.UnitRecord.Any())
                                    {
                                        do
                                        {
                                            var unitRecordToChange = recordToMerge.UnitRecord.FirstOrDefault();
                                            unitRecordToChange.ServiceRecordId = recordToKeep.ServiceRecordId;
                                            SetUnitRecordStateToModified(unitRecordToChange);

                                        }
                                        while (recordToMerge.UnitRecord.Count() > 0);
                                    }

                                    if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Modified)
                                    {
                                        //When state is modified, then the claim is in the database already
                                        DeleteServiceRecord(recordToMerge);
                                    }
                                    else if (myContext.Entry(recordToMerge).State == System.Data.Entity.EntityState.Added)
                                    {
                                        myContext.Entry(recordToMerge).State = System.Data.Entity.EntityState.Unchanged;
                                        myNewServices.Clear();
                                    }                               
                                }

                                RemoveDuplicateUnitRecords(recordToKeep);

                                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                            }
                            else
                            {
                                var recordToKeep = rejectedDuplicateModifiedClaims.FirstOrDefault();
                                RemoveDuplicateUnitRecords(recordToKeep);
                                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                            }
                        }
                        else if (notUsedReturnRecords.Any() && matchedRejectedServiceRecord != null)
                        {
                            //Got Rejected Claim come in and got Existed Rejected Claim but not match on the unit record.
                            //This could mean the line item was deleted somehow.
                            foreach (var unitRecord in notUsedReturnRecords)
                            {
                                unitRecord.ServiceRecordId = matchedRejectedServiceRecord.ServiceRecordId;
                                matchedRejectedServiceRecord.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToAdded(unitRecord);
                            }

                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.RejectedClaimId = myReturnClaimId;
                            matchedRejectedServiceRecord.CPSClaimNumber = myClaimNumberGroup.CPSClaimNumber;
                            SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                            var index = 1;
                            foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);
                                
                                index++;
                            }

                            _rejectedClaimNeeded = true;
                        }
                        else if (notUsedReturnRecords.Any())
                        {
                            //Need to create Paid Claim to hold the not used Line Items
                            returnFileUnitRecordList.Clear();
                            returnFileUnitRecordList.AddRange(notUsedReturnRecords);

                            needToCreateNewServiceRecord = true;
                        }

                        #endregion

                        #endregion
                    }
                }

                #region Deal with New Service Record with Old Unit Record -- STUPID!!!!

                if (myNewServices.Any())
                {
                    var record = myNewServices.FirstOrDefault();

                    var newUnitRecordList = new List<UnitRecord>();

                    do
                    {
                        var unitRecord = record.UnitRecord.FirstOrDefault();

                        if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                            myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                        {
                            newUnitRecordList.Add(CloneUnitRecord(unitRecord));
                            SetUnitRecordStateToDeleted(unitRecord);
                        }
                        else if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Added)
                        {
                            newUnitRecordList.Add(unitRecord);
                            record.UnitRecord.Remove(unitRecord);
                        }
                    }
                    while (record.UnitRecord.Count > 0);

                    record.UnitRecord.Clear();

                    foreach (var newUnitRecord in newUnitRecordList)
                    {
                        record.UnitRecord.Add(newUnitRecord);
                    }
                }

                #endregion
            }
            else
            {
                needToCreateNewServiceRecord = true;
            }

            //Cannot find the claim in our data, create new claim
            if (needToCreateNewServiceRecord)
            {
                if (myClaimNumberAndSeqAndAdjustmentList.Any()) //Adjustment
                {
                    #region Adjustment

                    #region CHANGE IN PAID AMOUNT

                    if (myClaimNumberAndSeqAndAdjustmentList.Any(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                    {
                        returnFileUnitRecordList.Clear();

                        foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT))
                        {
                            if (myAdjustment.ApprovedAmount != myAdjustment.DrawbackAmount)
                            {
                                var unitRecord = CreateReturnUnitRecord(myAdjustment.LineItem);
                                if (unitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = unitRecord.UnitCode.Split(',');
                                    unitRecord.UnitCode = splitCode.First();
                                }

                                returnFileUnitRecordList.Add(unitRecord);
                            }
                        }

                        if (returnFileUnitRecordList.Any())
                        {
                            //Need to be Paid Claim
                            var myNewService = new ServiceRecord();
                            myNewService.ServiceRecordId = Guid.NewGuid();

                            SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                            myNewService.PaidClaimId = myReturnClaimId;
                            myNewService.PaymentApproveDate = returnFileUTCDate;

                            foreach (var unitRecord in returnFileUnitRecordList)
                            {
                                unitRecord.ServiceRecordId = myNewService.ServiceRecordId;
                                myNewService.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToAdded(unitRecord);                                
                            }

                            var index = 1;
                            foreach (var unitRecord in myNewService.UnitRecord.OrderBy(x => x.UnitCode))
                            {
                                unitRecord.RecordIndex = index;
                                index++;
                            }

                            myNewService.ClaimAmount = GetUnitRecordPaidAmountSum(myNewService.UnitRecord);
                            myNewService.PaidAmount = myNewService.ClaimAmount;

                            var adjustmentMessage = "System: Payment Adjustments. ";
                            adjustmentMessage += "Change In Paid Amount. ";
                            adjustmentMessage += $"Draw Back: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.DrawbackAmount).ToString("C")}, ";
                            adjustmentMessage += $"Approved: {myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.CHANGE_IN_PAID_AMOUNT).Sum(x => x.ApprovedAmount).ToString("C")}";

                            myNewService.MessageFromICS = adjustmentMessage;

                            SetServiceRecordStateToAdded(myNewService);
                            myNewServices.Add(myNewService);
                        }
                    }

                    #endregion

                    #region Draw Back

                    if (myClaimNumberAndSeqAndAdjustmentList.Any(x => x.AdjustmentType == AdjustmentType.DRAW_BACK) && continueToProcessClaim)
                    {
                        returnFileUnitRecordList.Clear();

                        foreach (var myAdjustment in myClaimNumberAndSeqAndAdjustmentList.Where(x => x.AdjustmentType == AdjustmentType.DRAW_BACK))
                        {
                            var unitRecord = CreateReturnUnitRecord(myAdjustment.LineItem);
                            if (unitRecord.UnitCode.Contains(","))
                            {
                                var splitCode = unitRecord.UnitCode.Split(',');
                                unitRecord.UnitCode = splitCode.First();
                            }

                            if (!UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) && 
                                !UnitRecordContainInPreviousPaidClaim(matchedPreviousPaidClaims, unitRecord))
                            {
                                returnFileUnitRecordList.Add(unitRecord);
                            }
                        }

                        if (returnFileUnitRecordList.Any())
                        {
                            //Need to be Rejected Claim
                            var myNewService = new ServiceRecord();
                            myNewService.ServiceRecordId = Guid.NewGuid();
                            myNewService.PaidClaimId = null;
                            myNewService.RejectedClaimId = myRejectedClaimId;
                            myNewService.PaidAmount = 0d;
                            myNewService.PaymentApproveDate = null;

                            SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                            foreach (var unitRecord in returnFileUnitRecordList)
                            {
                                unitRecord.ServiceRecordId = myNewService.ServiceRecordId;
                                unitRecord.UnitAmount = Math.Abs(unitRecord.UnitAmount);
                                unitRecord.PaidAmount = Math.Abs(unitRecord.PaidAmount);

                                if (unitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = unitRecord.UnitCode.Split(',');
                                    unitRecord.UnitCode = splitCode.First();
                                }

                                myNewService.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToAdded(unitRecord);
                            }

                            var index = 1;
                            foreach (var unitRecord in myNewService.UnitRecord.OrderBy(x => x.UnitCode))
                            {
                                unitRecord.RecordIndex = index;
                                index++;
                            }

                            myNewService.ClaimAmount = GetUnitRecordPaidAmountSum(myNewService.UnitRecord);

                            var adjustmentMessage = "System: Payment Adjustments. ";
                            adjustmentMessage += $"Draw Back: {myNewService.ClaimAmount.ToString("C")}";

                            myNewService.MessageFromICS = adjustmentMessage;

                            SetServiceRecordStateToAdded(myNewService);
                            myNewServices.Add(myNewService);
                        }
                    }

                    #endregion

                    #endregion
                }
                else if (returnFileUnitRecordList.Any())
                {
                    #region Paid / Rejected

                    if (myClaimNumberGroup.PaidType == PAID_TYPE.PAID)
                    {
                        var myNewService = new ServiceRecord();
                        myNewService.ServiceRecordId = Guid.NewGuid();

                        SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                        if (myNewService.DateOfBirth == DateTime.Today)
                        {
                            var foundServiceRecord = myContext.ServiceRecord.Where(x => x.HospitalNumber.Equals(myNewService.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                    x.PatientLastName == myNewService.PatientLastName && x.PatientFirstName == myNewService.PatientFirstName).OrderBy(x => x.CreatedDate).ToList()
                                                    .FirstOrDefault(x => x.DateOfBirth.Date != x.CreatedDate.AddHours(_timeZoneOffset).Date);
                           
                            if (foundServiceRecord != null)
                            {
                                myNewService.DateOfBirth = foundServiceRecord.DateOfBirth;
                                myNewService.Sex = foundServiceRecord.Sex;
                            }
                            
                        }

                        myNewService.ClaimAmount = GetUnitRecordSubmittedAmountSum(returnFileUnitRecordList, myNewService.ServiceLocation);
                        myNewService.PaidAmount = GetUnitRecordPaidAmountSum(returnFileUnitRecordList);

                        myNewService.PaidClaimId = myReturnClaimId;
                        myNewService.PaymentApproveDate = returnFileUTCDate;
                        
                        foreach (var unitRecord in returnFileUnitRecordList)
                        {
                            unitRecord.ServiceRecordId = myNewService.ServiceRecordId;

                            if (unitRecord.UnitCode.Contains(","))
                            {
                                var splitCode = unitRecord.UnitCode.Split(',');
                                unitRecord.UnitCode = splitCode.First();
                            }

                            myNewService.UnitRecord.Add(unitRecord);
                            SetUnitRecordStateToAdded(unitRecord);
                        }

                        SetServiceRecordStateToAdded(myNewService);
                        myNewServices.Add(myNewService);
                    }
                    else if (myClaimNumberGroup.PaidType == PAID_TYPE.RETURNED_CLAIMS && continueToProcessClaim)
                    {
                        var notMatchResubmittedLineItems = GetNotMatchResubmitClaimLineItems(resubmitClaimWithLineItems, returnFileUnitRecordList);
                        var notMatchLineItems = GetNotMatchPreviousPaidClaimLineItems(matchedPreviousPaidClaims, notMatchResubmittedLineItems);

                        if (notMatchLineItems.Any())
                        {
                            //There are line items that need to be created

                            var myNewService = new ServiceRecord();
                            myNewService.ServiceRecordId = Guid.NewGuid();

                            SetServiceRecordFieldsFromClaimGroup(myNewService, myClaimNumberGroup);

                            myNewService.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchLineItems);

                            myNewService.RejectedClaimId = myReturnClaimId;
                            if (!string.IsNullOrEmpty(myClaimNumberGroup.MSBComment))
                            {
                                myNewService.MessageFromICS = myClaimNumberGroup.MSBComment;
                            }

                            foreach (var unitRecord in notMatchLineItems)
                            {
                                unitRecord.ServiceRecordId = myNewService.ServiceRecordId;

                                if (unitRecord.UnitCode.Contains(","))
                                {
                                    var splitCode = unitRecord.UnitCode.Split(',');
                                    unitRecord.UnitCode = splitCode.First();
                                }

                                myNewService.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToAdded(unitRecord);
                            }

                            SetServiceRecordStateToAdded(myNewService);
                            myNewServices.Add(myNewService);
                        }
                    }

                    #endregion
                }
            }

            return myNewServices;
        }

        private UnitRecord GetMatchedUnitRecord(IEnumerable<UnitRecord> existingUnitRecords, UnitRecord returnUnitRecord, IEnumerable<UnitRecord> foundUnitRecords)
        {
            List<UnitRecord> foundCodeMatchedUnitRecords = new List<UnitRecord>();

            var foundUnitRecordIds = foundUnitRecords.Select(x => x.UnitRecordId);

            var remainingUnitRecords = existingUnitRecords.Where(x => !foundUnitRecordIds.Contains(x.UnitRecordId)).ToList();

            var unitCodeSplitList = returnUnitRecord.UnitCode.Split(',');
            if (unitCodeSplitList.Length == 1)
            {
                foundCodeMatchedUnitRecords.AddRange(remainingUnitRecords.Where(x => x.UnitCode == returnUnitRecord.UnitCode).OrderBy(x => x.RecordIndex));
            }
            else if (unitCodeSplitList.Length == 2)
            {
                foundCodeMatchedUnitRecords.AddRange(remainingUnitRecords.Where(x => x.UnitCode == unitCodeSplitList.First()).OrderBy(x => x.RecordIndex));
            }

            if (foundCodeMatchedUnitRecords.Any())
            {
                var foundIndexMatchedUnitRecord = foundCodeMatchedUnitRecords.FirstOrDefault(x => x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode, StringComparison.OrdinalIgnoreCase) && 
                                                        x.UnitNumber == returnUnitRecord.UnitNumber && x.SubmittedRecordIndex == returnUnitRecord.SubmittedRecordIndex);

                if (foundIndexMatchedUnitRecord == null)
                {
                    //Too restrictive
                    foundIndexMatchedUnitRecord = foundCodeMatchedUnitRecords.FirstOrDefault(x => x.UnitNumber == returnUnitRecord.UnitNumber && (x.SubmittedRecordIndex == returnUnitRecord.SubmittedRecordIndex || x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode, StringComparison.OrdinalIgnoreCase)));

                    if (foundIndexMatchedUnitRecord == null)
                    {
                        foundIndexMatchedUnitRecord = foundCodeMatchedUnitRecords.FirstOrDefault(x => x.UnitNumber == returnUnitRecord.UnitNumber);
                    }

                    return foundIndexMatchedUnitRecord;
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
            myNewService.ServiceLocation = myServiceLocation;

            myNewService.Province = myClaimNumberGroup.ClaimPatientInfo.Province;

            myNewService.ServiceDate = myClaimNumberGroup.FirstLineItem.ServiceDate;

            myNewService.Sex = myClaimNumberGroup.ClaimPatientInfo.Sex;
            if (string.IsNullOrEmpty(myNewService.Sex))
            {
                myNewService.Sex = "F";
            }

            myNewService.DateOfBirth = DateTime.Today;

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

            if (string.IsNullOrEmpty(lastName))
            {
                lastName = "Please fill in!";
            }
            
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
            myUnitRecord.OriginalRunCode = myLine.OriginalRunCode;

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
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount(myUnitRecord.UnitPremiumCode, myUnitRecord.UnitAmount, myUnitRecord.UnitCode, myServiceLocation);
                }
                else if (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                {
                    //Returned record
                    myUnitRecord.UnitPremiumCode = "2";
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount("2", myUnitRecord.UnitAmount, myUnitRecord.UnitCode, myServiceLocation);
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

        private UnitRecord CloneUnitRecord(UnitRecord mySource)
        {
            UnitRecord myUnitRecord = new UnitRecord();
            
            myUnitRecord.UnitRecordId = Guid.NewGuid();
            myUnitRecord.ServiceRecordId = mySource.ServiceRecordId;
            myUnitRecord.UnitCode = mySource.UnitCode;
            myUnitRecord.UnitNumber = mySource.UnitNumber;
            myUnitRecord.UnitAmount = mySource.UnitAmount;
            myUnitRecord.UnitPremiumCode = mySource.UnitPremiumCode;

            myUnitRecord.ExplainCode = mySource.ExplainCode;
            myUnitRecord.RecordIndex = mySource.RecordIndex;

            myUnitRecord.PaidAmount = mySource.PaidAmount;
            myUnitRecord.DiagCode = mySource.DiagCode;
            myUnitRecord.RunCode = mySource.RunCode;
            myUnitRecord.OriginalRunCode = mySource.OriginalRunCode;

            myUnitRecord.ExplainCode2 = mySource.ExplainCode2;
            myUnitRecord.ExplainCode3 = mySource.ExplainCode3;

            myUnitRecord.StartTime = mySource.StartTime;
            myUnitRecord.EndTime = mySource.EndTime;

            myUnitRecord.ProgramPayment = mySource.ProgramPayment;
            myUnitRecord.SubmittedAmount = mySource.SubmittedAmount;
            myUnitRecord.SubmittedRecordIndex = mySource.SubmittedRecordIndex;

            myUnitRecord.RecordClaimType = mySource.RecordClaimType;
            myUnitRecord.SpecialCircumstanceIndicator = mySource.SpecialCircumstanceIndicator;
            myUnitRecord.BilateralIndicator = mySource.BilateralIndicator;

            return myUnitRecord;
        }

        private double GetTotalWithPremiumAmount(string locationOfService, double unitAmount, string unitCode, string serviceLocation)
        {
            var result = 0.0d;
            if (!StaticCodeList.MyPremiumCodeList.Contains(unitCode))
            {
                if (locationOfService.Equals("b", StringComparison.OrdinalIgnoreCase))
                {
                    result = Math.Round(0.5d * unitAmount, 2, MidpointRounding.AwayFromZero);
                }
                else if (locationOfService.Equals("k", StringComparison.OrdinalIgnoreCase))
                {
                    result = unitAmount;
                }
                else if (locationOfService.Equals("f", StringComparison.OrdinalIgnoreCase))
                {
                    result = Math.Round(0.1d * unitAmount, 2, MidpointRounding.AwayFromZero);
                }
            }

            var result2 = 0.0d;
            if (!string.IsNullOrEmpty(serviceLocation) && !StaticCodeList.MyRuralAndNorthenPremiumExcludeCodeList.Contains(unitCode) &&
                    serviceLocation.Equals("X", StringComparison.OrdinalIgnoreCase))
            {
                result2 = Math.Round(0.15d * unitAmount, 2, MidpointRounding.AwayFromZero);
            }

            return Math.Round(unitAmount + result + result2, 2, MidpointRounding.AwayFromZero);
        }

        private string CheckWhichExplainCodeToUse(string oldCode, string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
            {
                return newCode;
            }

            return oldCode;
        }

        private IList<ServiceRecord> GetMatchServiceRecords(Guid userId, int claimNumber)
        {
            return myContext.ServiceRecord.Include("UnitRecord").Where(x => x.ClaimNumber == claimNumber && x.UserId == userId && x.ClaimType == 0).OrderByDescending(x => x.CreatedDate).ToList();
        }

        private IList<ServiceRecord> GetMatchPaidServiceRecords(Guid userId, ClaimNumberGroup claimNumberGroup)
        {
            return myContext.ServiceRecord.Include("UnitRecord")
                                .Where(x => x.UserId == userId && x.ServiceDate == claimNumberGroup.FirstLineItem.ServiceDate && x.ClaimType == 0 && 
                                            x.PaidClaimId.HasValue && x.ClaimNumber != claimNumberGroup.ClaimNumber && !x.ClaimToIgnore).ToList()
                                .Where(x => IsStartWith(x.PatientLastName, claimNumberGroup.ClaimPatientInfo.LastName) && 
                                            x.HospitalNumber.Equals(claimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        private IEnumerable<ClaimsResubmitted> GetMatchedClaimResubmitted(Guid userId, ClaimNumberGroup claimNumberGroup)
        {
            return myContext.ClaimsResubmitted.Where(
                        x => x.ClaimNumber == claimNumberGroup.ClaimNumber && x.UserId == userId && x.ServiceDate == claimNumberGroup.FirstLineItem.ServiceDate).ToList()
                        .Where(x => x.HospitalNumber.Equals(claimNumberGroup.ClaimPatientInfo.HospitalNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private bool UnitRecordContainInResubmitClaim(IEnumerable<ClaimsResubmitted> resubmitClaimLineItems, UnitRecord unitRecordToCheck)
        {
            if (resubmitClaimLineItems.Any())
            {
                var unitCodeToUse = unitRecordToCheck.UnitCode;
                var unitCodeSplitList = unitRecordToCheck.UnitCode.Split(',');
                if (unitCodeSplitList.Length == 2)
                {
                    unitCodeToUse = unitCodeSplitList.First();
                }

                var resubmitUnitRecord = resubmitClaimLineItems.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                        x.UnitNumber == unitRecordToCheck.UnitNumber && x.UnitPremiumCode.Equals(unitRecordToCheck.UnitPremiumCode, StringComparison.OrdinalIgnoreCase));

                if (resubmitUnitRecord == null)
                {
                    resubmitUnitRecord = resubmitClaimLineItems.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                        x.UnitNumber == unitRecordToCheck.UnitNumber);

                    if (resubmitUnitRecord == null)
                    {
                        return false;
                    }
                }

                return true;
            }
            
            return false;
        }

        private bool UnitRecordContainInPreviousPaidClaim(IEnumerable<ServiceRecord> previousPaidClaims, UnitRecord unitRecordToCheck)
        {
            if (previousPaidClaims.Any())
            {
                var paidClaimsUnitRecords = previousPaidClaims.SelectMany(x => x.UnitRecord).ToList();

                var unitCodeToUse = unitRecordToCheck.UnitCode;
                var unitCodeSplitList = unitRecordToCheck.UnitCode.Split(',');
                if (unitCodeSplitList.Length == 2)
                {
                    unitCodeToUse = unitCodeSplitList.First();
                }

                var resubmitUnitRecord = paidClaimsUnitRecords.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                        x.UnitNumber == unitRecordToCheck.UnitNumber && x.UnitPremiumCode.Equals(unitRecordToCheck.UnitPremiumCode, StringComparison.OrdinalIgnoreCase));

                if (resubmitUnitRecord == null)
                {
                    resubmitUnitRecord = paidClaimsUnitRecords.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                        x.UnitNumber == unitRecordToCheck.UnitNumber);

                    if (resubmitUnitRecord == null)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private List<UnitRecord> GetNotMatchResubmitClaimLineItems(IEnumerable<ClaimsResubmitted> resubmitClaimLineItems, List<UnitRecord> returnUnitRecords)
        {
            if (resubmitClaimLineItems.Any())
            {
                var newUnitRecordList = new List<UnitRecord>();

                foreach (var returnUnitRecord in returnUnitRecords)
                {
                    var unitCodeToUse = returnUnitRecord.UnitCode;
                    var unitCodeSplitList = returnUnitRecord.UnitCode.Split(',');
                    if (unitCodeSplitList.Length == 2)
                    {
                        unitCodeToUse = unitCodeSplitList.First();
                    }

                    var resubmitUnitRecord = resubmitClaimLineItems.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                            x.UnitNumber == returnUnitRecord.UnitNumber && x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode, StringComparison.OrdinalIgnoreCase));

                    if (resubmitUnitRecord == null)
                    {
                        resubmitUnitRecord = resubmitClaimLineItems.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                            x.UnitNumber == returnUnitRecord.UnitNumber);

                        if (resubmitUnitRecord == null)
                        {
                            newUnitRecordList.Add(returnUnitRecord);
                        }
                    }
                }

                return newUnitRecordList;           
            }
            else
            {
                return returnUnitRecords;
            }
        }

        private List<UnitRecord> GetNotMatchPreviousPaidClaimLineItems(IEnumerable<ServiceRecord> previousPaidClaims, List<UnitRecord> returnUnitRecords)
        {
            if (previousPaidClaims.Any())
            {
                var newUnitRecordList = new List<UnitRecord>();

                var paidClaimsUnitRecords = previousPaidClaims.SelectMany(x => x.UnitRecord).ToList();

                foreach (var returnUnitRecord in returnUnitRecords)
                {
                    var unitCodeToUse = returnUnitRecord.UnitCode;
                    var unitCodeSplitList = returnUnitRecord.UnitCode.Split(',');
                    if (unitCodeSplitList.Length == 2)
                    {
                        unitCodeToUse = unitCodeSplitList.First();
                    }

                    var resubmitUnitRecord = paidClaimsUnitRecords.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                            x.UnitNumber == returnUnitRecord.UnitNumber && x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode, StringComparison.OrdinalIgnoreCase));

                    if (resubmitUnitRecord == null)
                    {
                        resubmitUnitRecord = paidClaimsUnitRecords.FirstOrDefault(x => x.UnitCode.Equals(unitCodeToUse, StringComparison.OrdinalIgnoreCase) &&
                            x.UnitNumber == returnUnitRecord.UnitNumber);

                        if (resubmitUnitRecord == null)
                        {
                            newUnitRecordList.Add(returnUnitRecord);
                        }
                    }
                }

                return newUnitRecordList;
            }
            else
            {
                return returnUnitRecords;
            }
        }

        public int GetCurrentRollOverNumber(Guid userId)
        {
            return myContext.ServiceRecord.Where(x => x.UserId == userId).Select(x => x.RollOverNumber).OrderByDescending(x => x).FirstOrDefault();
        }

        private void DeleteServiceRecord(ServiceRecord serviceRecord)
        {
            myContext.Entry(serviceRecord).State = System.Data.Entity.EntityState.Deleted;
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
            if (myContext.Entry(serviceRecord).State == System.Data.Entity.EntityState.Unchanged)
            {
                myContext.Entry(serviceRecord).State = System.Data.Entity.EntityState.Modified;
            }
        }

        private void SetServiceRecordStateToAdded(ServiceRecord serviceRecord)
        {
            myContext.Entry(serviceRecord).State = System.Data.Entity.EntityState.Added;
        }

        private void SetClaimsReturnPaymentSummaryStateToAdded(ClaimsReturnPaymentSummary record)
        {
            myContext.Entry(record).State = System.Data.Entity.EntityState.Added;
        }

        private void SetUnitRecordStateToAdded(UnitRecord unitRecord)
        {
            myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Added;
        }

        private void SetUnitRecordStateToModified(UnitRecord unitRecord)
        {
            if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
            {
                myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
            }
        }

        private void SetUnitRecordListStateToModified(IEnumerable<UnitRecord> unitRecords)
        {
            foreach (var unitRecord in unitRecords)
            {
                if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                {
                    myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
                }
            }
        }

        private void SetUnitRecordStateToDeleted(UnitRecord unitRecord)
        {
            var ss = myContext.Entry(unitRecord).State;
            if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified || 
                myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged ||
                myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
            }
            else 
            {
                myContext.Entry(unitRecord).State = System.Data.Entity.EntityState.Detached;
            }
        }

        public void GetTotalAmountsFromTotalLines(ClaimsInReturn myClaimsInReturn)
        {
            if (myTotalLines.Any())
            {
                foreach (var myTotal in myTotalLines)
                {
                    if (!myTotal.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase) && !myTotal.TotalLineType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                    {
                        myClaimsInReturn.TotalSubmitted += myTotal.FeeSubmitted;
                        myClaimsInReturn.TotalApproved += myTotal.FeeApproved;
                        myClaimsInReturn.TotalPremiumAmount += myTotal.TotalPremiumAmount;
                        myClaimsInReturn.TotalProgramAmount += myTotal.TotalProgramAmount;
                    }

                    myTotal.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;
                    myClaimsInReturn.ClaimsReturnPaymentSummary.Add(myTotal);
                    SetClaimsReturnPaymentSummaryStateToAdded(myTotal);
                }

                myClaimsInReturn.TotalPaidAmount = myTotalLines.Where(x => x.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase) || x.TotalLineType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase)).Sum(x => x.TotalPaidAmount);

                var runCodeInTotalPaidLine = myTotalLines.FirstOrDefault(x => (x.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase) || x.TotalLineType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(x.RunCode));
                if (runCodeInTotalPaidLine != null)
                {
                    myClaimsInReturn.RunCode = runCodeInTotalPaidLine.RunCode;
                }

                myClaimsInReturn.OtherFeeAndPayment = 0;

            }
        }

        private bool IsStartWith(string source, string target)
        {
            var source1 = Regex.Replace(source, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            var target1 = Regex.Replace(target, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            return source1.StartsWith(target1, StringComparison.OrdinalIgnoreCase);
        }

        private double GetUnitRecordPaidAmountSum(IEnumerable<UnitRecord> records)
        {
            return Math.Round(records.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
        }

        private double GetUnitRecordSubmittedAmountSum(IEnumerable<UnitRecord> records, string serviceLocation)
        {
            return records.Where(x => x.SubmittedAmount.HasValue).Select(x => GetTotalWithPremiumAmount(x.UnitPremiumCode, x.SubmittedAmount.Value, x.UnitCode, serviceLocation)).Sum();
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

        private void UpdateServiceRecordFieldFromReturn(ServiceRecord myExistedService, ClaimNumberGroup claimNumberGroup)
        {
            var changed = false;

            if (claimNumberGroup.PaidType != PAID_TYPE.PAID)
            {
                if (myExistedService.DateOfBirth.Date != claimNumberGroup.ClaimPatientInfo.BirthDate.Date && claimNumberGroup.ClaimPatientInfo.BirthDate != DateTime.Today)
                {
                    myExistedService.DateOfBirth = claimNumberGroup.ClaimPatientInfo.BirthDate.Date;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(claimNumberGroup.ClaimPatientInfo.Sex) && myExistedService.Sex != claimNumberGroup.ClaimPatientInfo.Sex)
                {
                    myExistedService.Sex = claimNumberGroup.ClaimPatientInfo.Sex;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(claimNumberGroup.FirstLineItem.ReferringDoctorNumber) && (string.IsNullOrEmpty(myExistedService.ReferringDoctorNumber) ||
                    myExistedService.ReferringDoctorNumber.Equals(claimNumberGroup.FirstLineItem.ReferringDoctorNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    myExistedService.ReferringDoctorNumber = claimNumberGroup.FirstLineItem.ReferringDoctorNumber;
                    changed = true;
                }
            }

            if (claimNumberGroup.FirstLineItem.ServiceDate != null && myExistedService.ServiceDate.Date != claimNumberGroup.FirstLineItem.ServiceDate.Date)
            {
                myExistedService.ServiceDate = claimNumberGroup.FirstLineItem.ServiceDate.Date;
                changed = true;
            }

            if (claimNumberGroup.ClaimPatientInfo.LastName != null && myExistedService.PatientLastName != claimNumberGroup.ClaimPatientInfo.LastName)
            {
                myExistedService.PatientLastName = claimNumberGroup.ClaimPatientInfo.LastName;
                changed = true;
            }

            if (changed)
            {
                SetServiceRecordStateToModified(myExistedService);
            }
        }

        private void RemoveDuplicateUnitRecords(ServiceRecord serviceRecord)
        {
            var groupByUnitCodes = serviceRecord.UnitRecord.GroupBy(x => new { x.UnitCode, x.UnitPremiumCode, x.UnitNumber }).Select(x => new { UnitCode = x.Key, RecordCount = x.Count(), RecordList = x.ToList() }).ToList();

            foreach(var group in groupByUnitCodes)
            {
                if (group.RecordCount > 1)
                {
                    var recordToKeep = group.RecordList.FirstOrDefault(x => !string.IsNullOrEmpty(x.ExplainCode) && !string.IsNullOrEmpty(x.ExplainCode2) && 
                                            !string.IsNullOrEmpty(x.ExplainCode3));
                    if (recordToKeep == null)
                    {
                        recordToKeep = group.RecordList.FirstOrDefault(x => !string.IsNullOrEmpty(x.ExplainCode) && !string.IsNullOrEmpty(x.ExplainCode2) &&
                                            !string.IsNullOrEmpty(x.ExplainCode3));
                        if (recordToKeep == null)
                        {
                            recordToKeep = group.RecordList.OrderByDescending(x => x.ExplainCode).FirstOrDefault();
                        }
                    }

                    foreach(var recordToRemove in group.RecordList.Where(x => x.UnitRecordId != recordToKeep.UnitRecordId))
                    {
                        SetUnitRecordStateToDeleted(recordToRemove);
                        //serviceRecord.UnitRecord.Remove(recordToRemove);
                    }
                }         
            }

            var index = 1;
            foreach (var unitRecord in serviceRecord.UnitRecord.Where(x => myContext.Entry(x).State != System.Data.Entity.EntityState.Deleted).OrderBy(x => x.SubmittedRecordIndex))
            {
                unitRecord.RecordIndex = index;
                SetUnitRecordStateToModified(unitRecord);

                index++;
            }
        }
        
        public ReturnModel InitialParseResult
        {
            get
            {
                return myReturnModel;
            }
        }

        public GenerateReturnResultModel GenerateReturnClaims(ReturnFileType returnFileType, string returnFileName)
        {
            var result = new GenerateReturnResultModel();
            result.Returns = new List<ClaimsInReturn>();
            result.PerformPendingClaimsSaveChanges = false;

            foreach (var returnContent in myReturnContents)
            {
                ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                myClaimsInReturn.ReturnFooter = string.Empty;
                myClaimsInReturn.ReturnFileType = (int)returnFileType;
                myClaimsInReturn.ReturnFileName = returnFileName;
                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.ReturnFileDate = MBS.RetrieveReturn.Program.GetFileDateTime(returnFileName);
                myClaimsInReturn.TotalApproved = 0;
                myClaimsInReturn.TotalSubmitted = 0;
                myClaimsInReturn.TotalPaidAmount = 0;
                myClaimsInReturn.RunCode = string.Empty;

                GetTotalAmountsFromTotalLines(myClaimsInReturn);

                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.UserId = myUserId;
                myClaimsInReturn.Content = myReturnContentForStorage;

                var returnFileUTCDate = MBS.RetrieveReturn.Program.GetFileDateTime(returnFileName).AddHours(6);

                #region Create Claim return for Paid and Rejected

                if (returnContent.PaidItems.Any() || returnContent.ReturnClaimItems.Any())
                {
                    RejectedClaim myRejectedClaim = new RejectedClaim();
                    myRejectedClaim.RejectedClaimId = Guid.NewGuid();
                    myRejectedClaim.CreatedDate = DateTime.UtcNow;
                    myRejectedClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;

                    if (returnContent.ReturnClaimItems.Count() > 0)
                    {
                        GenerateReturnClaimSerivceRecords(myRejectedClaim, returnFileType, returnContent.ReturnClaimItems, returnFileUTCDate);
                    }

                    PaidClaim myPaidClaim = new PaidClaim();
                    myPaidClaim.PaidClaimId = Guid.NewGuid();
                    myPaidClaim.CreatedDate = DateTime.UtcNow;
                    myPaidClaim.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;

                    if (returnContent.PaidItems.Count() > 0)
                    {
                        GeneratePaidClaimSerivceRecords(myPaidClaim, returnContent.PaidItems, myRejectedClaim, returnFileUTCDate);
                    }

                    if (myRejectedClaim.ServiceRecord.Count() > 0 || _rejectedClaimNeeded)
                    {
                        myClaimsInReturn.RejectedClaim.Add(myRejectedClaim);
                    }

                    if (myPaidClaim.ServiceRecord.Count() > 0 || _paidClaimNeeded)
                    {
                        myClaimsInReturn.PaidClaim.Add(myPaidClaim);
                    }

                    myClaimsInReturn.TotalPaid = myClaimsInReturn.PaidClaim.SelectMany(x => x.ServiceRecord).Count();
                    myClaimsInReturn.TotalRejected = myClaimsInReturn.RejectedClaim.SelectMany(x => x.ServiceRecord).Count();

                }

                #endregion

                #region Pending Claims

                if (returnContent.PendingClaimItems.Any())
                {
                    var updateResult = false;
                    foreach (var claimGroup in returnContent.PendingClaimItems)
                    {
                        if (SetServiceRecordToPending(claimGroup))
                        {
                            updateResult = true;
                        }
                    }

                    result.PerformPendingClaimsSaveChanges = updateResult;

                }

                result.Returns.Add(myClaimsInReturn);

                #endregion
            }

            return result;
        }
    }

    public class PatientInfo
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Province { get; set; }

        public string HospitalNumber { get; set; }

        public DateTime BirthDate { get; set; }

        public string Sex { get; set; }
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

        public int SeqNumber { get; set; }

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

        public string OriginalRunCode { get; set; }
    }

    public class ClaimNumberGroup
    {
        public int ClaimNumber { get; set; }

        public IList<ReturnLineItem> ReturnLineItems { get; set; }

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

        public string CPSClaimNumber { get; set; }
    }

    public class ReturnContent
    {
        public decimal TotalApproved { get; set; }

        public decimal TotalSubmitted { get; set; }

        public IList<ClaimNumberGroup> PaidItems { get; set; }

        public IList<ClaimNumberGroup> ReturnClaimItems { get; set; }

        public IList<ClaimNumberGroup> PendingClaimItems { get; set; }

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

    public enum CLAIM_PROCESS_STATUS
    {
        CONTINUE_TO_PROCESS,
        IGNORED,
        RESUBMITTED_WHOLE,
        RESUBMITTED_NON_MATCH
    }

    public class ClaimNumberAndSeqWithAdjustmentType
    {
        public ReturnLineItem LineItem { get; set; }

        public AdjustmentType AdjustmentType { get; set; }

        public double ApprovedAmount { get; set; }

        public double DrawbackAmount { get; set; }

        public string ExplainCode1 { get; set; }

        public string ExplainCode2 { get; set; }

        public string ExplainCode3 { get; set; }

        public List<string> ReturnLineContent { get; set; }
    }

    public class GenerateReturnResultModel
    {
        public List<ClaimsInReturn> Returns { get; set; }

        public bool PerformPendingClaimsSaveChanges { get; set; }
    }
}
