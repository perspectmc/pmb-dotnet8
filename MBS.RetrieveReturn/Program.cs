using MBS.Common;
using MBS.DomainModel;
using MBS.Web.Portal.Services;
using MBS.WebApiService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MBS.RetrieveReturn
{
    static class DisableConsoleQuickEdit
    {

        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static bool Go()
        {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                // ERROR: Unable to get console mode.
                return false;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;

            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // ERROR: Unable to set console mode
                return false;
            }

            return true;
        }
    }
    class Program
    {
        private MedicalBillingSystemEntities _context;
        private StringBuilder _logBuilder = new StringBuilder();

        static void Main(string[] args)
        {
            DisableConsoleQuickEdit.Go();

            const string appName = "MBS.RetrieveReturn";
            bool createdNew;

            var mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Console.WriteLine(appName + " is already running! Exiting the application.");
                return;
            }

            var program = new Program();

            program.TestReturnCode();

            //var usersToCheckDuplicateItems = program.GetReturnsFromAPI();

            //program.SendJobMailAndLog();
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

            //var fileName = "I021_K45_slmpc2_20240319123856.txt";
            //var userProfile = GetUserProfile(new Guid("f7466716-f7fb-45ee-891b-9272a4694e26"));

            //var fileName = "I021_G78_test1_20240515011133.txt";

            //var fileName = "I021_C37_rc_test_20240905011217.txt";
            var fileName = "I021_K43_rd_TEST_20240918011211.txt";

            var userProfile = GetUserProfile(new Guid("DE265C24-1DD2-4A7B-8E63-BF9FE8C192ED"));

            var fileType = fileName.StartsWith("I029_") ? ReturnFileType.DAILY : ReturnFileType.BIWEEKLY;

            var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Returns\\apatel\\" + fileName);
            //var previousContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Test Return\\OlanPreviousContent.txt");


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

            var myParser = new ReturnParser(returnContent, fileType, userProfile, string.Empty, _context);

            if (myParser.InitialParseResult.IsSuccess)
            {
                var myReturnClaimsIns = myParser.GenerateReturnClaims(fileType, fileName);

                var actualSaveClaims = 0;

                foreach (var returnClaim in myReturnClaimsIns.Returns)
                {
                    actualSaveClaims++;
                    SetEntityState(returnClaim);
                }

                WriteInfo("There are claims: " + actualSaveClaims + " or need to claims to pending: " + myReturnClaimsIns.PerformPendingClaimsSaveChanges);

                if (actualSaveClaims > 0 || myReturnClaimsIns.PerformPendingClaimsSaveChanges)
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
                    catch (Exception ex)
                    {
                        WriteInfo("ERROR - SAVING");
                        WriteInfo(ex.Message);
                    }
                }
            }
        }

        private List<SimpleUserProfile> GetReturnsFromAPI()
        {
            var usersToCheckDuplicate = new List<SimpleUserProfile>();

            _logBuilder.Remove(0, _logBuilder.Length);

            var timeZoneOffset = ConfigHelper.GetTimeZoneOffset();
            var numberOfDaysToGetForDailyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForDailyReturnFiles();
            var numberOfDaysToGetForBiWeeklyReturnFiles = ConfigHelper.GetNumberOfDaysToGetForBiWeeklyReturnFiles();
            var numberOfDailyReturnFilesToGet = ConfigHelper.GetNumberOfDailyReturnFilesToGet();
            var numberOfBiWeeklyReturnFilesToGet = ConfigHelper.GetNumberOfBiWeeklyReturnFilesToGet();

            try
            {
                _context = new MedicalBillingSystemEntities();
                _context.Users.FirstOrDefault(); //Test DB connection
            }
            catch (Exception ex)
            {                
                WriteInfo("Error connecting the database");
                WriteInfo("Exception caught: " + ex.Message);
                WriteInfo("Stack Trace: " + ex.StackTrace);

                SendJobMailAndLog();

                return usersToCheckDuplicate;
            }

            var maxDailyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForDailyReturnFiles);
            var maxBiWeeklyReturnFileDate = DateTime.UtcNow.AddHours(timeZoneOffset).AddDays(numberOfDaysToGetForBiWeeklyReturnFiles);

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            WriteInfo("Getting all the user profiles by active users");

            //.Where(x => x.UserId == new Guid("B2333D6B-FAC5-4D7E-BE4C-BD4DC7C0C873")) //C86EFBC1-ECCD-4CB7-8C57-1652983D8C42 //d8c7f68a-e7fe-4362-9161-c085caff86c9
            //.Where(x => x.UserId == new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9")) //new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E") //51773f1a-8702-49c1-8f5b-381f32c71cb5
            //29628C01-497E-423C-B711-F6261AA407AA //BBFA3A4A-004A-48AF-A7BA-EE64F70700CA //51773F1A-8702-49C1-8F5B-381F32C71CB5
            //foreach (var userProfile in GetTestProfiles())

            foreach (var userProfile in GetUserProfilesByActiveUsers())
            {
                WriteInfo(string.Empty);
                WriteInfo("Getting returns for:" + userProfile.DoctorNumber  + " - "  + userProfile.DoctorName + ": " + userProfile.UserId);
                WriteInfo("Get Previous Return Files");

                try
                {
                    #region Get Return and Process code

                    /// return param 1 - Daily Return File Names
                    /// return param 2 - BiWeekly Return Files Names
                    /// return param 3 - Concated Return File Content
                    var previousReturnsTuple = GetPreviousReturnFiles(userProfile.UserId, maxDailyReturnFileDate, maxBiWeeklyReturnFileDate);
                    var previousContent = previousReturnsTuple.Item3;

                    var maxFileDate = DateTime.MinValue;

                    var returnFilesToProcess = new List<ReturnFileModel>();
                    
                    WriteInfo("Getting Daily Return Files from MSB API");
                    var msbDailyReturnFileModel = apiService.GetDailyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbDailyReturnFileModel.IsSuccess)
                    {
                        var dailyReturnFiles = msbDailyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) }).Where(x => x.FileDate > maxDailyReturnFileDate)
                            .OrderByDescending(x => x.FileDate).Take(numberOfDailyReturnFilesToGet).OrderBy(x => x.FileDate);                            
                        if (dailyReturnFiles.Any())
                        {
                            foreach (var msbDailyReturnFile in dailyReturnFiles)
                            {
                                WriteInfo("Checking MSB Daily Return File: " + msbDailyReturnFile.FileName);

                                if (!previousReturnsTuple.Item1.Contains(msbDailyReturnFile.FileName))
                                {
                                    WriteInfo("The daily return file had not been processed, download content");
                                    var dailyReturnFileModel = apiService.GetDailyReturnFile(userProfile.GroupUserKey, msbDailyReturnFile.FileName);
                                    if (dailyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(dailyReturnFileModel.FileContent))
                                    {
                                        dailyReturnFileModel.FileDateTime = msbDailyReturnFile.FileDate;
                                        returnFilesToProcess.Add(dailyReturnFileModel);
                                    }
                                    else
                                    {
                                        WriteInfo("Unable to get daily file content: " + dailyReturnFileModel.ErrorMessage);
                                    }
                                }
                                else
                                {
                                    WriteInfo(msbDailyReturnFile.FileName + " already processed before, skip the file");
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
                        var biWeeklyReturnFiles = msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) }).Where(x => x.FileDate > maxBiWeeklyReturnFileDate)
                            .OrderByDescending(x => x.FileDate).Take(numberOfBiWeeklyReturnFilesToGet).OrderBy(x => x.FileDate);
                        if (biWeeklyReturnFiles.Any())
                        {
                            foreach (var msbBiWeeklyReturnFile in biWeeklyReturnFiles)
                            {
                                WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFile.FileName);

                                if (!previousReturnsTuple.Item2.Contains(msbBiWeeklyReturnFile.FileName))
                                {
                                    WriteInfo("The biweekly return file had not been processed, download content");
                                    var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFile.FileName);
                                    if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                                    {
                                        biWeeklyReturnFileModel.FileDateTime = msbBiWeeklyReturnFile.FileDate;
                                        returnFilesToProcess.Add(biWeeklyReturnFileModel);
                                    }
                                    else
                                    {
                                        WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                                    }
                                }
                                else
                                {
                                    WriteInfo(msbBiWeeklyReturnFile.FileName + " already processed before, skip the file");
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
                        WriteInfo("Not able to get biweekly return file list: " + msbBiWeeklyReturnFileModel.ErrorMessage);
                    }

                    WriteInfo("Finish download the return contents, and now loop through each file content according to File Date Time");

                    if (returnFilesToProcess.Any())
                    {
                        var fileDates = returnFilesToProcess.Where(x => x.FileDateTime > maxFileDate).OrderBy(x => x.FileDateTime).Select(x => x.FileName);

                        foreach (var fileModel in returnFilesToProcess.Where(x => x.FileDateTime > maxFileDate).OrderBy(x => x.FileDateTime))
                        {
                            WriteInfo("Working on return file: " + fileModel.FileName);

                            var processResult = ProcessReturn(fileModel, userProfile, previousContent);
                            if (processResult.IsSuccess)
                            {
                                previousContent += System.Environment.NewLine + fileModel.FileContent;
                            }

                            WriteInfo("Process Return Result: " + processResult.Message);
                        }
                    }
                    else
                    {
                        WriteInfo("No return files to process");
                    }

                    #endregion
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

                    WriteInfo("Exception caught: " + dbEx.Message);
                    WriteInfo("Stack Trace: " + dbEx.StackTrace);
                    WriteInfo("Dispose Context and Create New Context for next return");

                    if (_context.ChangeTracker.HasChanges())
                    {                      
                        try
                        {
                            _context.Dispose();
                        }
                        catch (Exception ex)
                        {
                            WriteInfo("Dispose Error: " + ex.Message);
                        }

                        try
                        {
                            _context = new MedicalBillingSystemEntities();
                        }
                        catch (Exception ex)
                        {
                            WriteInfo("New Context Error: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteInfo("Exception caught: " + ex.Message);
                    WriteInfo("Stack Trace: " + ex.StackTrace);
                    WriteInfo("Dispose Context and Create New Context for next return");

                    try
                    {
                        _context.Dispose();
                    }
                    catch (Exception ex1)
                    {
                        WriteInfo("Dispose Error: " + ex1.Message);
                    }

                    try
                    {
                        _context = new MedicalBillingSystemEntities();
                    }
                    catch (Exception ex2)
                    {
                        WriteInfo("New Context Error: " + ex2.Message);
                    }
                }
            }

            WriteInfo("Finish pick up return for all the users");

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }

            return usersToCheckDuplicate;
        }      
        
        private void WriteInfo(string message)
        {
            _logBuilder.Append(message).Append("<br/>");
            Console.WriteLine(message);
        }

        private void SendJobMailAndLog()
        {
            try
            {
                WriteToFile(ConfigHelper.GetMSBLogsPath() + "ReturnRetrival_logs_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                new MailSender().SendEmail(ConfigHelper.GetSupportEmail(), "MBS Return Retrieval Email - " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logBuilder.Append("Send Email Exception: " + ex.Message);
            }

            _logBuilder.Remove(0, _logBuilder.Length);
        }

        private ProcessResult ProcessReturn(ReturnFileModel fileModel, UserProfiles userProfile, string previousReturnContent)
        {
            var result = new ProcessResult();
            result.IsSuccess = false;

            WriteInfo("Process the return content");
            var myParser = new ReturnParser(fileModel.FileContent, fileModel.ReturnFileType, userProfile, previousReturnContent, _context);

            if (myParser.InitialParseResult.IsSuccess)
            {
                WriteInfo("Process the return, and get the ClaimInReturn");
                var myReturnClaimsIns = myParser.GenerateReturnClaims(fileModel.ReturnFileType, fileModel.FileName);
                var actualSaveClaims = 0;

                foreach (var returnClaim in myReturnClaimsIns.Returns)
                {
                    actualSaveClaims++;
                    SetEntityState(returnClaim);
                }

                WriteInfo("There are claims: " + actualSaveClaims);
                if (actualSaveClaims > 0 || myReturnClaimsIns.PerformPendingClaimsSaveChanges)
                {
                    WriteInfo("Saving the claim to database");
                    _context.SaveChanges();

                    result.IsSuccess = true;
                    result.Message = "Successfully pick up and process the return(s)";
                }
                else
                {
                    ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                    myClaimsInReturn.UserId = userProfile.UserId;
                    myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                    myClaimsInReturn.ReturnFooter = string.Empty;
                    myClaimsInReturn.ReturnFileType = (int)fileModel.ReturnFileType;
                    myClaimsInReturn.ReturnFileName = fileModel.FileName;
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.ReturnFileDate = MBS.RetrieveReturn.Program.GetFileDateTime(fileModel.FileName);
                    myClaimsInReturn.TotalApproved = 0;
                    myClaimsInReturn.TotalSubmitted = 0;
                    myClaimsInReturn.TotalPaidAmount = 0;
                    myClaimsInReturn.RunCode = string.Empty;
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.Content = fileModel.FileContent;

                    myParser.GetTotalAmountsFromTotalLines(myClaimsInReturn);

                    SetEntityState(myClaimsInReturn);

                    WriteInfo("There are no new claims in the return, please come back later! Save the return file to database");
                    _context.SaveChanges();

                    result.IsSuccess = true;
                    result.Message = "There are no new claims in the return, please come back later!";
                }
            }
            else
            {
                if (myParser.InitialParseResult.ErrorType == ErrorType.EMPTY_CONTENT)
                {
                    ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                    myClaimsInReturn.UserId = userProfile.UserId;
                    myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                    myClaimsInReturn.ReturnFooter = string.Empty;
                    myClaimsInReturn.ReturnFileType = (int)fileModel.ReturnFileType;
                    myClaimsInReturn.ReturnFileName = fileModel.FileName;
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.ReturnFileDate = MBS.RetrieveReturn.Program.GetFileDateTime(fileModel.FileName);
                    myClaimsInReturn.TotalApproved = 0;
                    myClaimsInReturn.TotalSubmitted = 0;
                    myClaimsInReturn.TotalPaidAmount = 0;
                    myClaimsInReturn.RunCode = string.Empty;
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.Content = fileModel.FileContent;

                    myParser.GetTotalAmountsFromTotalLines(myClaimsInReturn);

                    SetEntityState(myClaimsInReturn);

                    WriteInfo("There is no new content in the return file");
                    _context.SaveChanges();

                    result.IsSuccess = true;
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
            var claimReturns = _context.ClaimsInReturn.Where(
                                    x => x.UserId == userId && x.ReturnFileDate != null && x.ReturnFileName != "" && x.Content != null && x.Content != "" &&
                                            (x.ReturnFileType == 0 && x.ReturnFileDate > maxDailyReturnFileDate ||
                                            x.ReturnFileType == 1 && x.ReturnFileDate > maxBiWeeklyReturnFileDate)
                                        //&& x.UserId == Guid.Empty //NEED TO REMOVE---- TESTING PURPOSE                                        
                                   )
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
        
        private UserProfiles GetUserProfile(Guid userId)
        {
            return _context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }

        private IEnumerable<UserProfiles> GetTestProfiles()
        {
            return _context.UserProfiles.Where(x => x.UserId == new Guid("2434726f-ac59-4727-97be-0d99a2958a71")).ToList();
        }

        private IEnumerable<UserProfiles> GetUserProfilesByActiveUsers()
        {
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");            
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            var dateCondition = DateTime.UtcNow.AddMonths(-6);
            return (from uc in _context.UserProfiles
                    join m in _context.Memberships
                    on uc.UserId equals m.UserId
                    where m.IsLockedOut == false && uc.UserId != ignoreId1 && uc.UserId != ignoreId2 &&
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

    public class SimpleUserProfile
    {
        public Guid UserId { get; set; }

        public string DoctorName { get; set; }

        public string DoctorNumber { get; set; }
        public string DiagnosticCode { get; set; }
        public string GroupNumber { get; set; }
        public string GroupUserKey { get; set; }

        public string ClinicNumber { get; set; }
    }
}
