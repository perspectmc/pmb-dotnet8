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
    public class ReturnRetrivalJob : IJob
    {
        private MedicalBillingSystemEntities _context;
        private StringBuilder _logBuilder = new StringBuilder();

        public void Execute(IJobExecutionContext context)
        {
            _logBuilder.Remove(0, _logBuilder.Length);
            GetReturnsFromAPI();

            //TestReturnCode();
            //SendJobMailAndLog();
            //AddIgnoreReturn();
        }        

        private void TestReturnCode()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var fileName = "I021_H56_20240204111111.txt";
            var fileType = fileName.StartsWith("I029_") ? ReturnFileType.DAILY : ReturnFileType.BIWEEKLY;

            var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Return Files\\" + fileName);

            //H56 - 7B65ABA4-C0A1-4DEC-B404-54E81BA0140B
            //261 - C288884C-46E9-4137-9468-7B115896FE03
            var userProfile = GetUserProfile(new Guid("7B65ABA4-C0A1-4DEC-B404-54E81BA0140B"));

            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var numberOfDaysToGetForDailyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForDailyReturnFiles();
            var numberOfDaysToGetForBiWeeklyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForBiWeeklyReturnFiles();
            var numberOfDailyReturnFilesToGet = ConfigHelper.GetNumberOfDailyReturnFilesToGet();
            var numberOfBiWeeklyReturnFilesToGet = ConfigHelper.GetNumberOfBiWeeklyReturnFilesToGet();

            var maxDailyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForDailyReturnFiles);
            var maxBiWeeklyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForBiWeeklyReturnFiles);


            var previousReturnsTuple = GetPreviousReturnFiles(userProfile.UserId, maxDailyReturnFileDate, maxBiWeeklyReturnFileDate);
            /// return param 1 - Daily Return File Names
            /// return param 2 - BiWeekly Return Files Names
            /// return param 3 - Concated Return File Content

            var myParser = new ReturnParser(returnContent, fileType, userProfile, previousReturnsTuple.Item3, _context);

            if (myParser.InitialParseResult.IsSuccess)
            {
                var myReturnClaimsIns = myParser.GenerateReturnClaims(fileType, fileName);

                var actualSaveClaims = 0;

                foreach (var returnClaim in myReturnClaimsIns.Where(x => x.PaidClaim.Any() || x.RejectedClaim.Any()))
                {
                    actualSaveClaims++;
                    SetEntityState(returnClaim);
                }

                WriteInfo("There are claims: " + actualSaveClaims);
                if (actualSaveClaims > 0)
                {
                    WriteInfo("Saving the claim to database");
                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                System.Diagnostics.Debug.WriteLine("Property: {0} Error: {1}",
                                                        validationError.PropertyName,
                                                        validationError.ErrorMessage);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        WriteInfo("ERROR - SAVING");
                        WriteInfo(ex.Message);
                    }
                }
            }
        }

        public void GetReturnsFromAPI()
        {
            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var numberOfDaysToGetForDailyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForDailyReturnFiles();
            var numberOfDaysToGetForBiWeeklyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForBiWeeklyReturnFiles();
            var numberOfDailyReturnFilesToGet = ConfigHelper.GetNumberOfDailyReturnFilesToGet();
            var numberOfBiWeeklyReturnFilesToGet = ConfigHelper.GetNumberOfBiWeeklyReturnFilesToGet();

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                WriteInfo("Error connecting the database");
                WriteInfo("Exception caught: " + ex.Message);
                WriteInfo("Stack Trace: " + ex.StackTrace);

                SendJobMailAndLog();

                return;
            }

            var maxDailyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForDailyReturnFiles);
            var maxBiWeeklyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForBiWeeklyReturnFiles);

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);
            
            WriteInfo("Getting all the user profiles by active users");            
            foreach (var userProfile in GetUserProfilesByActiveUsers())
            {           
                WriteInfo("Getting returns for " + userProfile.DoctorName);

                WriteInfo("Get Previous Return Files");

                /// return param 1 - Daily Return File Names
                /// return param 2 - BiWeekly Return Files Names
                /// return param 3 - Concated Return File Content
                var previousReturnsTuple = GetPreviousReturnFiles(userProfile.UserId, maxDailyReturnFileDate, maxBiWeeklyReturnFileDate);

                try
                {
                    WriteInfo("Getting Daily Return Files from MSB API");
                    var msbDailyReturnFileModel = apiService.GetDailyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbDailyReturnFileModel.IsSuccess)
                    {
                        var dailyReturnFiles = msbDailyReturnFileModel.FileNames.OrderByDescending(x => x).Take(numberOfDailyReturnFilesToGet).OrderBy(x => x)
                            .Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                            .Where(x => x.FileDate > maxDailyReturnFileDate);
                        if (dailyReturnFiles.Any())
                        {                           
                            foreach (var msbDailyReturnFile in dailyReturnFiles.OrderBy(x => x.FileDate).Select(x => x.FileName)) 
                            {
                                WriteInfo("Checking MSB Daily Return File: " + msbDailyReturnFile);

                                if (!previousReturnsTuple.Item1.Contains(msbDailyReturnFile))
                                {
                                    WriteInfo("The daily return file had not been processed, download content");
                                    var dailyReturnFileModel = apiService.GetDailyReturnFile(userProfile.GroupUserKey, msbDailyReturnFile);
                                    if (dailyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(dailyReturnFileModel.FileContent))
                                    {
                                        var processResult = ProcessReturn(ReturnFileType.DAILY, msbDailyReturnFile, dailyReturnFileModel.FileContent, userProfile, previousReturnsTuple.Item3);
                                        WriteInfo("Process Return Result: " + processResult.Message);
                                    }
                                    else
                                    {
                                        WriteInfo("Unable to get daily file content: " + dailyReturnFileModel.ErrorMessage);
                                    }
                                }
                                else
                                {
                                    WriteInfo(msbDailyReturnFile + " already processed before, skip the file");
                                }                                
                            }
                        } 
                        else
                        {
                            WriteInfo("No daily return files");
                        }
                    }
                    else
                    {
                        WriteInfo("Not able to get daily return file list: " + msbDailyReturnFileModel.ErrorMessage);
                    }

                    WriteInfo("Getting BiWeekly Return Files from MSB API");
                    var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbBiWeeklyReturnFileModel.IsSuccess)
                    {
                        var biWeeklyReturnFiles = msbBiWeeklyReturnFileModel.FileNames.OrderByDescending(x => x).Take(numberOfBiWeeklyReturnFilesToGet).OrderBy(x => x)
                            .Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                            .Where(x => x.FileDate > maxBiWeeklyReturnFileDate);
                        if (biWeeklyReturnFiles.Any())
                        {
                            foreach (var msbBiWeeklyReturnFile in biWeeklyReturnFiles.OrderBy(x => x.FileDate).Select(x => x.FileName))
                            {
                                WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFile);

                                if (!previousReturnsTuple.Item2.Contains(msbBiWeeklyReturnFile))
                                {
                                    WriteInfo("The biweekly return file had not been processed, download content");
                                    var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFile);
                                    if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                                    {
                                        var processResult = ProcessReturn(ReturnFileType.BIWEEKLY, msbBiWeeklyReturnFile, biWeeklyReturnFileModel.FileContent, userProfile, previousReturnsTuple.Item3);
                                        WriteInfo("Process Return Result: " + processResult.Message);
                                    }
                                    else
                                    {
                                        WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                                    }
                                }
                                else
                                {
                                    WriteInfo(msbBiWeeklyReturnFile + " already processed before, skip the file");
                                }                               
                            }
                        }
                        else
                        {
                            WriteInfo("No biweekly return files");
                        }
                    }
                    else
                    {
                        WriteInfo("Not able to get biweekly return file list: " + msbDailyReturnFileModel.ErrorMessage);
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine("Property: {0} Error: {1}",
                                                    validationError.PropertyName,
                                                    validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteInfo("Exception caught: " + ex.Message);
                    WriteInfo("Stack Trace: " + ex.StackTrace);
                }                    
            }

            WriteInfo("Finish pick up return for all the users");

            SendJobMailAndLog();

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
                new MailSender().SendEmail(ConfigHelper.GetSupportEmail(), "MBS Return Retrieval Email - " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logBuilder.Append("Send Email Exception: " + ex.Message);
            }

            try
            {
                WriteToFile(ConfigHelper.GetMSBLogsPath() + "ReturnRetrival_logs_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        
        private ProcessResult ProcessReturn(ReturnFileType returnFileType, string msbReturnFileName, string msbReturnFileContent, UserProfiles userProfile, string previousReturnContent)
        {
            var result = new ProcessResult();
            result.IsSuccess = false;
            
            WriteInfo("Process the return content");
            var myParser = new ReturnParser(msbReturnFileContent, returnFileType, userProfile, previousReturnContent, _context);

            if (myParser.InitialParseResult.IsSuccess)
            {
                WriteInfo("Process the return, and get the ClaimInReturn");
                var myReturnClaimsIns = myParser.GenerateReturnClaims(returnFileType, msbReturnFileName);
                var actualSaveClaims = 0;

                foreach (var returnClaim in myReturnClaimsIns.Where(x => x.PaidClaim.Any() || x.RejectedClaim.Any()))
                {
                    actualSaveClaims++;
                    SetEntityState(returnClaim);
                }

                WriteInfo("There are claims: " + actualSaveClaims);
                if (actualSaveClaims > 0)
                {
                    WriteInfo("Saving the claim to database");
                    _context.SaveChanges();
                    result.IsSuccess = true;
                    result.Message = "Successfully pick up and process the return(s)";
                }
                else
                {
                    result.Message = "There are no new claims in the return, please come back later!";
                }
            }
            else
            {
                if (myParser.InitialParseResult.ErrorType == ErrorType.EMPTY_CONTENT)
                {
                    result.Message = "There is no new content in the return file";
                }                                 
            }
            
            return result;
        }

        /// <summary>
        /// Get Previous Process Return Files
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>
        /// return param 1 - Daily Return File Names
        /// return param 2 - BiWeekly Return Files Names
        /// return param 3 - Concated Return File Content
        /// </returns>
        private Tuple<IEnumerable<string>, IEnumerable<string>, string> GetPreviousReturnFiles(Guid userId, DateTime maxDailyReturnFileDate, DateTime maxBiWeeklyReturnFileDate)
        {
            //ReturnFileType = 0: Daily Return File
            //ReturnFileType = 1: BiWeekly Return File

            var claimReturns = _context.ClaimsInReturn.Where(x => x.UserId == userId && x.ReturnFileDate != null && x.ReturnFileName != "" && x.Content != null && x.Content != "" && 
                                    (x.ReturnFileType == 0 && x.ReturnFileDate > maxDailyReturnFileDate || 
                                    x.ReturnFileType == 1 && x.ReturnFileDate > maxBiWeeklyReturnFileDate))
                                .OrderByDescending(x => x.ReturnFileDate).ToList();

            var previousBiWeeklyReturnFileNames = claimReturns.Where(x => x.ReturnFileType == 1).Select(x => x.ReturnFileName).ToList();

            var previousDailyReturnFileNames = claimReturns.Where(x => x.ReturnFileType == 0).Select(x => x.ReturnFileName).ToList();

            var previousConcatedContent = string.Join(System.Environment.NewLine, claimReturns.Select(x => x.Content.Trim()));

            return new Tuple<IEnumerable<string>, IEnumerable<string>, string>(
                previousDailyReturnFileNames,
                previousBiWeeklyReturnFileNames,
                previousConcatedContent.Trim()
            );
        }

        private bool ProcessReturnBefore(Guid userId)
        {
            return false;
            //var result = false;
            //var returnDate = _context.ClaimsInReturn.Where(x => x.UserId == userId && x.Content != null).Select(x => x.UploadDate).OrderByDescending(x => x).FirstOrDefault();
            //if (returnDate != null && returnDate != DateTime.MinValue)
            //{
            //    result = returnDate.AddDays(2) >= DateTime.UtcNow;
            //}

            //return result;
        }
        
        private IEnumerable<UserCertificates> GetUserCertificates()
        {
            var ignoreId = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var dateCondition = DateTime.UtcNow.AddMonths(-6);
            return (from uc in _context.UserCertificates
                    join m in _context.Memberships
                    on uc.UserId equals m.UserId
                    where m.IsLockedOut == false && uc.UserId != ignoreId && m.LastLoginDate > dateCondition
                    select uc).ToList();
        }

        private UserProfiles GetUserProfile(Guid userId)
        {
            return _context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }      
        
        private IEnumerable<UserProfiles> GetUserProfilesByActiveUsers()
        {
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            var dateCondition = DateTime.UtcNow.AddMonths(-6);
            return (from uc in _context.UserProfiles
                    join m in _context.Memberships
                    on uc.UserId equals m.UserId
                    where m.IsLockedOut == false && uc.UserId != ignoreId1 && uc.UserId != ignoreId2 && m.LastLoginDate > dateCondition && 
                          uc.GroupUserKey != null && uc.GroupUserKey != "" && uc.GroupNumber != null && uc.GroupNumber != ""
                    select uc).ToList();
        }

        private void SetEntityState(ClaimsInReturn returnRecord)
        {
            _context.ClaimsInReturn.Add(returnRecord);

            foreach (var rejectedClaim in returnRecord.RejectedClaim)
            {
                _context.RejectedClaim.Add(rejectedClaim);
                if (rejectedClaim.ServiceRecord != null)
                {
                    SetServiceRecordState(rejectedClaim.ServiceRecord);
                }
            }

            foreach (var paidClaim in returnRecord.PaidClaim)
            {
                _context.PaidClaim.Add(paidClaim);
                if (paidClaim.ServiceRecord != null)
                {
                    SetServiceRecordState(paidClaim.ServiceRecord);
                }
            }
        }

        private void SetServiceRecordState(IEnumerable<ServiceRecord> serviceRecords)
        {
            foreach (var serviceRecord in serviceRecords)
            {
                _context.ServiceRecord.Add(serviceRecord);
                foreach (var unitRecord in serviceRecord.UnitRecord)
                {
                    _context.UnitRecord.Add(unitRecord);
                }
            }
        }
        
        private void WriteToFile(string fileName, string content)
        {
            var file = new StreamWriter(fileName);
            file.Write(content.Replace("<br/>", System.Environment.NewLine));
            file.Close();
        }

        public static DateTime GetFileDateTime(string fileName)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var splitString = fileName.Replace(".txt", string.Empty).Split('_');

            return DateTime.ParseExact(splitString.Last(), "yyyyMMddHHmmss", provider);
        }
    }

    public class ProcessResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }
    }
}
