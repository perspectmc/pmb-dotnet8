using MBS.Common;
using MBS.DataCache;
using MBS.DomainModel;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MBS.TestCodeUsed
{
    public class CleanUpParser
    {
        private MedicalBillingSystemEntities myContext;
        private StringBuilder _logBuilder;
        private int _timeZoneOffset = -6;

        public CleanUpParser(StringBuilder logBuilder)
        {
            _logBuilder = logBuilder;

            try
            {
                myContext = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public string CheckAndFixDuplicateClaims(SimpleUserProfile userProfile, IEnumerable<ReturnLineItem> returnLineItems)
        {       
            if (!myContext.ClaimsInReturn.Any(x => x.UserId == userProfile.UserId))
            {
                return string.Empty;
            } 
            
            var paidClaimId = myContext.PaidClaim.Where(x => x.ClaimsInReturn.UserId == userProfile.UserId).OrderByDescending(x => x.CreatedDate).FirstOrDefault().PaidClaimId;
            var rejectedClaimId = myContext.RejectedClaim.Where(x => x.ClaimsInReturn.UserId == userProfile.UserId).OrderByDescending(x => x.CreatedDate).FirstOrDefault().RejectedClaimId;

            #region Get Claims With More Than One CPS Claim Numbers

            var groupByClaimNumberAndOthers = returnLineItems
                                    .Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE ||
                                            x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE)
                                    .GroupBy(x => new { x.ClaimNumber, x.PatientInfo.HospitalNumber, x.ServiceDate }) //, x.SubmittedUnitCode, x.SubmittedUnitNumber
                                    .Select(x => new
                                    {
                                        ClaimNumber = x.Key.ClaimNumber,
                                        HSN = x.Key.HospitalNumber,
                                        ServiceDate = x.Key.ServiceDate,
                                                //UnitCode = x.Key.SubmittedUnitCode,
                                                //UnitNumber = x.Key.SubmittedUnitNumber,
                                        CPSClaimNumbers = x.Select(y => y.CPSClaimNumber).Distinct().ToList(),
                                        LineItems = x.ToList()
                                    }).ToList();

            var moreThanOneCPSClaimNumbers = groupByClaimNumberAndOthers.Where(x => x.CPSClaimNumbers.Count() > 1).ToList();

            #endregion

            foreach (var group in moreThanOneCPSClaimNumbers)
            {
                WriteInfo($"Working on - Claim #: {group.ClaimNumber}, HSN: {group.HSN}, Service Date: {group.ServiceDate.ToString("yyyy-MM-dd")}");

                var targetCPSNumber = group.CPSClaimNumbers.OrderBy(x => x).FirstOrDefault();
                WriteInfo("Target CPS Number: " + targetCPSNumber);

                var finalLineItems = new List<ReturnLineItem>();

                #region Get CPS Claim Number Line Item for Reprocess

                //Only interested in the earliest CPS Claim Number (the first), and exclude all negative paid claims.                    
                var groupByUnitCodeAndStuff = group.LineItems.Where(x => x.CPSClaimNumber == targetCPSNumber)
                                                .GroupBy(x => new { x.SubmittedUnitCode, x.SubmittedUnitNumber, x.ClaimAndSeqNumber })
                                                .Select(x =>
                                                    new
                                                    {
                                                        UnitCode = x.Key.SubmittedUnitCode,
                                                        UnitNumber = x.Key.SubmittedUnitNumber,
                                                        ClaimNumberAndSeq = x.Key.ClaimAndSeqNumber,
                                                        LatestRunCode = x.OrderByDescending(y => y.RunCode).FirstOrDefault().RunCode,
                                                        LineItems = x.ToList()
                                                    }).ToList();

                foreach (var unitCodeGroup in groupByUnitCodeAndStuff)
                {
                    var runCodeLineItems = unitCodeGroup.LineItems.Where(x => x.RunCode == unitCodeGroup.LatestRunCode);

                    ReturnLineItem finalLineItem = runCodeLineItems.FirstOrDefault();
                    if (runCodeLineItems.Count() == 2)
                    {
                        if (runCodeLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && x.ApprovedUnitAmount < 0) &&
                            runCodeLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE))
                        {
                            finalLineItem = runCodeLineItems.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE);
                        }
                        else if (runCodeLineItems.Count(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID) == 2)
                        {
                            if (runCodeLineItems.Sum(x => x.ApprovedUnitAmount) == 0)
                            {
                                finalLineItem = null;
                            }
                            else
                            {
                                finalLineItem = runCodeLineItems.FirstOrDefault(x => x.ApprovedUnitAmount > 0);
                            }
                        }
                    }
                    else if (runCodeLineItems.Count() > 2)
                    {
                        WriteInfo("ERROR!!!!!!!!!!!!!!!!!!!!!!");
                    }

                    if (finalLineItem != null)
                    {
                        WriteInfo($"Found - Claim Number & Seq: {unitCodeGroup.ClaimNumberAndSeq}, Unit Code: {unitCodeGroup.UnitCode}, Unit #: {unitCodeGroup.UnitNumber}, Run Code:{unitCodeGroup.LatestRunCode}, Type:{finalLineItem.PaidType.ToString()}");
                        finalLineItems.Add(finalLineItem);
                    }
                }

                #endregion

                if (finalLineItems.Any())
                {
                    #region Get Paid, Rejected, Pending Service Record

                    IList<ServiceRecord> serviceRecords = myContext.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.ClaimType == 0 &&
                                            x.ClaimNumber == group.ClaimNumber && x.HospitalNumber == group.HSN && x.ServiceDate == group.ServiceDate).OrderBy(x => x.DateOfBirth).ToList();
                    var continueToProcessClaim = !serviceRecords.Any(x => x.ClaimToIgnore);

                    IList<ClaimsResubmitted> resubmitClaimWithLineItems = myContext.ClaimsResubmitted.Where(
                                        x => x.ClaimNumber == group.ClaimNumber && x.UserId == userProfile.UserId && x.HospitalNumber == group.HSN &&
                                                x.ServiceDate == group.ServiceDate).ToList();

                    var lastName = finalLineItems.FirstOrDefault().PatientInfo.LastName;

                    var previousPaidClaims = myContext.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.HospitalNumber == group.HSN && x.ClaimType == 0 &&
                             x.ServiceDate == group.ServiceDate && x.PaidClaimId.HasValue && x.ClaimNumber != group.ClaimNumber && !x.ClaimToIgnore).ToList()
                             .Where(x => IsStartWith(x.PatientLastName, lastName)).ToList();

                    var paidServiceRecords = serviceRecords.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue).ToList();
                    var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedPaidServiceRecord == null)
                    {
                        matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault();
                    }

                    var rejectedServiceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue).ToList();
                    var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedRejectedServiceRecord == null)
                    {
                        matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault();
                    }

                    var pendingServiceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber)).ToList();
                    var matchedPendingServiceRecord = pendingServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedPendingServiceRecord == null)
                    {
                        matchedPendingServiceRecord = pendingServiceRecords.FirstOrDefault();
                    }

                    #endregion

                    #region Process Line Items

                    var paidLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.PAID).ToList();
                    if (paidLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForPaid(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            paidLineItems,
                            userProfile.UserId,
                            paidClaimId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            paidLineItems.Max(x => x.ReturnFileDate));
                    }

                    var rejectedLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.RETURNED_CLAIMS).ToList();
                    if (rejectedLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForRejected(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            rejectedLineItems,
                            resubmitClaimWithLineItems,
                            previousPaidClaims,
                            userProfile.UserId,
                            rejectedClaimId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            rejectedLineItems.Max(x => x.ReturnFileDate),
                            continueToProcessClaim);
                    }

                    var pendingLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.PENDING_CLAIMS).ToList();
                    if (pendingLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForPending(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            pendingLineItems,
                            resubmitClaimWithLineItems,
                            previousPaidClaims,
                            userProfile.UserId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            continueToProcessClaim);
                    }

                    #endregion
                }
            }

            if (myContext.ChangeTracker.HasChanges())
            {
                myContext.SaveChanges();
            }

            WriteInfo(System.Environment.NewLine);

            return _logBuilder.ToString();
        }
        
        private void CheckAndFixDuplicateClaimsForPaid(
                    ServiceRecord matchedPaidServiceRecord,
                    ServiceRecord matchedRejectedServiceRecord,
                    ServiceRecord matchedPendingServiceRecord,
                    IEnumerable<ReturnLineItem> paidLineItems,
                    Guid myUserId,
                    Guid myPaidClaimId,
                    string myDiagCode,
                    int claimNumber,
                    string CPSClaimNumber,
                    DateTime returnFileUTCDate)
        {
            ServiceRecord myNewServiceRecord = null;
            var returnFileUnitRecordList = new List<UnitRecord>();
            var unitRecordUsedIds = new List<Guid>();
            var needToCreateNewServiceRecord = false;

            foreach (var myLine in paidLineItems)
            {
                returnFileUnitRecordList.Add(CreateReturnUnitRecord(myLine, myDiagCode));
            }

            #region Check Rejected Service Record

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
                        foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                        foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;

                        if (returnUnitRecord.SubmittedAmount > 0d)
                        {
                            foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                        }

                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                        foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                        if (returnUnitRecord.UnitCode.Contains(","))
                        {
                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                            foundExistingUnitRecord.UnitCode = splitCode.First();
                        }

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedRejectedServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All line items

                    //All line items are PAID, return the service record with updated field, convert the rejected to paid
                    if (matchedPaidServiceRecord == null)
                    {
                        SetUnitRecordListStateToModified(matchedRejectedServiceRecord.UnitRecord);

                        //Convert the Rejected to Paid
                        matchedRejectedServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedRejectedServiceRecord.RejectedClaimId = null;
                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedRejectedServiceRecord.UnitRecord);
                        matchedRejectedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                        matchedRejectedServiceRecord.PaymentApproveDate = returnFileUTCDate;
                        matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

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

                        matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                        matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                        matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                        matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

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
                        myNewServiceRecord = new ServiceRecord();

                        CopyServiceRecordFields(matchedRejectedServiceRecord, myNewServiceRecord, claimNumber);
                        myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        var index = 1;
                        foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId; //remap the Unit Record to the New Service Record - Paid
                            unitRecord.RecordIndex = index;
                            myNewServiceRecord.UnitRecord.Add(unitRecord);
                            SetUnitRecordStateToModified(unitRecord);
                            index++;
                        }

                        myNewServiceRecord.PaidClaimId = myPaidClaimId;
                        myNewServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(foundUnitRecords);
                        myNewServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(foundUnitRecords);
                        myNewServiceRecord.PaymentApproveDate = returnFileUTCDate;

                        index = 1;
                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                        matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                        SetServiceRecordStateToAdded(myNewServiceRecord);
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

                        matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
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
                        matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                        index = 1;
                        foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }
                    }

                    #endregion
                }
            }

            #endregion

            #region Check Pending Service Record

            if (matchedPendingServiceRecord != null)
            {
                //Check Unit Records to see which ones need to set to Paid from Submitted
                var foundUnitRecords = new List<UnitRecord>();
                foreach (var returnUnitRecord in returnFileUnitRecordList)
                {
                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPendingServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                    if (foundExistingUnitRecord != null)
                    {
                        foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                        foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                        foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);

                        foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                        foundExistingUnitRecord.PaidAmount = returnUnitRecord.PaidAmount;
                        foundExistingUnitRecord.ProgramPayment = returnUnitRecord.ProgramPayment;

                        if (returnUnitRecord.SubmittedAmount > 0d)
                        {
                            foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                        }

                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;
                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                        foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;

                        if (returnUnitRecord.UnitCode.Contains(","))
                        {
                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                            foundExistingUnitRecord.UnitCode = splitCode.First();
                        }

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedPendingServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All Items

                    //All line items are PAID, return the service record with updated field

                    if (matchedPaidServiceRecord == null) //Does not have any paid claim previous with partially paid
                    {
                        SetUnitRecordListStateToModified(matchedPendingServiceRecord.UnitRecord);

                        matchedPendingServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.PaymentApproveDate = returnFileUTCDate;
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        SetServiceRecordStateToModified(matchedPendingServiceRecord);
                    }
                    else
                    {
                        //Have Paid Claim for the claim number before, add the unit records to the Paid Claim
                        foreach (var unitRecord in foundUnitRecords)
                        {
                            unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                            SetUnitRecordStateToModified(unitRecord);
                        }

                        matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
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

                        DeleteServiceRecord(matchedPendingServiceRecord);
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
                        myNewServiceRecord = new ServiceRecord();

                        CopyServiceRecordFields(matchedPendingServiceRecord, myNewServiceRecord, claimNumber);

                        var index = 1;
                        foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                        {
                            unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;
                            unitRecord.RecordIndex = index;
                            myNewServiceRecord.UnitRecord.Add(unitRecord);
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        myNewServiceRecord.PaidClaimId = myPaidClaimId;
                        myNewServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(foundUnitRecords);
                        myNewServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(foundUnitRecords);
                        myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        myNewServiceRecord.PaymentApproveDate = returnFileUTCDate;

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        SetServiceRecordStateToModified(matchedPendingServiceRecord);

                        index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.RecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        SetServiceRecordStateToAdded(myNewServiceRecord);
                    }
                    else
                    {
                        //Map the found unit records to matched paid claim
                        foreach (var unitRecord in foundUnitRecords.OrderBy(x => x.RecordIndex))
                        {
                            unitRecord.ServiceRecordId = matchedPaidServiceRecord.ServiceRecordId;
                            SetUnitRecordStateToModified(unitRecord);
                        }

                        matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
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
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;

                        SetServiceRecordStateToModified(matchedPendingServiceRecord);
                    }

                    #endregion
                }
            }

            #endregion

            #region Check Paid Service Record

            //Check all the Paid Claims and see if any of line items are paid
            if (matchedPaidServiceRecord != null)
            {
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

                        if (returnUnitRecord.SubmittedAmount > 0d)
                        {
                            foundExistingUnitRecord.SubmittedAmount = returnUnitRecord.SubmittedAmount;
                        }

                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;
                        foundExistingUnitRecord.UnitAmount = returnUnitRecord.UnitAmount;
                        foundExistingUnitRecord.UnitNumber = returnUnitRecord.UnitNumber;
                        foundExistingUnitRecord.UnitPremiumCode = returnUnitRecord.UnitPremiumCode;

                        if (returnUnitRecord.UnitCode.Contains(","))
                        {
                            var splitCode = returnUnitRecord.UnitCode.Split(',');
                            foundExistingUnitRecord.UnitCode = splitCode.First();
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
                    matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                    matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                    matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                    matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                    matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

                    SetServiceRecordStateToModified(matchedPaidServiceRecord);
                }
            }

            #endregion

            #region Deal with Not Found Line Items

            var notUsedReturnRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId)).ToList();

            var paidDuplicateModifiedClaims = new List<ServiceRecord>();

            //Check Number of Paid / Rejected Claims, and merge

            var serviceRecordList = new List<ServiceRecord>();

            if (matchedPaidServiceRecord != null)
            {
                serviceRecordList.Add(matchedPaidServiceRecord);
            }

            if (matchedRejectedServiceRecord != null)
            {
                serviceRecordList.Add(matchedRejectedServiceRecord);
            }

            if (matchedPendingServiceRecord != null)
            {
                serviceRecordList.Add(matchedPendingServiceRecord);
            }

            paidDuplicateModifiedClaims.AddRange(serviceRecordList.Where(x => x.PaidClaimId == myPaidClaimId &&
                                        myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

            if (myNewServiceRecord != null)
            {
                paidDuplicateModifiedClaims.Add(myNewServiceRecord);
            }

            if (paidDuplicateModifiedClaims.Any())
            {
                #region Deal With Not Found List Items

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

                    firstPaid.PaidClaimId = myPaidClaimId;
                    firstPaid.ClaimAmount = GetUnitRecordSubmittedAmountSum(firstPaid.UnitRecord);
                    firstPaid.PaidAmount = GetUnitRecordPaidAmountSum(firstPaid.UnitRecord);
                    firstPaid.PaymentApproveDate = returnFileUTCDate;
                    firstPaid.CPSClaimNumber = CPSClaimNumber;
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
                        }
                    }

                    RemoveDuplicateUnitRecords(recordToKeep);

                    recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord);
                    recordToKeep.PaidAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                    recordToKeep.PaymentApproveDate = returnFileUTCDate;
                }
                else
                {
                    var recordToKeep = paidDuplicateModifiedClaims.FirstOrDefault();
                    RemoveDuplicateUnitRecords(recordToKeep);
                    recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord);
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

                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
                matchedPaidServiceRecord.PaidClaimId = myPaidClaimId;
                matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

                SetServiceRecordStateToModified(matchedPaidServiceRecord);
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

            if (myNewServiceRecord != null)
            {
                var newUnitRecordList = new List<UnitRecord>();

                do
                {
                    var unitRecord = myNewServiceRecord.UnitRecord.FirstOrDefault();

                    if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                        myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                    {
                        newUnitRecordList.Add(CloneUnitRecord(unitRecord));
                        SetUnitRecordStateToDeleted(unitRecord);
                    }
                    else if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Added)
                    {
                        newUnitRecordList.Add(unitRecord);
                        myNewServiceRecord.UnitRecord.Remove(unitRecord);
                    }
                }
                while (myNewServiceRecord.UnitRecord.Count > 0);

                myNewServiceRecord.UnitRecord.Clear();

                foreach (var newUnitRecord in newUnitRecordList)
                {
                    myNewServiceRecord.UnitRecord.Add(newUnitRecord);
                }
            }

            #endregion

            #region Create New Service Records

            if (needToCreateNewServiceRecord)
            {
                myNewServiceRecord = new ServiceRecord();
                myNewServiceRecord.ServiceRecordId = Guid.NewGuid();

                SetServiceRecordFieldsFromClaimGroup(myUserId, myNewServiceRecord, paidLineItems.FirstOrDefault().PatientInfo, claimNumber, CPSClaimNumber, paidLineItems);

                if (myNewServiceRecord.DateOfBirth == DateTime.Today)
                {
                    var foundServiceRecord = myContext.ServiceRecord.Where(x => x.HospitalNumber == myNewServiceRecord.HospitalNumber &&
                                            x.PatientLastName == myNewServiceRecord.PatientLastName && x.PatientFirstName == myNewServiceRecord.PatientFirstName).OrderBy(x => x.CreatedDate).ToList()
                                            .FirstOrDefault(x => x.DateOfBirth.Date != x.CreatedDate.AddHours(_timeZoneOffset).Date);

                    if (foundServiceRecord != null)
                    {
                        myNewServiceRecord.DateOfBirth = foundServiceRecord.DateOfBirth;
                        myNewServiceRecord.Sex = foundServiceRecord.Sex;
                    }

                }

                myNewServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(returnFileUnitRecordList);
                myNewServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(returnFileUnitRecordList);

                myNewServiceRecord.PaidClaimId = myPaidClaimId;
                myNewServiceRecord.PaymentApproveDate = returnFileUTCDate;

                foreach (var unitRecord in returnFileUnitRecordList)
                {
                    unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;

                    if (unitRecord.UnitCode.Contains(","))
                    {
                        var splitCode = unitRecord.UnitCode.Split(',');
                        unitRecord.UnitCode = splitCode.First();
                    }

                    myNewServiceRecord.UnitRecord.Add(unitRecord);
                    SetUnitRecordStateToAdded(unitRecord);
                }

                SetServiceRecordStateToAdded(myNewServiceRecord);
            }

            #endregion
        }

        private void CheckAndFixDuplicateClaimsForRejected(
                ServiceRecord matchedPaidServiceRecord,
                ServiceRecord matchedRejectedServiceRecord,
                ServiceRecord matchedPendingServiceRecord,
                IEnumerable<ReturnLineItem> rejectedLineItems,
                IEnumerable<ClaimsResubmitted> resubmitClaimWithLineItems,
                IEnumerable<ServiceRecord> previousPaidClaims,
                Guid myUserId,
                Guid myRejectedClaimId,
                string myDiagCode,
                int claimNumber,
                string CPSClaimNumber,
                DateTime returnFileUTCDate,
                bool continueToProcessClaim)
        {
            ServiceRecord myNewServiceRecord = null;
            var returnFileUnitRecordList = new List<UnitRecord>();
            var unitRecordUsedIds = new List<Guid>();
            var needToCreateNewServiceRecord = false;

            foreach (var myLine in rejectedLineItems)
            {
                returnFileUnitRecordList.Add(CreateReturnUnitRecord(myLine, myDiagCode));
            }

            #region Check Pending Service Records

            if (matchedPendingServiceRecord != null && myContext.Entry(matchedPendingServiceRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                matchedPendingServiceRecord = null;
            }

            if (matchedRejectedServiceRecord != null && myContext.Entry(matchedRejectedServiceRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                matchedRejectedServiceRecord = null;
            }

            if (matchedPaidServiceRecord != null)
            {
                foreach (var returnUnitRecord in returnFileUnitRecordList)
                {
                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, new List<UnitRecord>());
                    if (foundExistingUnitRecord != null)
                    {
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }
            }

            var notInPaidRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId)).ToList();

            unitRecordUsedIds.Clear();
            returnFileUnitRecordList.Clear();

            returnFileUnitRecordList.AddRange(notInPaidRecords);

            if (!returnFileUnitRecordList.Any())
            {
                return;
            }

            if (matchedPendingServiceRecord != null)
            {
                #region Found Pending Claim

                //Check Unit Records to see which ones need to set to Rejected

                var foundUnitRecords = new List<UnitRecord>();
                foreach (var returnUnitRecord in returnFileUnitRecordList)
                {
                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPendingServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                    if (foundExistingUnitRecord != null)
                    {
                        foundExistingUnitRecord.ExplainCode = returnUnitRecord.ExplainCode;
                        foundExistingUnitRecord.ExplainCode2 = returnUnitRecord.ExplainCode2;
                        foundExistingUnitRecord.ExplainCode3 = returnUnitRecord.ExplainCode3;
                        foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedPendingServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All Items Match - Pending Claim

                    //All line items are rejected, return the service record with updated field

                    if (!continueToProcessClaim)
                    {
                        DeleteUnitRecords(foundUnitRecords);
                        DeleteServiceRecord(matchedPendingServiceRecord);
                    }
                    else
                    {
                        if (matchedRejectedServiceRecord == null)
                        {
                            //Convert the submitted claim to rejected, since no rejected claim existed
                            var index = 1;
                            foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                                {
                                    matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);
                                    index++;
                                }
                            }

                            if (matchedPendingServiceRecord.UnitRecord.Any())
                            {
                                matchedPendingServiceRecord.RejectedClaimId = myRejectedClaimId;
                                matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                                matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);

                                SetServiceRecordStateToModified(matchedPendingServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPendingServiceRecord);
                            }
                        }
                        else
                        {
                            //Have existed Rejected Claim, move the unit records to the Rejected Claim
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) || 
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                                {
                                    matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
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

                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

                            SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                            DeleteServiceRecord(matchedPendingServiceRecord);
                        }
                    }

                    #endregion
                }
                else if (foundUnitRecords.Any())
                {
                    #region Partial Line Items - Pending Claim

                    //Only partial line items match, need to map them to the Rejected Claim

                    if (!continueToProcessClaim)
                    {
                        foreach (var unitRecord in foundUnitRecords)
                        {
                            matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
                            SetUnitRecordStateToDeleted(unitRecord);
                        }

                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);
                    }
                    else
                    {
                        if (matchedRejectedServiceRecord != null)
                        {
                            #region Existed Rejected Claim - Pending Claim

                            //There is existed Rejected Claim

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                                {
                                    matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
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

                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

                            SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                            if (matchedPendingServiceRecord.UnitRecord.Any())
                            {
                                index = 1;
                                foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                                matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;

                                SetServiceRecordStateToModified(matchedPendingServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPendingServiceRecord);
                            }

                            #endregion
                        }
                        else
                        {
                            #region Create New Rejected Claim - Pending Claim

                            //Need to create new Service Record to hold the Rejected Line Items and delete the line items from the Submitted Claim
                            //deal with rejected claim in Daily Return or BiWeekly Return files

                            var notMatchUnitRecords = new List<UnitRecord>();

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                                {
                                    matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }
                                else
                                {
                                    notMatchUnitRecords.Add(unitRecord);
                                }
                            }

                            if (notMatchUnitRecords.Any())
                            {
                                myNewServiceRecord = new ServiceRecord();

                                CopyServiceRecordFields(matchedPendingServiceRecord, myNewServiceRecord, claimNumber);

                                var index = 1;
                                foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;
                                    unitRecord.RecordIndex = index;
                                    myNewServiceRecord.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;
                                myNewServiceRecord.RejectedClaimId = myRejectedClaimId;

                                SetServiceRecordStateToAdded(myNewServiceRecord);
                            }

                            if (matchedPendingServiceRecord.UnitRecord.Any())
                            {
                                var index = 1;
                                foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.RecordIndex = index;
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                                matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                                SetServiceRecordStateToModified(matchedPendingServiceRecord);
                            }
                            else
                            {
                                DeleteServiceRecord(matchedPendingServiceRecord);
                            }

                            #endregion
                        }
                    }

                    #endregion
                }

                #endregion
            }

            #endregion

            #region Check Paid Service Record
            //WE got rejected claim, and check any existed paid claim. This is draw back claim from paid to rejected

            if (false && matchedPaidServiceRecord != null)
            {
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

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All Line Items - Paid Claim

                    //All lines items are there

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
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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

                                matchedPaidServiceRecord.RejectedClaimId = myRejectedClaimId;
                                matchedPaidServiceRecord.PaidClaimId = null;
                                matchedPaidServiceRecord.PaymentApproveDate = null;
                                matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

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
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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

                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                            matchedRejectedServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedRejectedServiceRecord.UnitRecord);
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

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

                        matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                        matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                        matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;

                        SetServiceRecordStateToModified(matchedPaidServiceRecord);
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
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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

                            matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;
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

                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;
                                matchedPaidServiceRecord.PaymentApproveDate = returnFileUTCDate;
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
                            #region Create New Rejected Claim - Paid Claim

                            //Create new rejected claim to hold the line items

                            var notMatchUnitRecords = new List<UnitRecord>();

                            foreach (var unitRecord in foundUnitRecords)
                            {
                                if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                    UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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
                                myNewServiceRecord = new ServiceRecord();

                                CopyServiceRecordFields(matchedPaidServiceRecord, myNewServiceRecord, claimNumber);

                                var index = 1;
                                foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                                {
                                    unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;
                                    unitRecord.RecordIndex = index;
                                    myNewServiceRecord.UnitRecord.Add(unitRecord);
                                    SetUnitRecordStateToModified(unitRecord);

                                    index++;
                                }

                                myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                                myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;
                                myNewServiceRecord.RejectedClaimId = myRejectedClaimId;
                                SetServiceRecordStateToAdded(myNewServiceRecord);
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

                                matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                                matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

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

            #region Check Rejected Claims - Update Explain Codes

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

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (foundUnitRecords.Any())
                {
                    SetUnitRecordListStateToModified(foundUnitRecords);

                    matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                    matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

                    SetServiceRecordStateToModified(matchedRejectedServiceRecord);
                }
            }

            #endregion

            #region Deal with Not Found Line Items

            var notUsedReturnRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId) &&
                                        !UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, x) && !UnitRecordContainInPreviousPaidClaim(previousPaidClaims, x)).ToList();

            var rejectedDuplicateModifiedClaims = new List<ServiceRecord>();

            var serviceRecordList = new List<ServiceRecord>();

            if (matchedPaidServiceRecord != null)
            {
                serviceRecordList.Add(matchedPaidServiceRecord);
            }

            if (matchedRejectedServiceRecord != null)
            {
                serviceRecordList.Add(matchedRejectedServiceRecord);
            }

            if (matchedPendingServiceRecord != null)
            {
                serviceRecordList.Add(matchedPendingServiceRecord);
            }

            //Check Number of Paid / Rejected Claims, and merge
            rejectedDuplicateModifiedClaims.AddRange(serviceRecordList.Where(x => x.RejectedClaimId == myRejectedClaimId &&
                                        myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

            if (myNewServiceRecord != null)
            {
                rejectedDuplicateModifiedClaims.Add(myNewServiceRecord);
            }

            if (rejectedDuplicateModifiedClaims.Any())
            {
                #region Deal with Not Found Line Items

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
                    firstRejected.RejectedClaimId = myRejectedClaimId;
                    firstRejected.CPSClaimNumber = CPSClaimNumber;
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

                #endregion
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
                matchedRejectedServiceRecord.RejectedClaimId = myRejectedClaimId;
                matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;
                SetServiceRecordStateToModified(matchedRejectedServiceRecord);

                var index = 1;
                foreach (var unitRecord in matchedRejectedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                {
                    unitRecord.RecordIndex = index;
                    SetUnitRecordStateToModified(unitRecord);

                    index++;
                }
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

            if (myNewServiceRecord != null && myNewServiceRecord.UnitRecord.Any())
            {
                var newUnitRecordList = new List<UnitRecord>();

                do
                {
                    var unitRecord = myNewServiceRecord.UnitRecord.FirstOrDefault();

                    if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                        myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                    {
                        newUnitRecordList.Add(CloneUnitRecord(unitRecord));
                        SetUnitRecordStateToDeleted(unitRecord);
                    }
                    else if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Added)
                    {
                        newUnitRecordList.Add(unitRecord);
                        myNewServiceRecord.UnitRecord.Remove(unitRecord);
                    }
                }
                while (myNewServiceRecord.UnitRecord.Count > 0);

                myNewServiceRecord.UnitRecord.Clear();

                foreach (var newUnitRecord in newUnitRecordList)
                {
                    myNewServiceRecord.UnitRecord.Add(newUnitRecord);
                }
            }

            #endregion

            #region Create New Service Records

            //if (needToCreateNewServiceRecord && continueToProcessClaim)
            //{
            //    var notMatchResubmittedLineItems = GetNotMatchResubmitClaimLineItems(resubmitClaimWithLineItems, returnFileUnitRecordList);
            //    var notMatchLineItems = GetNotMatchPreviousPaidClaimLineItems(previousPaidClaims, notMatchResubmittedLineItems);
            //    if (notMatchLineItems.Any())
            //    {
            //        //There are line items that need to be created

            //        myNewServiceRecord = new ServiceRecord();
            //        myNewServiceRecord.ServiceRecordId = Guid.NewGuid();

            //        SetServiceRecordFieldsFromClaimGroup(myUserId, myNewServiceRecord, rejectedLineItems.FirstOrDefault().PatientInfo, claimNumber, CPSClaimNumber, rejectedLineItems);

            //        myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchLineItems);

            //        myNewServiceRecord.RejectedClaimId = myRejectedClaimId;

            //        foreach (var unitRecord in notMatchLineItems)
            //        {
            //            unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;

            //            if (unitRecord.UnitCode.Contains(","))
            //            {
            //                var splitCode = unitRecord.UnitCode.Split(',');
            //                unitRecord.UnitCode = splitCode.First();
            //            }

            //            myNewServiceRecord.UnitRecord.Add(unitRecord);
            //            SetUnitRecordStateToAdded(unitRecord);
            //        }

            //        SetServiceRecordStateToAdded(myNewServiceRecord);
            //    }
            //}

            #endregion
        }

        private void CheckAndFixDuplicateClaimsForPending(
                ServiceRecord matchedPaidServiceRecord,
                ServiceRecord matchedRejectedServiceRecord,
                ServiceRecord matchedPendingServiceRecord,
                IEnumerable<ReturnLineItem> pendingLineItems,
                IEnumerable<ClaimsResubmitted> resubmitClaimWithLineItems,
                IEnumerable<ServiceRecord> previousPaidClaims,
                Guid myUserId,
                string myDiagCode,
                int claimNumber,
                string CPSClaimNumber,
                bool continueToProcessClaim)
        {
            ServiceRecord myNewServiceRecord = null;
            var returnFileUnitRecordList = new List<UnitRecord>();
            var unitRecordUsedIds = new List<Guid>();
            var needToCreateNewServiceRecord = false;

            if (!continueToProcessClaim)
            {
                return;
            }

            foreach (var myLine in pendingLineItems)
            {
                returnFileUnitRecordList.Add(CreateReturnUnitRecord(myLine, myDiagCode));
            }

            #region Check Pending Service Record

            if (matchedPendingServiceRecord != null &&  myContext.Entry(matchedPendingServiceRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                matchedPendingServiceRecord = null;
            }

            if (matchedRejectedServiceRecord != null && myContext.Entry(matchedRejectedServiceRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                matchedRejectedServiceRecord = null;
            }

            if (matchedPaidServiceRecord != null)
            {
                foreach (var returnUnitRecord in returnFileUnitRecordList)
                {
                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPaidServiceRecord.UnitRecord, returnUnitRecord, new List<UnitRecord>());
                    if (foundExistingUnitRecord != null)
                    {
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }
            }

            var notInPaidRecords = returnFileUnitRecordList.Where(x => !unitRecordUsedIds.Contains(x.UnitRecordId)).ToList();

            unitRecordUsedIds.Clear();
            returnFileUnitRecordList.Clear();

            returnFileUnitRecordList.AddRange(notInPaidRecords);

            if (!returnFileUnitRecordList.Any())
            {
                return;
            }

            if (matchedPendingServiceRecord != null) //Submitted Claim & Not deleted
            {
                var foundUnitRecords = new List<UnitRecord>();
                foreach (var returnUnitRecord in returnFileUnitRecordList)
                {
                    var foundExistingUnitRecord = GetMatchedUnitRecord(matchedPendingServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);

                    if (foundExistingUnitRecord != null)
                    {
                        foundExistingUnitRecord.ExplainCode = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode, returnUnitRecord.ExplainCode);
                        foundExistingUnitRecord.ExplainCode2 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode2, returnUnitRecord.ExplainCode2);
                        foundExistingUnitRecord.ExplainCode3 = CheckWhichExplainCodeToUse(foundExistingUnitRecord.ExplainCode3, returnUnitRecord.ExplainCode3);
                        foundExistingUnitRecord.RunCode = returnUnitRecord.RunCode;
                        foundExistingUnitRecord.OriginalRunCode = returnUnitRecord.OriginalRunCode;

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (foundUnitRecords.Any()) //Update the unit records
                {
                    foreach (var unitRecord in foundUnitRecords)
                    {
                        if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                            UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                        {
                            matchedPendingServiceRecord.UnitRecord.Remove(unitRecord);
                            SetUnitRecordStateToDeleted(unitRecord);
                        }
                        else
                        {
                            SetUnitRecordStateToModified(unitRecord);
                        }
                    }

                    if (matchedPendingServiceRecord.UnitRecord.Any())
                    {
                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);
                    }
                    else
                    {
                        DeleteServiceRecord(matchedPendingServiceRecord);
                    }
                }
            }

            #endregion

            #region Check Rejected Claims
            //Never get pending claim from. If the claim is paid, it will do a draw back, and then pending. No rejected record.

            if (matchedRejectedServiceRecord != null)
            {
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

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedRejectedServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All Line Items

                    //All lines items are there

                    if (matchedPendingServiceRecord == null)
                    {
                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;
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
                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                            {
                                matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                SetUnitRecordStateToDeleted(unitRecord);
                            }
                            else
                            {
                                unitRecord.ServiceRecordId = matchedPendingServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToModified(unitRecord);
                            }
                        }

                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);

                        DeleteServiceRecord(matchedRejectedServiceRecord);
                    }

                    #endregion
                }
                else if (foundUnitRecords.Any())
                {
                    #region Partial Line Items - Rejected Claim

                    //Partial line match on Rejected, only remap the matched

                    if (matchedPendingServiceRecord != null)
                    {
                        #region Existed Submit Claim - Rejected Claim

                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                            {
                                matchedRejectedServiceRecord.UnitRecord.Remove(unitRecord);
                                SetUnitRecordStateToDeleted(unitRecord);
                            }
                            else
                            {
                                unitRecord.ServiceRecordId = matchedPendingServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToModified(unitRecord);
                            }
                        }

                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);

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
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;
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
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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
                            myNewServiceRecord = new ServiceRecord();

                            CopyServiceRecordFields(matchedRejectedServiceRecord, myNewServiceRecord, claimNumber);

                            var index = 1;
                            foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;
                                unitRecord.RecordIndex = index;
                                myNewServiceRecord.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                            myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;

                            SetServiceRecordStateToAdded(myNewServiceRecord);
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
                            matchedRejectedServiceRecord.CPSClaimNumber = CPSClaimNumber;

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

            if (false && matchedPaidServiceRecord != null)
            {
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

                        foundUnitRecords.Add(foundExistingUnitRecord);
                        unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                    }
                }

                if (matchedPaidServiceRecord.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                {
                    #region All Line Items - Paid Claim

                    //All lines items are there

                    if (matchedPendingServiceRecord == null)
                    {
                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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
                            matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

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
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                            {
                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                SetUnitRecordStateToDeleted(unitRecord);
                            }
                            else
                            {
                                unitRecord.ServiceRecordId = matchedPendingServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToModified(unitRecord);
                            }
                        }

                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);

                        DeleteServiceRecord(matchedPaidServiceRecord);
                    }

                    #endregion
                }
                else if (foundUnitRecords.Any())
                {
                    #region Partial Line Items - Paid Claim

                    //Partial line match on Paid, only remap the matched

                    if (matchedPendingServiceRecord != null)
                    {
                        #region Existed Submit Claim - Paid Claim

                        foreach (var unitRecord in foundUnitRecords)
                        {
                            if (UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, unitRecord) ||
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
                            {
                                matchedPaidServiceRecord.UnitRecord.Remove(unitRecord);
                                SetUnitRecordStateToDeleted(unitRecord);
                            }
                            else
                            {
                                unitRecord.ServiceRecordId = matchedPendingServiceRecord.ServiceRecordId;
                                SetUnitRecordStateToModified(unitRecord);
                            }
                        }

                        var index = 1;
                        foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                        matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;
                        SetServiceRecordStateToModified(matchedPendingServiceRecord);

                        if (matchedPaidServiceRecord.UnitRecord.Any())
                        {
                            index = 1;
                            foreach (var unitRecord in matchedPaidServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.RecordIndex = index;
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                            matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                            matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;
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
                                UnitRecordContainInPreviousPaidClaim(previousPaidClaims, unitRecord))
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
                            myNewServiceRecord = new ServiceRecord();

                            CopyServiceRecordFields(matchedPaidServiceRecord, myNewServiceRecord, claimNumber);

                            var index = 1;
                            foreach (var unitRecord in notMatchUnitRecords.OrderBy(x => x.SubmittedRecordIndex))
                            {
                                unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;
                                unitRecord.RecordIndex = index;
                                myNewServiceRecord.UnitRecord.Add(unitRecord);
                                SetUnitRecordStateToModified(unitRecord);

                                index++;
                            }

                            myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchUnitRecords);
                            myNewServiceRecord.CPSClaimNumber = CPSClaimNumber;

                            SetServiceRecordStateToAdded(myNewServiceRecord);
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

                            matchedPaidServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(matchedPaidServiceRecord.UnitRecord);
                            matchedPaidServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(matchedPaidServiceRecord.UnitRecord);
                            matchedPaidServiceRecord.CPSClaimNumber = CPSClaimNumber;

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
                                            !UnitRecordContainInResubmitClaim(resubmitClaimWithLineItems, x) && !UnitRecordContainInPreviousPaidClaim(previousPaidClaims, x)).ToList();

            var serviceRecordList = new List<ServiceRecord>();

            if (matchedPaidServiceRecord != null)
            {
                serviceRecordList.Add(matchedPaidServiceRecord);
            }

            if (matchedRejectedServiceRecord != null)
            {
                serviceRecordList.Add(matchedRejectedServiceRecord);
            }

            if (matchedPendingServiceRecord != null)
            {
                serviceRecordList.Add(matchedPendingServiceRecord);
            }

            var pendingDuplicateClaims = new List<ServiceRecord>();

            //Check Number of Pending or Rejected Claims, and merge
            pendingDuplicateClaims.AddRange(serviceRecordList.Where(x => x.CPSClaimNumber == CPSClaimNumber &&
                                        !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && myContext.Entry(x).State == System.Data.Entity.EntityState.Modified));

            if (myNewServiceRecord != null)
            {
                pendingDuplicateClaims.Add(myNewServiceRecord);
            }

            if (pendingDuplicateClaims.Any())
            {
                #region Deal With Not Found List Items

                if (notUsedReturnRecords.Any())
                {
                    //Get the first claim in the list and add not used line items to it
                    var firstPending = pendingDuplicateClaims.FirstOrDefault();

                    foreach (var unitRecord in notUsedReturnRecords)
                    {
                        unitRecord.ServiceRecordId = firstPending.ServiceRecordId;
                        firstPending.UnitRecord.Add(unitRecord);
                        SetUnitRecordStateToAdded(unitRecord);
                    }

                    firstPending.ClaimAmount = GetUnitRecordPaidAmountSum(firstPending.UnitRecord);
                    firstPending.CPSClaimNumber = CPSClaimNumber;
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
            else if (notUsedReturnRecords.Any() && matchedPendingServiceRecord != null )
            {
                //Got Pending Claim come in and got Existed Pending Claim but not match on the unit record.
                //This could mean the line item was deleted somehow.
                foreach (var unitRecord in notUsedReturnRecords)
                {
                    unitRecord.ServiceRecordId = matchedPendingServiceRecord.ServiceRecordId;
                    matchedPendingServiceRecord.UnitRecord.Add(unitRecord);
                    SetUnitRecordStateToAdded(unitRecord);
                }

                var index = 1;
                foreach (var unitRecord in matchedPendingServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                {
                    unitRecord.RecordIndex = index;
                    SetUnitRecordStateToModified(unitRecord);

                    index++;
                }

                matchedPendingServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(matchedPendingServiceRecord.UnitRecord);
                matchedPendingServiceRecord.CPSClaimNumber = CPSClaimNumber;

                SetServiceRecordStateToModified(matchedPendingServiceRecord);
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

            if (myNewServiceRecord != null)
            {
                var newUnitRecordList = new List<UnitRecord>();

                do
                {
                    var unitRecord = myNewServiceRecord.UnitRecord.FirstOrDefault();

                    if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                        myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                    {
                        newUnitRecordList.Add(CloneUnitRecord(unitRecord));
                        SetUnitRecordStateToDeleted(unitRecord);
                    }
                    else if (myContext.Entry(unitRecord).State == System.Data.Entity.EntityState.Added)
                    {
                        newUnitRecordList.Add(unitRecord);
                        myNewServiceRecord.UnitRecord.Remove(unitRecord);
                    }
                }
                while (myNewServiceRecord.UnitRecord.Count > 0);

                myNewServiceRecord.UnitRecord.Clear();

                foreach (var newUnitRecord in newUnitRecordList)
                {
                    myNewServiceRecord.UnitRecord.Add(newUnitRecord);
                }
            }

            #endregion

            #region Create New Service Records

            //if (needToCreateNewServiceRecord && continueToProcessClaim)
            //{
            //    var notMatchResubmittedLineItems = GetNotMatchResubmitClaimLineItems(resubmitClaimWithLineItems, returnFileUnitRecordList);
            //    var notMatchLineItems = GetNotMatchPreviousPaidClaimLineItems(previousPaidClaims, notMatchResubmittedLineItems);
            //    if (notMatchLineItems.Any())
            //    {
            //        myNewServiceRecord = new ServiceRecord();
            //        myNewServiceRecord.ServiceRecordId = Guid.NewGuid();
            //        myNewServiceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(notMatchLineItems);

            //        SetServiceRecordFieldsFromClaimGroup(myUserId, myNewServiceRecord, pendingLineItems.FirstOrDefault().PatientInfo, claimNumber, CPSClaimNumber, pendingLineItems);

            //        foreach (var unitRecord in notMatchLineItems)
            //        {
            //            unitRecord.ServiceRecordId = myNewServiceRecord.ServiceRecordId;

            //            myNewServiceRecord.UnitRecord.Add(unitRecord);
            //            SetUnitRecordStateToAdded(unitRecord);
            //        }

            //        SetServiceRecordStateToAdded(myNewServiceRecord);
            //    }
            //}

            #endregion
        }

        public void AdvanceCheckOnClaims(SimpleUserProfile userProfile, IEnumerable<ReturnLineItem> returnLineItems)
        {            
            if (!myContext.ClaimsInReturn.Any(x => x.UserId == userProfile.UserId))
            {
                return;
            }

            var latestClaimsInReturn = myContext.ClaimsInReturn.Where(x => x.UserId == userProfile.UserId).OrderByDescending(x => x.UploadDate).FirstOrDefault();

            #region Get Claims With More Than One CPS Claim Numbers

            var groupByClaimNumberAndHSN = returnLineItems.OrderBy(x => x.ReturnFileDate)
                        .Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID ||
                                x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE ||
                                x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE)
                        .GroupBy(x => new { x.ClaimNumber, x.PatientInfo.HospitalNumber })
                        .Select(x => new
                        {
                            ClaimNumber = x.Key.ClaimNumber,
                            HSN = x.Key.HospitalNumber,
                            ServiceDate = x.Select(y => y.ServiceDate).Distinct().Min(),
                            LineItems = x.ToList()
                        }).ToList();

            var fixReturnLineItems = new List<ReturnLineItem>();

            foreach(var group in groupByClaimNumberAndHSN)
            {
                foreach(var lineItem in group.LineItems)
                {
                    lineItem.ServiceDate = group.ServiceDate;
                    fixReturnLineItems.Add(lineItem);
                }
            }
            
            var groupByClaimNumberAndOthers = fixReturnLineItems
                                    .GroupBy(x => new { x.ClaimNumber, x.PatientInfo.HospitalNumber, x.ServiceDate })
                                    .Select(x => new
                                    {
                                        ClaimNumber = x.Key.ClaimNumber,
                                        HSN = x.Key.HospitalNumber,
                                        ServiceDate = x.Key.ServiceDate,
                                        CPSClaimNumbers = x.Select(y => y.CPSClaimNumber).Distinct().ToList(),
                                        LineItems = x.ToList()
                                    }).ToList();

            #endregion

            foreach (var group in groupByClaimNumberAndOthers)
            {
                WriteInfo(string.Empty);
                WriteInfo($"Working on - Claim #: {group.ClaimNumber}, HSN: {group.HSN}, Service Date: {group.ServiceDate.ToString("yyyy-MM-dd")}");

                var targetCPSNumber = group.CPSClaimNumbers.OrderBy(x => x).FirstOrDefault();
                WriteInfo("Target CPS Number: " + targetCPSNumber);

                var finalLineItems = new List<ReturnLineItem>();

                #region Get CPS Claim Number Line Item for Reprocess

                //Only interested in the earliest CPS Claim Number (the first), and exclude all negative paid claims.                    
                var groupByUnitCodeAndStuff = group.LineItems.Where(x => x.CPSClaimNumber == targetCPSNumber)
                                                .GroupBy(x => new { x.SubmittedUnitCode, x.SubmittedUnitNumber, x.ClaimAndSeqNumber })
                                                .Select(x =>
                                                    new
                                                    {
                                                        UnitCode = x.Key.SubmittedUnitCode,
                                                        UnitNumber = x.Key.SubmittedUnitNumber,
                                                        ClaimNumberAndSeq = x.Key.ClaimAndSeqNumber,
                                                        LatestRunCode = x.OrderByDescending(y => y.RunCode).FirstOrDefault().RunCode,
                                                        LineItems = x.ToList()
                                                    }).ToList();

                foreach (var unitCodeGroup in groupByUnitCodeAndStuff)
                {
                    var runCodeLineItems = unitCodeGroup.LineItems.Where(x => x.RunCode == unitCodeGroup.LatestRunCode);

                    ReturnLineItem finalLineItem = runCodeLineItems.FirstOrDefault();
                    if (runCodeLineItems.Count() == 2)
                    {
                        if (runCodeLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID && x.ApprovedUnitAmount < 0) &&
                            runCodeLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE))
                        {
                            finalLineItem = runCodeLineItems.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE || x.ReturnedRecordType == RETURNED_RECORD_TYPE.VISIT_PROCEDURE);
                        }
                        else if (runCodeLineItems.Count(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.PAID) == 2)
                        {
                            if (runCodeLineItems.Sum(x => x.ApprovedUnitAmount) == 0)
                            {
                                finalLineItem = null;
                            }
                            else
                            {
                                finalLineItem = runCodeLineItems.FirstOrDefault(x => x.ApprovedUnitAmount > 0);
                            }
                        }
                    }
                    else if (runCodeLineItems.Count() > 2)
                    {
                        WriteInfo("ERROR!!!!!!!!!!!!!!!!!!!!!!");
                    }

                    if (finalLineItem != null)
                    {
                        WriteInfo($"Claim # & Seq: {unitCodeGroup.ClaimNumberAndSeq}, Unit C: {unitCodeGroup.UnitCode}, Unit #: {unitCodeGroup.UnitNumber}, Run C:{unitCodeGroup.LatestRunCode}, Type:{finalLineItem.PaidType.ToString()}, File:{finalLineItem.ReturnFileName}");
                        finalLineItems.Add(finalLineItem);
                    }
                }

                #endregion

                if (finalLineItems.Any())
                {
                    #region Get Paid, Rejected, Pending Service Record

                    var paidClaimId = Guid.Empty;
                    var paidClaim = myContext.PaidClaim.Where(x => x.ClaimsInReturn.UserId == userProfile.UserId).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                    if (paidClaim != null)
                    {
                        paidClaimId = paidClaim.PaidClaimId;
                    }
                    else
                    {
                        var newPaidClaim = new PaidClaim();
                        newPaidClaim.ClaimsInReturnId = latestClaimsInReturn.ClaimsInReturnId;
                        newPaidClaim.CreatedDate = DateTime.UtcNow;
                        newPaidClaim.PaidClaimId = Guid.NewGuid();
                        myContext.Entry(newPaidClaim).State = System.Data.Entity.EntityState.Added;

                        paidClaimId = newPaidClaim.PaidClaimId;
                    }
                    

                    var rejectedClaimId = myContext.RejectedClaim.Where(x => x.ClaimsInReturn.UserId == userProfile.UserId).OrderByDescending(x => x.CreatedDate).FirstOrDefault().RejectedClaimId;
                    
                    IList<ServiceRecord> serviceRecords = myContext.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.ClaimType == 0 &&
                                            x.ClaimNumber == group.ClaimNumber && x.HospitalNumber == group.HSN && x.ServiceDate == group.ServiceDate).OrderBy(x => x.DateOfBirth).ToList();
                    var continueToProcessClaim = !serviceRecords.Any(x => x.ClaimToIgnore);

                    IList<ClaimsResubmitted> resubmitClaimWithLineItems = myContext.ClaimsResubmitted.Where(
                                        x => x.ClaimNumber == group.ClaimNumber && x.UserId == userProfile.UserId && x.HospitalNumber == group.HSN &&
                                                x.ServiceDate == group.ServiceDate).ToList();

                    var lastName = finalLineItems.FirstOrDefault().PatientInfo.LastName;

                    var previousPaidClaims = myContext.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.HospitalNumber == group.HSN && x.ClaimType == 0 &&
                             x.ServiceDate == group.ServiceDate && x.PaidClaimId.HasValue && x.ClaimNumber != group.ClaimNumber && !x.ClaimToIgnore).ToList()
                             .Where(x => IsStartWith(x.PatientLastName, lastName)).ToList();

                    var paidServiceRecords = serviceRecords.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue).ToList();
                    var matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedPaidServiceRecord == null)
                    {
                        matchedPaidServiceRecord = paidServiceRecords.FirstOrDefault();
                    }

                    var rejectedServiceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue).ToList();
                    var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedRejectedServiceRecord == null)
                    {
                        matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault();
                    }

                    var pendingServiceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber)).ToList();
                    var matchedPendingServiceRecord = pendingServiceRecords.FirstOrDefault(x => x.CPSClaimNumber == targetCPSNumber);

                    if (matchedPendingServiceRecord == null)
                    {
                        matchedPendingServiceRecord = pendingServiceRecords.FirstOrDefault();
                    }

                    #endregion

                    #region Process Line Items

                    var paidLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.PAID).ToList();
                    if (paidLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForPaid(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            paidLineItems,
                            userProfile.UserId,
                            paidClaimId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            paidLineItems.Max(x => x.ReturnFileDate));
                    }

                    var rejectedLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.RETURNED_CLAIMS).ToList();
                    if (rejectedLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForRejected(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            rejectedLineItems,
                            resubmitClaimWithLineItems,
                            previousPaidClaims,
                            userProfile.UserId,
                            rejectedClaimId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            rejectedLineItems.Max(x => x.ReturnFileDate),
                            continueToProcessClaim);
                    }

                    var pendingLineItems = finalLineItems.Where(x => x.PaidType == PAID_TYPE.PENDING_CLAIMS).ToList();
                    if (pendingLineItems.Any())
                    {
                        CheckAndFixDuplicateClaimsForPending(
                            matchedPaidServiceRecord,
                            matchedRejectedServiceRecord,
                            matchedPendingServiceRecord,
                            pendingLineItems,
                            resubmitClaimWithLineItems,
                            previousPaidClaims,
                            userProfile.UserId,
                            userProfile.DiagnosticCode,
                            group.ClaimNumber,
                            targetCPSNumber,
                            continueToProcessClaim);
                    }

                    #endregion
                }
            }
            
            if (myContext.ChangeTracker.HasChanges())
            {
                myContext.SaveChanges();
            }

            WriteInfo(System.Environment.NewLine);
        }

        private void SetServiceRecordFieldsFromClaimGroup(Guid myUserId, ServiceRecord myNewService, PatientInfo claimPatientInfo, int claimNumber, string CPSClaimNumber, IEnumerable<ReturnLineItem> returnLineItems)
        {
            myNewService.CreatedDate = DateTime.UtcNow;
            myNewService.UserId = myUserId;
            myNewService.ClaimNumber = claimNumber; //Use the claim number in return, set in the above
            myNewService.RollOverNumber = GetCurrentRollOverNumber(myUserId);
            myNewService.PatientLastName = claimPatientInfo.LastName;
            myNewService.PatientFirstName = claimPatientInfo.FirstName;
            myNewService.HospitalNumber = claimPatientInfo.HospitalNumber;
            myNewService.CPSClaimNumber = CPSClaimNumber;

            myNewService.Province = claimPatientInfo.Province;

            myNewService.ServiceDate = returnLineItems.FirstOrDefault().ServiceDate;

            myNewService.Sex = claimPatientInfo.Sex;
            if (string.IsNullOrEmpty(myNewService.Sex))
            {
                myNewService.Sex = "F";
            }

            myNewService.DateOfBirth = DateTime.Today;

            if (claimPatientInfo.BirthDate != null && claimPatientInfo.BirthDate != DateTime.MinValue)
            {
                myNewService.DateOfBirth = claimPatientInfo.BirthDate;
            }

            if (!string.IsNullOrEmpty(returnLineItems.FirstOrDefault().ReferringDoctorNumber))
            {
                myNewService.ReferringDoctorNumber = returnLineItems.FirstOrDefault().ReferringDoctorNumber;
            }

            if (returnLineItems.Any(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE))
            {
                myNewService.DischargeDate = returnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                    .OrderBy(x => x.ClaimAndSeqNumber).FirstOrDefault().LastServiceDate;
            }
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
                        x.UnitNumber == unitRecordToCheck.UnitNumber && x.UnitPremiumCode.Equals(unitRecordToCheck.UnitPremiumCode));

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

        private bool IsStartWith(string source, string target)
        {
            var source1 = Regex.Replace(source, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            var target1 = Regex.Replace(target, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            return source1.StartsWith(target1, StringComparison.OrdinalIgnoreCase);
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
                        x.UnitNumber == unitRecordToCheck.UnitNumber && x.UnitPremiumCode.Equals(unitRecordToCheck.UnitPremiumCode));

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

        private int GetCurrentRollOverNumber(Guid userId)
        {
            return myContext.ServiceRecord.Where(x => x.UserId == userId).Select(x => x.RollOverNumber).OrderByDescending(x => x).FirstOrDefault();
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
                            x.UnitNumber == returnUnitRecord.UnitNumber && x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode));

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
                            x.UnitNumber == returnUnitRecord.UnitNumber && x.UnitPremiumCode.Equals(returnUnitRecord.UnitPremiumCode));

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


        private double GetUnitRecordSubmittedAmountSum(IEnumerable<UnitRecord> records)
        {
            return records.Where(x => x.SubmittedAmount.HasValue).Select(x => GetTotalWithPremiumAmount(x.UnitPremiumCode, x.SubmittedAmount.Value, x.UnitCode)).Sum();
        }

        private double GetTotalWithPremiumAmount(string locationOfService, double unitAmount, string unitCode)
        {
            var result = unitAmount;

            if (!StaticCodeList.MyPremiumCodeList.Contains(unitCode.ToUpper()))
            {
                if (locationOfService.Equals("b", StringComparison.OrdinalIgnoreCase))
                {
                    result = unitAmount * 1.5;
                }
                else if (locationOfService.Equals("k", StringComparison.OrdinalIgnoreCase))
                {
                    result = unitAmount * 2.0;
                }
                else if (locationOfService.Equals("f", StringComparison.OrdinalIgnoreCase))
                {
                    result = unitAmount * 1.1;
                }
            }

            return Math.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        private double GetUnitRecordPaidAmountSum(IEnumerable<UnitRecord> records)
        {
            return Math.Round(records.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
        }


        private void SetServiceRecordState(IEnumerable<ServiceRecord> serviceRecords)
        {
            foreach (var serviceRecord in serviceRecords)
            {
                myContext.ServiceRecord.Add(serviceRecord);
                foreach (var unitRecord in serviceRecord.UnitRecord)
                {
                    myContext.UnitRecord.Add(unitRecord);
                }
            }
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
        
        private DateTime GetFileDateTime(string fileName)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var splitString = fileName.Replace(".txt", string.Empty).Split('_');

            return DateTime.ParseExact(splitString.Last(), "yyyyMMddHHmmss", provider);
        }

        private UnitRecord CreateReturnUnitRecord(ReturnLineItem myLine, string myDiagCode)
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
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount(myUnitRecord.UnitPremiumCode, myUnitRecord.UnitAmount, myUnitRecord.UnitCode);
                }
                else if (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                {
                    //Returned record
                    myUnitRecord.UnitPremiumCode = "2";
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount("2", myUnitRecord.UnitAmount, myUnitRecord.UnitCode);
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

        private string CheckWhichExplainCodeToUse(string oldCode, string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
            {
                return newCode;
            }

            return oldCode;
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

        private void RemoveDuplicateUnitRecords(ServiceRecord serviceRecord)
        {
            var groupByUnitCodes = serviceRecord.UnitRecord.GroupBy(x => new { x.UnitCode, x.UnitPremiumCode, x.UnitNumber }).Select(x => new { UnitCode = x.Key, RecordCount = x.Count(), RecordList = x.ToList() }).ToList();

            foreach (var group in groupByUnitCodes)
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

                    foreach (var recordToRemove in group.RecordList.Where(x => x.UnitRecordId != recordToKeep.UnitRecordId))
                    {
                        SetUnitRecordStateToDeleted(recordToRemove);
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
                var foundIndexMatchedUnitRecord = foundCodeMatchedUnitRecords.FirstOrDefault(x => x.UnitPremiumCode == returnUnitRecord.UnitPremiumCode &&
                                                        x.UnitNumber == returnUnitRecord.UnitNumber && x.SubmittedRecordIndex == returnUnitRecord.SubmittedRecordIndex);
                return foundIndexMatchedUnitRecord;
            }

            return null;
        }

        private void WriteInfo(string message)
        {
            _logBuilder.Append(message).Append(System.Environment.NewLine);
            Console.WriteLine(message);
        }
    }
}
