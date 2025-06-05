using MBS.Common;
using MBS.DomainModel;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBS.WebApiService;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Globalization;

namespace MBS.Web.Portal.Services
{
    public class SubmitPendingClaims : IJob
    {
        private MedicalBillingSystemEntities _context;
        private StringBuilder _logBuilder = new StringBuilder();

        public void Execute(IJobExecutionContext context)
        {
            _logBuilder.Remove(0, _logBuilder.Length);
            ProcessPendingClaims();
        }        

        public void ProcessPendingClaims()
        {
            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var containError = false;

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                WriteInfo("Submit Pending Claims Schedule Job");
                WriteInfo("Error connecting the database");
                WriteInfo("Exception caught: " + ex.Message);
                WriteInfo("Stack Trace: " + ex.StackTrace);

                SendJobMailAndLog();

                return;
            }

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);
            
            WriteInfo("Getting all the pending claims");

            var fetchedUserProfiles = new List<UserProfiles>();   
            foreach (var claimsIn in GetPendingClaimsIn())
            {
                var userProfile = fetchedUserProfiles.FirstOrDefault(x => x.UserId == claimsIn.UserId);

                if (userProfile == null)
                {
                    userProfile = GetUserProfile(claimsIn.UserId);
                    if (userProfile != null)
                    {
                        fetchedUserProfiles.Add(userProfile);
                    }
                }

                if (userProfile != null)
                {
                    WriteInfo("Working on user: " + userProfile.DoctorNumber + " " + userProfile.DoctorName);
                    WriteInfo("Working on Pending Claims: " + claimsIn.ClaimsInId + " - " + claimsIn.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss") + " - " + claimsIn.SubmittedFileName);

                    try
                    {
                        var needToSave = false;
                        var submitResult = apiService.UploadFile(userProfile.GroupUserKey, claimsIn.Content, userProfile.GroupNumber, claimsIn.SubmittedFileName);
                        if (submitResult.IsSuccess)
                        {
                            WriteInfo("MSB accepted the file - " + claimsIn.SubmittedFileName);
                            claimsIn.ValidationContent = submitResult.ISCContent;
                            claimsIn.FileSubmittedStatus = "ACCEPTED";
                            claimsIn.DateChangeToAccepted = DateTime.UtcNow;
                            _context.Entry(claimsIn).State = System.Data.Entity.EntityState.Modified;
                            needToSave = true;
                        }
                        else
                        {
                            if (submitResult.ErrorType == ErrorType.DUPLICATE_FILENAME)
                            {
                                WriteInfo("MSB response with Duplicate File code, this mean the file is accepted before - " + claimsIn.SubmittedFileName);
                                claimsIn.FileSubmittedStatus = "ACCEPTED";
                                claimsIn.DateChangeToAccepted = DateTime.UtcNow;
                                _context.Entry(claimsIn).State = System.Data.Entity.EntityState.Modified;
                                needToSave = true;
                            }
                            else if (submitResult.ErrorType == ErrorType.VALIDATION_FAILED)
                            {
                                WriteInfo("MSB response with Validation Failed with the claims, need to have the user re-submit. Mark the submission REJECTED.");

                                var serviceRecords = GetClaimsInServiceRecord(claimsIn.ClaimsInId);
                                foreach (var record in serviceRecords)
                                {
                                    record.ClaimsInId = null;
                                    _context.Entry(record).State = System.Data.Entity.EntityState.Modified;
                                }

                                claimsIn.FileSubmittedStatus = "REJECTED";
                                claimsIn.ValidationContent = submitResult.ISCContent;

                                _context.Entry(claimsIn).State = System.Data.Entity.EntityState.Modified;

                                needToSave = true;
                            }
                        }

                        if (needToSave)
                        {
                            _context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        containError = true;
                        WriteInfo("Error: " + ex.Message);
                    }
                }
                else
                {
                    WriteInfo("User Profile does not contain Group User Key or Group Number: " + claimsIn.UserId);
                }
            }

            WriteInfo("Finish submitting pending claims");

            if (containError)
            {
                SendJobMailAndLog();
            }

            _logBuilder.Remove(0, _logBuilder.Length);
        }

        private void WriteInfo(string message)
        {
            _logBuilder.Append(message).Append("<br/>");
            Debug.Write(message);
            Debug.Write(System.Environment.NewLine);
        }

        private void SendJobMailAndLog()
        {
            try
            {
                new MailSender().SendEmail(ConfigHelper.GetSupportEmail(), "Submit Pending Claims Email - " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logBuilder.Append("Send Email Exception: " + ex.Message);
            }

            try
            {
                WriteToFile(ConfigHelper.GetMSBLogsPath() + "SubmitPendingClaims_logs_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        
        private IEnumerable<ClaimsIn> GetPendingClaimsIn()
        {
            return _context.ClaimsIn.Where(x => x.FileSubmittedStatus == "PENDING" && x.Content != null).OrderBy(x => x.CreatedDate).ToList();
        }

        private IEnumerable<ServiceRecord> GetClaimsInServiceRecord(Guid claimsInId)
        {
            return _context.ServiceRecord.Where(x => x.ClaimsInId == claimsInId).ToList();
        }

        private UserProfiles GetUserProfile(Guid userId)
        {
            return _context.UserProfiles.FirstOrDefault(x => x.UserId == userId && x.GroupUserKey != null && x.GroupNumber != null);
        }      
        
        private void WriteToFile(string fileName, string content)
        {
            var file = new StreamWriter(fileName);
            file.Write(content.Replace("<br/>", System.Environment.NewLine));
            file.Close();
        }
    }
}
