using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Web.Providers.Entities;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Models;

namespace MBS.Web.Portal.Repositories
{
    public class ServiceRecordRepository : IServiceRecordRepository
    {
        MedicalBillingSystemEntities context = new MedicalBillingSystemEntities();

        public ServiceRecordRepository()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
        }

        public ServiceRecord GetRecord(Guid id, Guid userId)
        {
            return context.ServiceRecord.FirstOrDefault(x => x.UserId == userId && x.ServiceRecordId == id);
        }

        public ServiceRecord GetRecordWithUnitRecords(Guid id, Guid userId)
        {
            return context.ServiceRecord.Include("UnitRecord").FirstOrDefault(x => x.UserId == userId && x.ServiceRecordId == id);
        }

        public IEnumerable<ServiceRecord> GetPaidRecordsWithUnitRecords(Guid userId, int claimNumber)
        {
            return context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userId && x.ClaimNumber == claimNumber && 
                        x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.ClaimToIgnore == false).ToList();
        }

        public ServiceRecord GetRecord(Guid id)
        {
            return context.ServiceRecord.FirstOrDefault(x => x.ServiceRecordId == id);
        }

        public UserProfiles GetUserProfile(Guid userId)
        {
            return context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }

        public void InsertOrUpdate(ServiceRecord serviceRecord)
        {
            if (serviceRecord.ServiceRecordId == Guid.Empty)
            {
                // New entity
                serviceRecord.ServiceRecordId = Guid.NewGuid();
                serviceRecord.CreatedDate = DateTime.UtcNow;
                context.ServiceRecord.Add(serviceRecord);
            }
            else
            {
                // Existing entity
                context.Entry(serviceRecord).State = EntityState.Modified;
                serviceRecord.LastModifiedDate = DateTime.UtcNow;
            }
        }

        public void InsertOrUpdate(UnitRecord unitRecord)
        {
            if (unitRecord.UnitRecordId == Guid.Empty)
            {
                // New entity
                unitRecord.UnitRecordId = Guid.NewGuid();
                context.UnitRecord.Add(unitRecord);
            }
            else
            {
                // Existing entity
                context.Entry(unitRecord).State = EntityState.Modified;
            }
        }

        public void Insert(ClaimsIn claimsIn)
        {
            // New entity
            claimsIn.ClaimsInId = Guid.NewGuid();
            claimsIn.CreatedDate = DateTime.UtcNow;
            context.ClaimsIn.Add(claimsIn);
        }

        public void InsertFax(FaxDeliver faxDeliver)
        {
            faxDeliver.FaxDeliverId = Guid.NewGuid();
            faxDeliver.CreatedDate = DateTime.UtcNow;
            context.FaxDeliver.Add(faxDeliver);
        }

        public void SetServiceRecordToModified(ServiceRecord serviceRecord)
        {
            context.Entry(serviceRecord).State = EntityState.Modified;
        }

        public string GetNextRecordIndex(Guid userId, int timeZoneOffset)
        {
            var nowDate = DateTime.UtcNow;
            var myCount = context.ClaimsIn.Count(x => x.UserId == userId && x.CreatedDate > nowDate.Date && x.CreatedDate < nowDate);

            return nowDate.ToString("yyyy-MM-dd-") + myCount.ToString().PadLeft(2, '0');
        }

        public IEnumerable<ServiceRecord> GetServiceRecordByIds(IEnumerable<Guid> serviceRecordIds)
        {
            return context.ServiceRecord.Where(x => serviceRecordIds.Contains(x.ServiceRecordId) && x.RejectedClaimId.HasValue).ToList();
        }

        public IEnumerable<ServiceRecord> GetWCBServiceRecords(IEnumerable<Guid> serviceRecordIds)
        {
            return context.ServiceRecord.Where(x => serviceRecordIds.Contains(x.ServiceRecordId) && x.ClaimType == (int)ClaimType.WCB).ToList();
        }

        public IEnumerable<SimpleRecord> GetAllSimpleUnSubmittedServiceRecords(Guid userId)
        {
            return context.Database.SqlQuery<SimpleRecord>(
                "SELECT MAX(sr.ServiceRecordId) AS ServiceRecordId, MAX(sr.ClaimNumber) AS ClaimNumber, MAX(sr.HospitalNumber) AS HospitalNumber, MAX(sr.PatientLastName) AS PatientLastName, " +
                "MAX(sr.PatientFirstName) AS PatientFirstName, MAX(sr.ServiceDate) AS ServiceDate, MAX(sr.ClaimAmount) AS ClaimAmount, " +
                "STRING_AGG(ur.UnitCode, ',') AS AllFeeCodes, MAX(sr.CPSClaimNumber) As CPSClaimNumber, MAX(sr.LastModifiedDate) AS LastModifiedDate " +
                "FROM ServiceRecord sr LEFT JOIN UnitRecord ur ON sr.ServiceRecordId = ur.ServiceRecordId " +
                "WHERE sr.UserId = '" + userId + "' AND sr.CPSClaimNumber IS NULL AND sr.RejectedClaimId IS NULL AND sr.ClaimsInId IS NULL AND sr.PaidClaimId IS NULL AND sr.ClaimToIgnore = 0 " +
                "GROUP BY sr.ServiceRecordId " +
                "ORDER BY ClaimNumber DESC "
                ).ToList();
        }

        public IEnumerable<SimpleRecord> GetAllSimpleSubmittedServiceRecords(Guid userId)
        {
            //WCB Claims
            //NEW       WCBFaxStatus = NULL
            //SUBMITTED WCBFaxStatus = NULL OR XXXX AND ClaimsInId IS NOT NULL
            //PAID      WCBFaxStatus = EMPTY AND ClaimsInId != NULL AND PaidClaimId IS NOT NULL

            var timeZoneOffset = MBS.Web.Portal.Services.ConfigHelper.GetTimeZoneOffset();

            return context.Database.SqlQuery<SimpleRecord>(
                "SELECT MAX(sr.ServiceRecordId) AS ServiceRecordId, MAX(sr.ClaimNumber) AS ClaimNumber, MAX(sr.HospitalNumber) AS HospitalNumber, MAX(sr.PatientLastName) AS PatientLastName, " +
                "MAX(sr.PatientFirstName) AS PatientFirstName, MAX(sr.ServiceDate) AS ServiceDate, MAX(sr.ClaimAmount) AS ClaimAmount, " +
                "STRING_AGG(ur.UnitCode, ',') AS AllFeeCodes, MAX(sr.WCBFaxStatus) AS WCBFaxStatus, " +
                "DATEADD(hour, " + timeZoneOffset + ", MAX(ci.CreatedDate)) As SubmissionDate, MAX(sr.CPSClaimNumber) As CPSClaimNumber " +
                "FROM ServiceRecord sr LEFT JOIN UnitRecord ur ON sr.ServiceRecordId = ur.ServiceRecordId LEFT JOIN ClaimsIn ci ON sr.ClaimsInId = ci.ClaimsInId " +
                "WHERE sr.UserId = '" + userId + "' AND sr.CPSClaimNumber IS NULL AND sr.RejectedClaimId IS NULL AND sr.ClaimsInId IS NOT NULL " +
                "AND ((sr.ClaimType = 0 AND sr.PaidClaimId IS NULL) OR (sr.ClaimType = 1 AND (sr.WCBFaxStatus != '' Or sr.WCBFaxStatus IS NULL))) AND sr.ClaimToIgnore = 0 " +
                "GROUP BY sr.ServiceRecordId " +
                "ORDER BY ClaimNumber DESC "
                ).ToList();
        }

        public IEnumerable<SimpleRecord> GetAllSimplePendingServiceRecords(Guid userId)
        {
            var timeZoneOffset = MBS.Web.Portal.Services.ConfigHelper.GetTimeZoneOffset();

            return context.Database.SqlQuery<SimpleRecord>(
                "SELECT MAX(sr.ServiceRecordId) AS ServiceRecordId, MAX(sr.ClaimNumber) AS ClaimNumber, MAX(sr.HospitalNumber) AS HospitalNumber, MAX(sr.PatientLastName) AS PatientLastName, " +
                "MAX(sr.PatientFirstName) AS PatientFirstName, MAX(sr.ServiceDate) AS ServiceDate, MAX(sr.ClaimAmount) AS ClaimAmount, " +
                "CONCAT(STRING_AGG(ur.ExplainCode, ','), ',', STRING_AGG(ur.ExplainCode2, ','), ',', STRING_AGG(ur.ExplainCode3, ',')) AS AllExplainCodes, " +
                "STRING_AGG(ur.UnitCode, ',') AS AllFeeCodes, " +
                "DATEADD(hour, " + timeZoneOffset + ", MAX(ci.CreatedDate)) As SubmissionDate, MAX(sr.CPSClaimNumber) As CPSClaimNumber " +
                "FROM ServiceRecord sr LEFT JOIN UnitRecord ur ON sr.ServiceRecordId = ur.ServiceRecordId LEFT JOIN ClaimsIn ci ON sr.ClaimsInId = ci.ClaimsInId " +
                "WHERE sr.UserId = '" + userId + "' AND sr.CPSClaimNumber IS NOT NULL AND sr.RejectedClaimId IS NULL AND sr.PaidClaimId IS NULL AND sr.ClaimToIgnore = 0" +
                "GROUP BY sr.ServiceRecordId " +
                "ORDER BY ClaimNumber DESC "
                ).ToList();
        }

        public IEnumerable<SimpleRecord> GetAllSimpleRejectedServiceRecords(Guid userId)
        {
            var timeZoneOffset = MBS.Web.Portal.Services.ConfigHelper.GetTimeZoneOffset();

            return context.Database.SqlQuery<SimpleRecord>(
                "SELECT MAX(sr.ServiceRecordId) AS ServiceRecordId, MAX(sr.ClaimNumber) AS ClaimNumber, MAX(sr.HospitalNumber) AS HospitalNumber, MAX(sr.PatientLastName) AS PatientLastName, " +
                "MAX(sr.PatientFirstName) AS PatientFirstName, MAX(sr.ServiceDate) AS ServiceDate, MAX(sr.ClaimAmount) AS ClaimAmount, " +
                "CONCAT(STRING_AGG(ur.ExplainCode, ','), ',', STRING_AGG(ur.ExplainCode2, ','), ',', STRING_AGG(ur.ExplainCode3, ',')) AS AllExplainCodes, " +
                "STRING_AGG(ur.UnitCode, ',') AS AllFeeCodes, " + 
                "DATEADD(hour, " + timeZoneOffset + ", MAX(ci.CreatedDate)) As SubmissionDate, MAX(sr.CPSClaimNumber) As CPSClaimNumber " +
                "FROM ServiceRecord sr LEFT JOIN UnitRecord ur ON sr.ServiceRecordId = ur.ServiceRecordId LEFT JOIN ClaimsIn ci ON sr.ClaimsInId = ci.ClaimsInId " +
                "WHERE sr.UserId = '" + userId + "' AND sr.RejectedClaimId IS NOT NULL AND sr.ClaimToIgnore = 0" +
                "GROUP BY sr.ServiceRecordId " +
                "ORDER BY ClaimNumber DESC "
                ).ToList();
        }

        public string GetMostPopularUsedCodes(Guid userId)
        {
            return context.Database.SqlQuery<string>("SELECT STRING_AGG(B.UnitCode, ',') AS UnitCode FROM (SELECT TOP(50) A.UnitCode FROM " +
                "(SELECT ur.UnitCode, COUNT(ur.UnitCode) As CodeCount from UnitRecord ur LEFT JOIN ServiceRecord sr ON ur.ServiceRecordId = sr.ServiceRecordId WHERE sr.UserId = '" + userId +
                "' GROUP BY ur.UnitCode) AS A ORDER BY CodeCount DESC) AS B").FirstOrDefault();
        }


        public void Save()
        {
            context.SaveChanges();
        }

        public void Delete(ServiceRecord serviceRecord)
        {
            context.Entry(serviceRecord).State = EntityState.Deleted;
            var unitRecords = context.UnitRecord.Where(x => x.ServiceRecordId == serviceRecord.ServiceRecordId);

            foreach (var unitRecord in unitRecords)
            {
                context.Entry(unitRecord).State = EntityState.Deleted;
            }
        }

        public bool IsServiceRecordBelongToUser(Guid userId, Guid serviceRecordId)
        {
            return context.ServiceRecord.Count(x => x.ServiceRecordId == serviceRecordId && x.UserId == userId) > 0;
        }

        public bool IsAccessValidServiceRecords(Guid userId, Guid id, ClaimsInType type)
        {
            var result = 0;

            if (type == ClaimsInType.RejectedClaim || type == ClaimsInType.PaidClaim)
            {
                result = context.ClaimsInReturn.Count(x => x.UserId == userId && x.ClaimsInReturnId == id);
            }
            else
            {
                result = context.ClaimsIn.Count(x => x.UserId == userId && x.ClaimsInId == id);
            }

            return result > 0;
        }

        public NextNumberModel GetNextClaimNumber(Guid userId)
        {
            var model = new NextNumberModel();

            try
            {
                model.RollOverNumber = context.ServiceRecord.Where(x => x.UserId == userId).Select(x => x.RollOverNumber).Max();
                model.NextClaimNumber = context.ServiceRecord.Where(x => x.UserId == userId && x.RollOverNumber == model.RollOverNumber).Select(x => x.ClaimNumber).Max();
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

        public IEnumerable<PatientInfo> GetPatientListUsingHSN(Guid userId, string hsnPrefix)
        {
            var patientInfoList = context.Database.SqlQuery<SearchResult>(
                "SELECT A.DistinctName, SUBSTRING(RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5), PATINDEX('%[^0]%', RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5)+'.'), LEN(RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5))) AS ReferringDocNumber FROM (SELECT CONCAT(ClaimNumber, '|', RIGHT(CONCAT('00000', ReferringDoctorNumber), 5)) AS ClaimNumberReferingDocNumber, (PatientLastName + '|' + PatientFirstName + '|' + HospitalNumber + '|' + RIGHT(CONVERT(char(7), DateOfBirth, 121), 5) + '|' + Sex + '|' + Province) AS DistinctName FROM ServiceRecord WHERE UserId = '" + userId.ToString() + "' AND HospitalNumber LIKE '" + hsnPrefix + "%') AS A GROUP BY A.DistinctName"
                ).ToList();

            return patientInfoList.Select(x => GetPatientInfo(x));
        }

        public IEnumerable<PatientInfo> GetPatientList(Guid userId, string lastNamePrefix)
        {
            var patientInfoList = context.Database.SqlQuery<SearchResult>(
                "SELECT A.DistinctName, SUBSTRING(RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5), PATINDEX('%[^0]%', RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5)+'.'), LEN(RIGHT(MAX(A.ClaimNumberReferingDocNumber), 5))) AS ReferringDocNumber FROM (SELECT CONCAT(ClaimNumber, '|', RIGHT(CONCAT('00000', ReferringDoctorNumber), 5)) AS ClaimNumberReferingDocNumber, (PatientLastName + '|' + PatientFirstName + '|' + HospitalNumber + '|' + RIGHT(CONVERT(char(7), DateOfBirth, 121), 5) + '|' + Sex + '|' + Province) AS DistinctName FROM ServiceRecord WHERE UserId = '" + userId.ToString() + "' AND PatientLastName LIKE '" + lastNamePrefix.Replace("'", "''") + "%') AS A GROUP BY A.DistinctName"
                ).ToList();

            return patientInfoList.Select(x => GetPatientInfo(x));
        }

        private PatientInfo GetPatientInfo(SearchResult patientInfoConCat)
        {
            var info = patientInfoConCat.DistinctName.Split('|');
            var result = new PatientInfo()
            {
                myLastName = info[0],
                myFirstName = info[1],
                myHospitalNumber = info[2],
                myBirthDate = GetBirthDate(info[3]),
                mySex = info[4],
                myProvince = info[5],
                myReferringDocNumber = string.Empty
            };

            result.myReferringDocNumber = patientInfoConCat.ReferringDocNumber;

            return result;
        }

        private string GetBirthDate(string birthDateString)
        {
            var info = birthDateString.Split('-');
            return (info[1] + info[0]);
        }

        public IQueryable<ServiceRecord> GetAllPaidClaimServiceRecords(Guid userId)
        {
            //DO NOT PUT TOLIST() HERE SINCE THERE ARE MORE OPERATIONS TO DO
            return context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userId && x.ClaimToIgnore == false && 
                        ((x.ClaimType == 0 && x.PaidClaimId.HasValue) || (x.ClaimType == 1 && x.WCBFaxStatus == "")));
        }

        public IEnumerable<UnitRecord> GetUnitRecords(Guid serviceRecordId)
        {
            return context.UnitRecord.Where(x => x.ServiceRecordId == serviceRecordId).ToList();
        }

        public IEnumerable<UnitRecord> GetUnitRecords(IEnumerable<Guid> serviceRecordIds)
        {
            return context.UnitRecord.Where(x => serviceRecordIds.Contains(x.ServiceRecordId)).ToList();
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public void DeleteAllUnitRecords(Guid serviceRecordId)
        {
            var unitRecords = context.UnitRecord.Where(x => x.ServiceRecordId == serviceRecordId && x.RecordIndex != 0).ToList();
            foreach (var unitRecord in unitRecords)
            {
                context.Entry(unitRecord).State = EntityState.Deleted;
            }
        }

        public Guid GetReturnId(ServiceRecord serviceRecord)
        {
            if (serviceRecord.PaidClaimId.HasValue)
            {
                return context.PaidClaim.Where(x => x.PaidClaimId == serviceRecord.PaidClaimId.Value).Select(x => x.ClaimsInReturnId).FirstOrDefault();
            }
            else if (serviceRecord.RejectedClaimId.HasValue)
            {
                return context.RejectedClaim.Where(x => x.RejectedClaimId == serviceRecord.RejectedClaimId.Value).Select(x => x.ClaimsInReturnId).FirstOrDefault();
            }

            return Guid.Empty;
        }

        public Guid GetClaimsInReturnId(ServiceRecord serviceRecord)
        {
            if (serviceRecord.PaidClaimId.HasValue)
            {
                return context.PaidClaim.FirstOrDefault(x => x.PaidClaimId == serviceRecord.PaidClaimId.Value).ClaimsInReturnId;
            }
            else if (serviceRecord.RejectedClaimId.HasValue)
            {
                return context.RejectedClaim.FirstOrDefault(x => x.RejectedClaimId == serviceRecord.RejectedClaimId.Value).ClaimsInReturnId;
            }

            return Guid.Empty;
        }

        public void UpdateClaimInReturn(ClaimsInReturn claimsInReturn)
        {
            context.Entry(claimsInReturn).State = EntityState.Modified;
        }

        public void AddPaidClaim(PaidClaim paidClaim)
        {
            context.Entry(paidClaim).State = EntityState.Added;
        }

        public IEnumerable<RejectedClaim> GetRejectedClaims(IEnumerable<Guid> rejectedClaimIds)
        {
            return context.RejectedClaim.Where(x => rejectedClaimIds.Contains(x.RejectedClaimId)).ToList();
        }

        public IEnumerable<ClaimsInReturn> GetClaimsInReturn(IEnumerable<Guid> returnIds)
        {
            return context.ClaimsInReturn.Where(x => returnIds.Contains(x.ClaimsInReturnId)).ToList();
        }

        public void ResetRejectedClaimIdForServiceRecords(IEnumerable<Guid> serviceRecordIds)
        {
            var idList = string.Join("','", serviceRecordIds.Select(x => x.ToString()));
            var query = "UPDATE ServiceRecord Set RejectedClaimId = NULL, ClaimsInId = NULL WHERE ServiceRecordId IN ('" + idList + "');";

            context.Database.ExecuteSqlCommand(query);
        }

        public void UpdateServiceRecordsWithClaimInId(Guid claimsInId, Guid userId, IEnumerable<Guid> serviceRecordIds)
        {
            var idList = string.Join("','", serviceRecordIds.Select(x => x.ToString()));
            var query = "UPDATE ServiceRecord Set ClaimsInId = '" + claimsInId + "' WHERE ServiceRecordId IN ('" + idList + "') AND UserId = '" + userId + "';";

            context.Database.ExecuteSqlCommand(query);
        }

        public void UpdateServiceRecordsWithClaimToIgnore(Guid userId, IEnumerable<Guid> serviceRecordIds)
        {
            var idList = string.Join("','", serviceRecordIds.Select(x => x.ToString()));
            var query = "UPDATE ServiceRecord Set ClaimToIgnore = 1 WHERE ServiceRecordId IN ('" + idList + "') AND UserId = '" + userId + "';";

            context.Database.ExecuteSqlCommand(query);
        }

        public IEnumerable<ServiceRecord> GetUnsubmittedWCBServiceRecords(Guid userId)
        {
            return context.ServiceRecord.Where(x => x.UserId == userId && !x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue &&
                    !x.RejectedClaimId.HasValue && x.ClaimType == (int)ClaimType.WCB).ToList();
        }

        public IEnumerable<ServiceRecord> GetUnsubmittedMSBServiceRecords(Guid userId)
        {
            return context.ServiceRecord.Where(x => x.UserId == userId && !x.ClaimsInId.HasValue && !x.PaidClaimId.HasValue &&
                    !x.RejectedClaimId.HasValue && x.ClaimType == (int)ClaimType.MSB && x.CPSClaimNumber == null && x.ClaimToIgnore == false).ToList();
        }

        public IEnumerable<FaxDeliver> WCBClaimsNeedToReSubmit(Guid userId, int timeZoneOffset)
        {
            var targetDate = DateTime.UtcNow.AddDays(-1);
            return context.FaxDeliver.Where(x => x.UserId == userId && x.Status == (int)DeliverStatus.FAIL ||
                    (x.Status == (int)DeliverStatus.PENDING && x.CreatedDate < targetDate)).ToList();
        }

        public void DeleteDeliver(FaxDeliver faxDeliver)
        {
            context.Entry(faxDeliver).State = EntityState.Deleted;
        }

        public void ConvertWCBToPaid(Guid serviceRecordId, Guid paidClaimId, double paidAmount)
        {
            var query = string.Format("UPDATE ServiceRecord Set PaidClaimId = '{0}', PaidAmount = '{1}', WCBFaxStatus = 'Received by WCB' WHERE ServiceRecordId = '{2}'",
                            paidClaimId, paidAmount, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);
        }

        public void SetRelatedFaxDeliver(Guid serviceRecordId, DeliverStatus status)
        {
            var query = string.Format("UPDATE FaxDeliver Set Status = '{0}' WHERE ServiceRecordId = '{1}'", (int)status, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);
        }

        public void InsertClaimInReturn(ClaimsInReturn claimsInReturn)
        {
            context.ClaimsInReturn.Add(claimsInReturn);
        }

        public void InsertPaidClaim(PaidClaim paidClaim)
        {
            context.PaidClaim.Add(paidClaim);
        }

        public void ResetSubmittedServiceRecord(Guid serviceRecordId, int rollOverNumber, int newClaimNumber)
        {
            var query = string.Format("UPDATE ServiceRecord Set RejectedClaimId = NULL, ClaimsInId = NULL, PaidClaimId = NULL, CPSClaimNumber = NULL, ClaimNumber = '{0}', RollOverNumber='{1}' WHERE ServiceRecordId = '{2}'", newClaimNumber, rollOverNumber, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);

            var query2 = string.Format("UPDATE UnitRecord SET SubmittedRecordIndex = RecordIndex WHERE ServiceRecordId = '{0}'", serviceRecordId);
            context.Database.ExecuteSqlCommand(query2);
        }

        public void ResetSubmittedServiceRecord(Guid userId, Guid serviceRecordId, int rollOverNumber, int newClaimNumber)
        {
            var query = string.Format("UPDATE ServiceRecord Set RejectedClaimId = NULL, ClaimsInId = NULL, PaidClaimId = NULL, CPSClaimNumber = NULL, ClaimNumber = '{0}', RollOverNumber='{1}' WHERE ServiceRecordId = '{2}' AND UserId = '{3}'", newClaimNumber, rollOverNumber, serviceRecordId, userId);
            context.Database.ExecuteSqlCommand(query);

            var query2 = string.Format("UPDATE UnitRecord SET SubmittedRecordIndex = RecordIndex WHERE ServiceRecordId = '{0}'", serviceRecordId);
            context.Database.ExecuteSqlCommand(query2);
        }

        public void ResetSubmittedWCBServiceRecord(Guid serviceRecordId, int rollOverNumber, int newClaimNumber)
        {
            var query = string.Format("UPDATE ServiceRecord Set ClaimType = 1, RejectedClaimId = NULL, ClaimsInId = NULL, PaidClaimId = NULL, ClaimNumber = '{0}', RollOverNumber='{1}' WHERE ServiceRecordId = '{2}'", newClaimNumber, rollOverNumber, serviceRecordId);
            context.Database.ExecuteSqlCommand(query);
        }

        public void ConvertRejectedToPaid(Guid serviceRecordId, Guid paidClaimId, double claimAmount, double paidAmount)
        {
            var query = string.Format("UPDATE ServiceRecord Set RejectedClaimId = NULL, PaidClaimId = '{0}', ClaimAmount = {2}, PaidAmount = {3}, PaymentApproveDate = GETUTCDATE() WHERE ServiceRecordId = '{1}'", paidClaimId, serviceRecordId, claimAmount, paidAmount);
            context.Database.ExecuteSqlCommand(query);
        }

        public FaxDeliver GetFaxDeliver(Guid serviceRecordId)
        {
            return context.FaxDeliver.FirstOrDefault(x => x.ServiceRecordId == serviceRecordId);
        }

        public IEnumerable<FaxDeliver> GetPendingFaxes()
        {
            return context.FaxDeliver.Where(x => x.Status == (int)DeliverStatus.PENDING).ToList();
        }

        public void UpdateFaxDeliver(FaxDeliver record)
        {
            context.Entry(record).State = EntityState.Modified;
        }

        private IEnumerable<PatientInfo> DistinctInfo(IEnumerable<PatientInfo> patientInfoList)
        {
            IList<PatientInfo> result = new List<PatientInfo>();

            var uniqueHealthCardNumbers = patientInfoList.Select(x => x.myHospitalNumber).Distinct();

            foreach (var healthCardNumber in uniqueHealthCardNumbers)
            {
                result.Add(patientInfoList.FirstOrDefault(x => x.myHospitalNumber == healthCardNumber));
            }

            return result;
        }

        public IQueryable<ServiceRecord> SearchServiceRecords(Guid userId, int? claimNumber, string lastName, string hospitalNumber,
                            SearchClaimType reportType, DateTime? serviceStart, DateTime? serviceEnd, DateTime? submissionStart, DateTime? submissionEnd, string explainCode, bool isAdmin)
        {
            var serviceRecords = context.ServiceRecord.Include("UnitRecord").Include("ClaimsIn").AsQueryable().Where(x => !x.ClaimToIgnore);

            if (!isAdmin)
            {
                serviceRecords = serviceRecords.Where(x => x.UserId == userId);
            }

            if (reportType == SearchClaimType.Paid)
            {
                serviceRecords = serviceRecords.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue);
            }
            else if (reportType == SearchClaimType.Rejected)
            {
                serviceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue);
            }
            else if (reportType == SearchClaimType.Submitted)
            {
                serviceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber == null && x.ClaimsInId.HasValue);
            }
            else if (reportType == SearchClaimType.Pending)
            {
                serviceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber != null);
            }
            else if (reportType == SearchClaimType.Unsubmitted)
            {
                serviceRecords = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber == null && !x.ClaimsInId.HasValue);
            }
            else if (reportType == SearchClaimType.All)
            {
                //serviceRecords = serviceRecords.Where(x => x.PaidClaimId.HasValue || x.RejectedClaimId.HasValue || x.ClaimsInId.HasValue || x.CPSClaimNumber != null);
            }

            if (claimNumber.HasValue)
            {
                serviceRecords = serviceRecords.Where(x => x.ClaimNumber == claimNumber.Value);
            }

            if (!string.IsNullOrEmpty(lastName))
            {
                serviceRecords = serviceRecords.Where(x => x.PatientLastName.Contains(lastName));
            }

            if (!string.IsNullOrEmpty(hospitalNumber))
            {
                serviceRecords = serviceRecords.Where(x => x.HospitalNumber.Contains(hospitalNumber));
            }

            if (serviceStart.HasValue)
            {
                serviceRecords = serviceRecords.Where(x => x.ServiceDate >= serviceStart.Value);
            }

            if (serviceEnd.HasValue)
            {
                serviceRecords = serviceRecords.Where(x => x.ServiceDate <= serviceEnd.Value);
            }

            if (submissionStart.HasValue)
            {
                serviceRecords = serviceRecords.Where(x => x.ClaimsIn.CreatedDate >= submissionStart.Value);
            }

            if (submissionEnd.HasValue)
            {
                serviceRecords = serviceRecords.Where(x => x.ClaimsIn.CreatedDate <= submissionEnd.Value);
            }

            if (!string.IsNullOrEmpty(explainCode))
            {
                explainCode = explainCode.ToUpper();
                serviceRecords = serviceRecords.Where(x => x.UnitRecord.Any(y => y.ExplainCode == explainCode || y.ExplainCode2 == explainCode || y.ExplainCode3 == explainCode));
            }

            return serviceRecords;
        }

        public IQueryable<ClaimsSearchView> SearchUnitRecords(Guid userId, int? claimNumber, string lastName, string firstName, string clinicName, string hospitalNumber, List<int> searchClaimTypes, DateTime? serviceStart, DateTime? serviceEnd,
            DateTime? submissionStart, DateTime? submissionEnd, string explainCode, string diagCode, string unitCode, string cpsClaimNumber, bool isAdmin)
        {
            var unitRecords = context.ClaimsSearchView.AsQueryable();

            if (!isAdmin)
            {
                unitRecords = unitRecords.Where(x => x.UserId == userId);
            }

            if (searchClaimTypes.Any())
            {
                unitRecords = unitRecords.Where(x => searchClaimTypes.Contains(x.ClaimStatus));
            }
            else
            {
                unitRecords = unitRecords.Where(x => x.ClaimStatus != (int)SearchClaimType.Deleted);
            }

            if (claimNumber.HasValue)
            {
                unitRecords = unitRecords.Where(x => x.ClaimNumber == claimNumber.Value);
            }

            if (!string.IsNullOrEmpty(cpsClaimNumber))
            {
                unitRecords = unitRecords.Where(x => x.CPSClaimNumber == cpsClaimNumber);
            }
            
            if (!string.IsNullOrEmpty(lastName))
            {
                unitRecords = unitRecords.Where(x => x.PatientLastName.Contains(lastName));
            }

            if (!string.IsNullOrEmpty(firstName))
            {
                unitRecords = unitRecords.Where(x => x.PatientFirstName.Contains(firstName));
            }

            if (!string.IsNullOrEmpty(clinicName))
            {
                unitRecords = unitRecords.Where(x => x.DoctorName.Contains(clinicName));
            }

            if (!string.IsNullOrEmpty(hospitalNumber))
            {
                unitRecords = unitRecords.Where(x => x.HospitalNumber.Contains(hospitalNumber));
            }

            if (serviceStart.HasValue)
            {
                unitRecords = unitRecords.Where(x => x.ServiceDate >= serviceStart.Value);
            }

            if (serviceEnd.HasValue)
            {
                unitRecords = unitRecords.Where(x => x.ServiceDate <= serviceEnd.Value);
            }

            if (submissionStart.HasValue)
            {
                unitRecords = unitRecords.Where(x => x.SubmissionDate >= submissionStart.Value);
            }

            if (submissionEnd.HasValue)
            {
                unitRecords = unitRecords.Where(x => x.SubmissionDate <= submissionEnd.Value);
            }

            if (!string.IsNullOrEmpty(explainCode))
            {
                explainCode = explainCode.ToUpper();
                unitRecords = unitRecords.Where(y => y.ExplainCode == explainCode || y.ExplainCode2 == explainCode || y.ExplainCode3 == explainCode);
            }

            if (!string.IsNullOrEmpty(unitCode))
            {
                unitCode = unitCode.ToUpper();
                unitRecords = unitRecords.Where(y => y.UnitCode == unitCode);
            }

            if (!string.IsNullOrEmpty(diagCode))
            {
                diagCode = diagCode.ToUpper();
                unitRecords = unitRecords.Where(y => y.DiagCode == diagCode);
            }

            return unitRecords;
        }

        public void UpdateUnitRecord(UnitRecord unitRecord)
        {
            if (context.Entry(unitRecord).State == EntityState.Unchanged)
            {
                context.Entry(unitRecord).State = EntityState.Modified;
            }
        }

        public void InsertUnitRecord(UnitRecord unitRecord)
        {
            context.Entry(unitRecord).State = EntityState.Added;
        }

        public void Insert(ClaimsResubmitted claimResubmit)
        {
            context.Entry(claimResubmit).State = EntityState.Added;
        }

        public IEnumerable<Users> GetUserNames(IEnumerable<Guid> userIds)
        {
            return context.Users.Where(x => userIds.Contains(x.UserId)).ToList();
        }

    }

    public interface IServiceRecordRepository : IDisposable
    {        
        ServiceRecord GetRecord(Guid id, Guid userId);

        ServiceRecord GetRecordWithUnitRecords(Guid id, Guid userId);

        IEnumerable<ServiceRecord> GetPaidRecordsWithUnitRecords(Guid userId, int claimNumber);

        ServiceRecord GetRecord(Guid id);

        void InsertOrUpdate(ServiceRecord serviceRecord);

        void Delete(ServiceRecord serviceRecord);

        void Save();

		void DeleteAllUnitRecords(Guid serviceRecordId);

		void InsertOrUpdate(UnitRecord unitRecord);

        void UpdateUnitRecord(UnitRecord unitRecord);

		bool IsAccessValidServiceRecords(Guid userId, Guid id, ClaimsInType type);

		bool IsServiceRecordBelongToUser(Guid userId, Guid serviceRecordId);

		NextNumberModel GetNextClaimNumber(Guid userId);

		IEnumerable<ServiceRecord> GetServiceRecordByIds(IEnumerable<Guid> serviceRecordIds);

        IEnumerable<SimpleRecord> GetAllSimpleUnSubmittedServiceRecords(Guid userId);

        IEnumerable<SimpleRecord> GetAllSimpleSubmittedServiceRecords(Guid userId);

        IEnumerable<SimpleRecord> GetAllSimplePendingServiceRecords(Guid userId);

        IEnumerable<SimpleRecord> GetAllSimpleRejectedServiceRecords(Guid userId);

        IQueryable<ServiceRecord> GetAllPaidClaimServiceRecords(Guid userId);

        IEnumerable<PatientInfo> GetPatientList(Guid userId, string lastNamePrefix);

        IEnumerable<PatientInfo> GetPatientListUsingHSN(Guid userId, string hsnPrefix);

        IEnumerable<UnitRecord> GetUnitRecords(Guid serviceRecordId);
		IEnumerable<UnitRecord> GetUnitRecords(IEnumerable<Guid> serviceRecordIds);

		Guid GetReturnId(ServiceRecord serviceRecord);

		void Insert(ClaimsIn claimsIn);

        void Insert(ClaimsResubmitted claimResubmit);

        void InsertFax(FaxDeliver faxDeliver);

		string GetNextRecordIndex(Guid userId, int timeZoneOffset);

        IEnumerable<RejectedClaim> GetRejectedClaims(IEnumerable<Guid> rejectedClaimIds);

        IEnumerable<ClaimsInReturn> GetClaimsInReturn(IEnumerable<Guid> returnIds);

        void UpdateClaimInReturn(ClaimsInReturn claimsInReturn);

        void ResetRejectedClaimIdForServiceRecords(IEnumerable<Guid> serviceRecordIds);

        UserProfiles GetUserProfile(Guid userId);

        IEnumerable<ServiceRecord> GetUnsubmittedWCBServiceRecords(Guid userId);

        IEnumerable<ServiceRecord> GetUnsubmittedMSBServiceRecords(Guid userId);

        void SetServiceRecordToModified(ServiceRecord serviceRecord);

        void UpdateServiceRecordsWithClaimInId(Guid claimsInId, Guid userId, IEnumerable<Guid> serviceRecordIds);

        Guid GetClaimsInReturnId(ServiceRecord serviceRecord);

        IEnumerable<FaxDeliver> WCBClaimsNeedToReSubmit(Guid userId, int timeZoneOffset);

        IEnumerable<ServiceRecord> GetWCBServiceRecords(IEnumerable<Guid> serviceRecordIds);

        void DeleteDeliver(FaxDeliver faxDeliver);

        void ConvertWCBToPaid(Guid serviceRecordId, Guid paidClaimId, double paidAmount);

        void SetRelatedFaxDeliver(Guid serviceRecordId, DeliverStatus status);

        void InsertClaimInReturn(ClaimsInReturn claimsInReturn);

        void InsertPaidClaim(PaidClaim paidClaim);

        void ResetSubmittedServiceRecord(Guid serviceRecordId, int rollOverNumber, int newClaimNumber);

        void ResetSubmittedServiceRecord(Guid userId, Guid serviceRecordId, int rollOverNumber, int newClaimNumber);

        void ResetSubmittedWCBServiceRecord(Guid serviceRecordId, int rollOverNumber, int newClaimNumber);

        void ConvertRejectedToPaid(Guid serviceRecordId, Guid paidClaimId, double claimAmount, double paidAmount);

        FaxDeliver GetFaxDeliver(Guid serviceRecordId);

        IEnumerable<FaxDeliver> GetPendingFaxes();

        void UpdateFaxDeliver(FaxDeliver record);

        IQueryable<ServiceRecord> SearchServiceRecords(Guid userId, int? claimNumber, string lastName, string hospitalNumber,
            SearchClaimType reportType, DateTime? serviceStart, DateTime? serviceEnd, DateTime? submissionStart, DateTime? submissionEnd, string explainCode, bool isAdmin);

        IQueryable<ClaimsSearchView> SearchUnitRecords(Guid userId, int? claimNumber, string lastName, string firstName, string clinicName, string hospitalNumber, List<int> searchClaimTypes, DateTime? serviceStart, DateTime? serviceEnd,
            DateTime? submissionStart, DateTime? submissionEnd, string explainCode, string diagCode, string unitCode, string cpsClaimNumber, bool isAdmin);

        IEnumerable<Users> GetUserNames(IEnumerable<Guid> userIds);

        string GetMostPopularUsedCodes(Guid userId);

        void AddPaidClaim(PaidClaim paidClaim);

        void InsertUnitRecord(UnitRecord unitRecord);

        void UpdateServiceRecordsWithClaimToIgnore(Guid userId, IEnumerable<Guid> serviceRecordIds);
    }
}