using MBS.Common;
using MBS.DomainModel;
using Microsoft.VisualBasic.FileIO;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MBS.WebApiService;
using MBS.Web.Portal.Services;
using MBS.DataCache;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace MBS.TestCodeUsed
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
        private IEnumerable<DateTime> _scheduleList;
        private int _timeZoneOffset = 0;
        private DateTime _currentSchedulePeriodStart;
        private DateTime _currentSchedulePeriodEnd;
        private StringBuilder _logBuilder = new StringBuilder();
        private char myBreak = '\n';

        static void Main(string[] args)
        {
            DisableConsoleQuickEdit.Go();

            var program = new Program();
            
            //program.GetPaidSummaryFromReprint();

            //program.GetRefDocDictionary();

            //program.GetICDCodeDictionary();

            //program.GetFeeCodeDictionary();

            program.GetExplainCodeDictionary();

            //program.DownloadReturns();

            //program.ShowClaimTraceInBiweeklyReturn();

            //program.FixServiceLocation();

            //program.GetRefDocDictionary();

            //program.CheckPendingClaimsInReturn();

            //program.MergeServiceRecords();

            //program.CheckAndRerunClaims();

            //program.CheckDuplicateClaimsWithServiceDateAndHSN();

            //program.RemoveDuplicateLineItems();

            //program.BackfillClaimsInId();

            ///program.GetReturns();

            //var userId = new Guid("7154791D-5AEB-4881-895A-208493BA9579");
            //program.RemoveDuplicateLineItems_Step1(userId);
            //program.RemoveDuplicateLineItems_Step2(userId);
            //program.RemoveDuplicateLineItems_Step3(userId);
            //program.RemoveDuplicateLineItems_Step4(userId);

            //var userId2 = new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9");
            ////program.RemoveDuplicateLineItems_Step1(userId2);
            ////program.RemoveDuplicateLineItems_Step2(userId2);
            ////program.RemoveDuplicateLineItems_Step3(userId2);
            //program.RemoveDuplicateLineItems_Step4(userId2);

            //var userId3 = new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E");
            ////program.RemoveDuplicateLineItems_Step1(userId3);
            ////program.RemoveDuplicateLineItems_Step2(userId3);
            ////program.RemoveDuplicateLineItems_Step3(userId3);
            //program.RemoveDuplicateLineItems_Step4(userId3);


            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Merge Paid Claims");
            //program.MergePaidClaimsForUser();
            //Console.WriteLine("*************************************************************");
            //Console.WriteLine(System.Environment.NewLine);
            //Console.WriteLine(System.Environment.NewLine);

            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Check Pending Claims");
            //program.CheckPendingClaimInPaidOrRejected();
            //Console.WriteLine("*************************************************************");
            //Console.WriteLine(System.Environment.NewLine);
            //Console.WriteLine(System.Environment.NewLine);

            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Match Claims");
            //program.BackfillClaimsInId();
            //Console.WriteLine("*************************************************************");
            //Console.WriteLine(System.Environment.NewLine);
            //Console.WriteLine(System.Environment.NewLine);

            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Merge Rejected Claims");
            //program.MergeRejectedClaimsForUser();
            //Console.WriteLine("*************************************************************");
            //Console.WriteLine(System.Environment.NewLine);
            //Console.WriteLine(System.Environment.NewLine);

            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Update CPS Numbers");
            //program.SetRecordsToPending();
            //Console.WriteLine("*************************************************************");
            //Console.WriteLine(System.Environment.NewLine);
            //Console.WriteLine(System.Environment.NewLine);

            //Console.WriteLine("*************************************************************");
            //Console.WriteLine("Reprocess Returns");
            //program.ReprocessPendingReturn();
            //Console.WriteLine("*************************************************************");
        }

        private void GetUserIds()
        {
            var userList = GetCSVContent("ManagerUsers.csv").Select(x => new { Name = x[1].Trim().ToLower(), Email = x[2].Trim().ToLower() }).ToList();

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var memberships = _context.Memberships.ToList();
            var userProfiles = _context.UserProfiles.ToList();
            var builder = new StringBuilder();

            foreach(var user in userList)
            {
                
                var foundUsingEmail = memberships.FirstOrDefault(x => x.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

                if (foundUsingEmail != null)
                {
                    
                    Console.WriteLine(user.Name + "-" + user.Email + ": " + foundUsingEmail.UserId);
                }
                else
                {
                    var foundUsingNames = userProfiles.Where(x => x.DoctorName.Contains(user.Name)).ToList();
                    if (foundUsingNames.Count() == 1)
                    {
                        Console.WriteLine(user.Name + "-" + user.Email + ": " + foundUsingNames.FirstOrDefault().UserId);
                    }
                    else if (foundUsingNames.Count() > 1)
                    {
                        Console.WriteLine(user.Name + "-" + user.Email + ": " + string.Join(", ", foundUsingNames.Select(x => x.UserId)));
                    }
                    else
                    {
                        Console.WriteLine(user.Name + "-" + user.Email + ": NOT FOUND");
                    }
                }
            }
            
            try
            {
                _context.Dispose();
            }
            catch (Exception ex)            
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ShowClaimTraceInBiweeklyReturn()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            //var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("133889BC-98EB-4EFC-B68E-7252C4197D8F"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            _context.Dispose();

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            char myBreak = '\n';

            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    var myPreviousLines = new List<string>();

                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    var returnLineItems = new List<ReturnLineItem>();

                    var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                    var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                    #region Get BiWeekly Returns and Convert To ReturnLineItems

                    var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbBiWeeklyReturnFileModel.IsSuccess && msbBiWeeklyReturnFileModel.FileNames.Any())
                    {
                        foreach (var msbBiWeeklyReturnFileName in msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                                .Where(x => x.FileDate > new DateTime(2024, 2, 6))
                                    .OrderBy(x => x.FileDate).Select(x => x.FileName))
                        {

                            WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFileName);

                            WriteInfo("The biweekly return file had not been processed, download content");
                            var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFileName);
                            if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                            {
                                var currentReturnLines = RemoveUnRelatedLines(
                                                            biWeeklyReturnFileModel.FileContent.Split(myBreak),
                                                            myDoctorNumber,
                                                            myClinicNumber);
                                var distinctCurrentLines = currentReturnLines.Distinct();
                                var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();

                                var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                                exceptLines.AddRange(keep89Lines);

                                var myReturnLineItems = new List<ReturnLineItem>();

                                foreach (var myLine in exceptLines)
                                {
                                    if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                                    {
                                    }
                                    else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                                    {
                                        //Ignore the lines
                                    }
                                    else
                                    {
                                        var temp = CreateReturnLineItem(myLine);
                                        temp.ReturnFileName = msbBiWeeklyReturnFileName;
                                        temp.ReturnFileDate = GetFileDateTime(msbBiWeeklyReturnFileName).AddHours(6);
                                        myReturnLineItems.Add(temp);
                                    }
                                }

                                var oopLineItems = myReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                                foreach (var lineItem in myReturnLineItems.Where(x => string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                                {
                                    var oopLine = oopLineItems.FirstOrDefault(x => x.CPSClaimNumber == lineItem.CPSClaimNumber);
                                    if (oopLine != null)
                                    {
                                        lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                                        lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                                        lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                                    }
                                    else
                                    {
                                        WriteInfo("Cannot find OOP line for: " + lineItem.CPSClaimNumber);
                                    }
                                }

                                returnLineItems.AddRange(myReturnLineItems);

                                myPreviousLines.AddRange(currentReturnLines);
                            }
                            else
                            {
                                WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                            }
                        }
                    }

                    #endregion

                    Console.WriteLine();
                    var wantedLineItems = returnLineItems.Where(x => x.PatientInfo.HospitalNumber == "700109862" && x.ServiceDate == new DateTime(2024, 2, 1)).ToList();

                    foreach (var lineItem in wantedLineItems.OrderBy(x => x.ReturnFileDate).ThenBy(x => x.ClaimAndSeqNumber))
                    {
                        WriteInfo($"{lineItem.ReturnFileName.PadRight(55, ' ')}: {lineItem.ClaimNumber}_{lineItem.SeqNumber} - {lineItem.LineContent}");
                    }

                }
            }

            WriteLogToFile("ShowTracesInClaims" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }

        private void FixServiceLocation()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var serviceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.ServiceLocation == "X" && x.ClaimStatus == 0).ToList();

            foreach(var serviceRecord in serviceRecords)
            {
                foreach(var unitRecord in serviceRecord.UnitRecord)
                {
                    unitRecord.PaidAmount = GetTotalWithPremiumAmount(unitRecord.UnitCode, unitRecord.UnitAmount, unitRecord.UnitPremiumCode, "X");
                    SetUnitRecordStateToModified(unitRecord);
                }

                serviceRecord.ClaimAmount = serviceRecord.UnitRecord.Sum(x => x.PaidAmount);

                SetServiceRecordStateToModified(serviceRecord);
            }

            _context.SaveChanges();

        }


        private void GetReturnFilesForPaymentSummary()
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

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null && x.GroupUserKey != "" && x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E")).ToList();

            var wantedReprintDate = new DateTime(2024, 7, 1);
            foreach (var userProfile in userProfiles)
            {
                WriteInfo(string.Empty);
                WriteInfo("Getting returns for:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);
                WriteInfo("Get Previous Return Files");

                try
                {
                    var returnFilesToProcess = new List<ReturnFileModel>();

                    WriteInfo("Getting BiWeekly Return Files from MSB API");
                    var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbBiWeeklyReturnFileModel.IsSuccess)
                    {
                        var biWeeklyReturnFiles = msbBiWeeklyReturnFileModel.FileNames.OrderByDescending(x => x).Where(x => x.Contains("REPRINT_")).OrderBy(x => x)
                            .Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                            .Where(x => x.FileDate > wantedReprintDate);
                        if (biWeeklyReturnFiles.Any())
                        {
                            foreach (var msbBiWeeklyReturnFile in biWeeklyReturnFiles.OrderBy(x => x.FileDate).Select(x => x.FileName))
                            {
                                WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFile);

                                var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFile);
                                if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                                {
                                    biWeeklyReturnFileModel.FileDateTime = GetFileDateTime(msbBiWeeklyReturnFile);
                                    returnFilesToProcess.Add(biWeeklyReturnFileModel);
                                }
                                else
                                {
                                    WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
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

                    foreach (var fileModel in returnFilesToProcess.OrderBy(x => x.FileDateTime))
                    {
                        WriteInfo("Working on return file: " + fileModel.FileName);

                        ProcessPaymentSummary(_context, userProfile, fileModel);
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

                    if (_context.ChangeTracker.HasChanges())
                    {
                        //foreach (var entry in _context.ChangeTracker.Entries())
                        //{
                        //    Console.WriteLine(entry.CurrentValues.GetType());
                        //}

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
        }


        private void CheckPendingClaimsInReturn()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            //var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("80B42BDD-F9FE-4A46-917E-E93E4BD2C0CB"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            _context.Dispose();

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            var createdDate = new DateTime(2024, 2, 1);
            char myBreak = '\n';

            var pendingClaimLineItems = new List<ReturnLineItem>();
            
            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    var myPreviousLines = new List<string>();

                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    var returnLineItems = new List<ReturnLineItem>();

                    var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                    var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                    #region Get BiWeekly Returns and Convert To ReturnLineItems

                    var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbBiWeeklyReturnFileModel.IsSuccess && msbBiWeeklyReturnFileModel.FileNames.Any())
                    {
                        foreach (var msbBiWeeklyReturnFileName in msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                                .Where(x => x.FileDate > new DateTime(2024, 2, 6))
                                    .OrderBy(x => x.FileDate).Select(x => x.FileName))
                        {

                            WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFileName);

                            WriteInfo("The biweekly return file had not been processed, download content");
                            var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFileName);
                            if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                            {
                                var currentReturnLines = RemoveUnRelatedLines(
                                                            biWeeklyReturnFileModel.FileContent.Split(myBreak),
                                                            myDoctorNumber,
                                                            myClinicNumber);
                                var distinctCurrentLines = currentReturnLines.Distinct();
                                var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();

                                var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                                exceptLines.AddRange(keep89Lines);

                                var myReturnLineItems = new List<ReturnLineItem>();

                                foreach (var myLine in exceptLines)
                                {
                                    if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                                    {
                                    }
                                    else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                                    {
                                        //Ignore the lines
                                    }
                                    else
                                    {
                                        var temp = CreateReturnLineItem(myLine);
                                        temp.ReturnFileName = msbBiWeeklyReturnFileName;
                                        temp.ReturnFileDate = GetFileDateTime(msbBiWeeklyReturnFileName).AddHours(6);
                                        myReturnLineItems.Add(temp);
                                    }
                                }

                                var oopLineItems = myReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                                foreach (var lineItem in myReturnLineItems.Where(x => string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                                {
                                    var oopLine = oopLineItems.FirstOrDefault(x => x.CPSClaimNumber == lineItem.CPSClaimNumber);
                                    if (oopLine != null)
                                    {
                                        lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                                        lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                                        lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                                    }
                                    else
                                    {
                                        WriteInfo("Cannot find OOP line for: " + lineItem.CPSClaimNumber);
                                    }
                                }

                                returnLineItems.AddRange(myReturnLineItems);

                                myPreviousLines.AddRange(currentReturnLines);
                            }
                            else
                            {
                                WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                            }
                        }
                    }

                    #endregion

                    if (returnLineItems.Any())
                    {
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

                        foreach (var group in groupByClaimNumberAndHSN)
                        {
                            foreach (var lineItem in group.LineItems)
                            {
                                lineItem.ServiceDate = group.ServiceDate;
                                fixReturnLineItems.Add(lineItem);
                            }
                        }

                        var groupByClaimNumberAndOthers = fixReturnLineItems
                                                .Where(x => x.PatientInfo.HospitalNumber == "902803530")
                                                .GroupBy(x => new { x.ClaimNumber, x.PatientInfo.HospitalNumber, x.ServiceDate })
                                                .Select(x => new
                                                {
                                                    ClaimNumber = x.Key.ClaimNumber,
                                                    HSN = x.Key.HospitalNumber,
                                                    ServiceDate = x.Key.ServiceDate,
                                                    CPSClaimNumbers = x.Select(y => y.CPSClaimNumber).Distinct().ToList(),
                                                    LineItems = x.ToList()
                                                }).ToList();

                        foreach (var group in groupByClaimNumberAndOthers)
                        {
                            var finalLineItems = new List<ReturnLineItem>();

                            WriteInfo(string.Empty);
                            WriteInfo($"Working on - Claim #: {group.ClaimNumber}, HSN: {group.HSN}, Service Date: {group.ServiceDate.ToString("yyyy-MM-dd")}");

                            var targetCPSNumber = group.CPSClaimNumbers.OrderBy(x => x).FirstOrDefault();
                            WriteInfo("Target CPS Number: " + targetCPSNumber);

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

                            //pendingClaimLineItems.AddRange(finalLineItems.Where(x => x.PaidType == PAID_TYPE.PENDING_CLAIMS));
                        }
                    }
                }
            }

            WriteInfo("Pending Line Items: " + pendingClaimLineItems.Count());

            WriteLogToFile("CheckAndRerunClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }


        private void CheckResubmitClaims()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userResubmittedClaims = _context.ClaimsResubmitted.ToList().GroupBy(x => x.UserId).Select(x => new {
                            UserId = x.Key, ResubmittedLineItems = x.ToList() }).ToList();

            foreach(var group in userResubmittedClaims)
            {
                Console.WriteLine("Working On: " + group.UserId);

                var userServiceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == group.UserId && !x.PaidClaimId.HasValue).ToList();

                var claimNumberGroups = group.ResubmittedLineItems.GroupBy(x => x.ClaimNumber).Select(x => new { ClaimNumber = x.Key, CreatedOn = x.Max(y => y.CreatedDate), LineItems = x.ToList() }).ToList();

                foreach(var claimNumberGroup in claimNumberGroups)
                {
                    var foundClaimNumberLineItems = userServiceRecords.Where(x => x.ClaimNumber == claimNumberGroup.ClaimNumber && x.CreatedDate > claimNumberGroup.CreatedOn);

                    if (foundClaimNumberLineItems.Any())
                    {
                        foreach (var resubmittedLineItem in claimNumberGroup.LineItems)
                        {
                            Console.WriteLine($"RESUBMITTED - Claim #: {resubmittedLineItem.ClaimNumber}, HSN: {resubmittedLineItem.HospitalNumber}, P.Last: {resubmittedLineItem.PatientLastName}, S.Date:{resubmittedLineItem.ServiceDate}, Code:{resubmittedLineItem.UnitCode}, Unit:{resubmittedLineItem.UnitNumber}");

                            var foundLineItems = foundClaimNumberLineItems.SelectMany(x => x.UnitRecord).Where(y => y.UnitCode == resubmittedLineItem.UnitCode && y.UnitNumber == resubmittedLineItem.UnitNumber).ToList();
                            if (foundLineItems.Any())
                            {
                                foreach (var found in foundLineItems)
                                {
                                    Console.WriteLine($"FOUND - Claim #: {found.ServiceRecord.ClaimNumber}, HSN: {found.ServiceRecord.HospitalNumber}, P.Last: {found.ServiceRecord.PatientLastName}, S.Date:{found.ServiceRecord.ServiceDate}, Code:{found.UnitCode}, Unit:{found.UnitNumber}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("NOT FOUND");
                            }
                        }
                    }
                }
            }
        }

        private void MergeServiceRecords()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var recordsToMerge = _context.ServiceRecord.Include("UnitRecord").Where(x => x.ClaimStatus != 0 && x.ClaimStatus != 5).GroupBy(x => new { x.UserId, x.ClaimNumber, x.HospitalNumber, x.ServiceDate, x.ClaimStatus })
                                .Select(x => new
                                {
                                    UserId = x.Key.UserId,
                                    ClaimNumber = x.Key.ClaimNumber,
                                    HospitalNumber = x.Key.HospitalNumber,
                                    ServiceDate = x.Key.ServiceDate,
                                    ClaimStatus = x.Key.ClaimStatus,
                                    RecordCount = x.Count(),
                                    Records = x.ToList()
                                })
                                .Where(x => x.RecordCount > 1).ToList();
            
            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            foreach (var group in recordsToMerge.Where(x => !lockOutUsers.Contains(x.UserId)))
            {
                WriteInfo($"Merging - UserId:{group.UserId}, Claim #:{group.ClaimNumber}, HSN:{group.HospitalNumber}, ServiceDate:{group.ServiceDate.ToString("yyyy-MM-dd")}, ClaimStatus:{group.ClaimStatus}");

                foreach (var records in group.Records)
                {
                    var recordToKeep = group.Records.OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth).ThenByDescending(x => x.MessageFromICS).FirstOrDefault();

                    var unitRecords = group.Records.SelectMany(x => x.UnitRecord);

                    RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                    foreach (var recordToRemove in group.Records.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                    {
                        DeleteServiceRecord(recordToRemove);
                    }

                    if (group.ClaimStatus == (int)SearchClaimType.Paid)
                    {
                        recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord);
                        recordToKeep.PaidAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord); 
                    }
                    else
                    {
                        recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                    }

                    SetServiceRecordStateToModified(recordToKeep);
                }
            }

            _context.SaveChanges();

            _context.Dispose();

        }

        private void CheckDuplicateClaimsWithServiceDateAndHSN()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var wantedUserIds = new List<Guid>()
            {
                new Guid("C288884C-46E9-4137-9468-7B115896FE03"),
                new Guid("7B65ABA4-C0A1-4DEC-B404-54E81BA0140B"),
                new Guid("4D67BD8F-24DA-4FDA-A498-785B4DA07FB3"),
                new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9"),
                new Guid("51773f1a-8702-49c1-8f5b-381f32c71cb5"),
                new Guid("40C18E8C-39D2-46F3-8BAF-F8DC361E6721"),
                new Guid("3FDCFE2C-228A-44BC-942E-9429D6BC357C"),
                new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E"),
                new Guid("1D196D61-68C6-4130-968A-6D7AA6CDDC46"),
                new Guid("00873DA3-09A8-4E44-9E67-08C61C4076D1"),
                new Guid("ABCED5EA-722D-4B30-806B-8282AFFF54F2"),
                new Guid("BBB119ED-BC24-48D2-BA07-2F938D01DA1C"),
                new Guid("0A96F7BD-7CB9-4515-9F9E-EAE1E33BB43C"),
                new Guid("CC5F9DB3-F651-45D3-94C9-4AF2B41DB99A"),
                new Guid("7BAFBC50-8DA9-4BB5-B30D-C2C63BEEDD7D")
            };

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");

            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("B93C8221-34C8-4756-8288-A1589C463A34"))

            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9") || x.UserId == new Guid("51773F1A-8702-49C1-8F5B-381F32C71CB5") || 
            //                                x.UserId == new Guid("3FDCFE2C-228A-44BC-942E-9429D6BC357C") || x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E"))

            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2 && wantedUserIds.Contains(x.UserId))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            _context.Dispose();

            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    WriteInfo(string.Empty);
                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    WriteInfo("First Time");
                    RemoveDuplicateLineItemWithHSNAndServiceDate(userProfile.UserId);

                    WriteInfo("Second Time");
                    RemoveDuplicateLineItemWithHSNAndServiceDate(userProfile.UserId);
                }

            }

            WriteLogToFile("CheckDuplicateClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
        }

        private void RemoveDuplicateLineItemWithHSNAndServiceDate(Guid userId)
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var groupByLineItems = _context.ClaimsSearchView.Where(x => x.UserId == userId && x.ClaimStatus != 0 && x.ClaimStatus != 1 && x.ClaimStatus != 5 && x.CPSClaimNumber != null).ToList()
                                    .GroupBy(x => new { x.HospitalNumber, x.ServiceDate, x.UnitCode, x.UnitNumber, x.SubmittedRecordIndex })
                                    .Select(x => new
                                    {
                                        HospitalNumber = x.Key.HospitalNumber,
                                        ServiceDate = x.Key.ServiceDate,
                                        UnitCode = x.Key.UnitCode,
                                        UnitNumber = x.Key.UnitNumber,
                                        SubmittedRecordIndex = x.Key.SubmittedRecordIndex,
                                        RunCode = x.Select(y => y.RunCode).Max(),
                                        ServiceRecordIds = x.Select(y => y.ServiceRecordId).Distinct().ToList(),
                                        RecordCount = x.Count(),
                                        Records = x.ToList()
                                    }).Where(x => x.RecordCount > 1 && x.ServiceRecordIds.Count() > 1).ToList();

            foreach (var group in groupByLineItems)
            {
                WriteInfo(System.Environment.NewLine);
                WriteInfo($"HSN:{group.HospitalNumber}, S.Date:{group.ServiceDate}, U.Code:{group.UnitCode}, U.#:{group.UnitNumber}, U.Idx:{group.SubmittedRecordIndex}, R.C:{group.RunCode} ");

                var serviceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userId && group.ServiceRecordIds.Contains(x.ServiceRecordId)).ToList()
                                        .Where(x => _context.Entry(x).State != System.Data.Entity.EntityState.Deleted).ToList();
               
                //All = -1,
                //Unsubmitted = 0,
                //Submitted = 1,
                //Pending = 2,
                //Paid = 3,
                //Rejected = 4,
                //Deleted = 5

                ServiceRecord wantedServiceRecord = null;

                //Check if there is paid
                IEnumerable<ServiceRecord> multipleServiceRecords = serviceRecords.Where(x => x.ClaimStatus == 3).OrderBy(x => x.CPSClaimNumber);

                if (multipleServiceRecords.Any())
                {
                    wantedServiceRecord = multipleServiceRecords.FirstOrDefault();
                }
                else
                {
                    WriteInfo("NO PAID CLAIM RECORDS");

                    multipleServiceRecords = serviceRecords.Where(x => x.ClaimStatus == 4); //Rejected
                    if (multipleServiceRecords.Any())
                    {
                        wantedServiceRecord = multipleServiceRecords.OrderBy(x => x.CPSClaimNumber).FirstOrDefault();
                    }
                    else
                    {   //Pending
                        wantedServiceRecord = serviceRecords.Where(x => x.ClaimStatus == 2).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();
                    }
                }

                if (wantedServiceRecord != null)
                {
                    WriteInfo($"Wanted: S.RecordId: {wantedServiceRecord.ServiceRecordId}, Claim #:{wantedServiceRecord.ClaimNumber}, ClaimStatus:{wantedServiceRecord.ClaimStatus}");

                    var serviceRecordsToClean = serviceRecords.Where(x => x.ServiceRecordId != wantedServiceRecord.ServiceRecordId).ToList();

                    if (serviceRecordsToClean.Any())
                    {
                        var index = 1;

                        foreach (var needToClean in serviceRecordsToClean)
                        {
                            WriteInfo($"Cleaning: S.RecordId:{needToClean.ServiceRecordId}, Claim #:{needToClean.ClaimNumber}, Claim Status:{needToClean.ClaimStatus}");

                            var unitRecordIdsToRemove = new List<Guid>();
                            foreach (var needToCleanUnitRecord in needToClean.UnitRecord)
                            {
                                if (group.UnitCode == needToCleanUnitRecord.UnitCode &&
                                    group.UnitNumber == needToCleanUnitRecord.UnitNumber &&
                                    group.SubmittedRecordIndex == needToCleanUnitRecord.SubmittedRecordIndex)
                                {
                                    WriteInfo($"Removing: S.RecordId:{needToClean.ServiceRecordId}, U.RecordId:{needToCleanUnitRecord.UnitRecordId}, Unit C: {needToCleanUnitRecord.UnitCode}, Unit #: {needToCleanUnitRecord.UnitNumber}, Prem C:{needToCleanUnitRecord.UnitPremiumCode}, Index:{needToCleanUnitRecord.SubmittedRecordIndex}");
                                    unitRecordIdsToRemove.Add(needToCleanUnitRecord.UnitRecordId);
                                }
                            }

                            if (wantedServiceRecord.ClaimStatus == 3)  //Wanted is Paid
                            {
                                #region Wanted Service Record is Paid

                                //Remove the duplicate unit records from the claims
                                foreach (var id in unitRecordIdsToRemove)
                                {
                                    var unitRecord = needToClean.UnitRecord.FirstOrDefault(x => x.UnitRecordId == id);
                                    SetUnitRecordStateToDeleted(unitRecord);
                                    needToClean.UnitRecord.Remove(unitRecord);
                                }

                                if (!needToClean.UnitRecord.Any())
                                {
                                    SetServiceRecordStateToDeleted(needToClean);
                                }
                                else
                                {
                                    #region Deal with leave over line items in clean up service record, move them to wanted

                                    //There are some line items in the Clean Up Service Record
                                    if (needToClean.ClaimStatus == 3) //Being clean up is paid
                                    {
                                        do
                                        {
                                            var unitRecord = needToClean.UnitRecord.FirstOrDefault();
                                            WriteInfo($"Moving: S.RecordId:{needToClean.ServiceRecordId}, U.RecordId:{unitRecord.UnitRecordId}, Unit C: {unitRecord.UnitCode}, Unit #: {unitRecord.UnitNumber}, Prem C:{unitRecord.UnitPremiumCode}, Index:{unitRecord.SubmittedRecordIndex}");

                                            unitRecord.ServiceRecordId = wantedServiceRecord.ServiceRecordId;

                                            if (unitRecord.PaidAmount == 0)
                                            {
                                                unitRecord.PaidAmount = GetTotalWithPremiumAmount(unitRecord.UnitCode, unitRecord.UnitAmount, unitRecord.UnitPremiumCode, wantedServiceRecord.ServiceLocation);
                                            }

                                            SetUnitRecordStateToModified(unitRecord);
                                        }
                                        while (needToClean.UnitRecord.Count > 0);

                                        RemoveDuplicateUnitRecordsWithRecordIndex(wantedServiceRecord);

                                        wantedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(wantedServiceRecord.UnitRecord, wantedServiceRecord.ServiceLocation);
                                        wantedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(wantedServiceRecord.UnitRecord);
                                        SetServiceRecordStateToModified(wantedServiceRecord);

                                        index = 1;
                                        foreach (var unitRecord in wantedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }

                                        SetServiceRecordStateToDeleted(needToClean);
                                    }
                                    else
                                    {
                                        WriteInfo($"Partial Unit Records Remain: Service Record Id: {needToClean.ServiceRecordId}");

                                        needToClean.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(needToClean.UnitRecord, needToClean.ServiceLocation);
                                        SetServiceRecordStateToModified(needToClean);

                                        index = 1;
                                        foreach (var unitRecord in needToClean.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                        {
                                            unitRecord.RecordIndex = index;
                                            SetUnitRecordStateToModified(unitRecord);

                                            index++;
                                        }
                                    }

                                    #endregion 
                                }

                                #endregion
                            }
                            else
                            {
                                //Wanted is Rejected, Deleted, Pending

                                //If the number of unit records = the number of records being removed
                                if (needToClean.UnitRecord.Count == unitRecordIdsToRemove.Count())
                                {
                                    WriteInfo($"No Remaining Unit Records. Set Claim To Ignore: Service Record Id: {needToClean.ServiceRecordId}");
                                    needToClean.ClaimToIgnore = true;
                                    SetServiceRecordStateToModified(needToClean);
                                }
                                else
                                {
                                    WriteInfo($"Partial Unit Records Remain: Service Record Id: {needToClean.ServiceRecordId}");

                                    foreach (var id in unitRecordIdsToRemove)
                                    {
                                        var unitRecord = needToClean.UnitRecord.FirstOrDefault(x => x.UnitRecordId == id);
                                        SetUnitRecordStateToDeleted(unitRecord);
                                        needToClean.UnitRecord.Remove(unitRecord);
                                    }

                                    needToClean.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(needToClean.UnitRecord, needToClean.ServiceLocation);
                                    SetServiceRecordStateToModified(needToClean);

                                    index = 1;
                                    foreach (var unitRecord in needToClean.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }
                                
                                }
                            }
                        }

                        RemoveDuplicateUnitRecordsWithRecordIndex(wantedServiceRecord);

                        wantedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(wantedServiceRecord.UnitRecord, wantedServiceRecord.ServiceLocation);

                        if (wantedServiceRecord.ClaimStatus == 3)
                        {
                            wantedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(wantedServiceRecord.UnitRecord);
                        }

                        SetServiceRecordStateToModified(wantedServiceRecord);

                        index = 1;
                        foreach (var unitRecord in wantedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }
                    }
                    else
                    {
                        RemoveDuplicateUnitRecordsWithRecordIndex(wantedServiceRecord);
                        wantedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(wantedServiceRecord.UnitRecord, wantedServiceRecord.ServiceLocation);

                        if (wantedServiceRecord.ClaimStatus == 3)
                        {
                            wantedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(wantedServiceRecord.UnitRecord);
                        }

                        var index = 1;
                        foreach (var unitRecord in wantedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        SetServiceRecordStateToModified(wantedServiceRecord);
                    }
                }

            }

            if (_context.ChangeTracker.HasChanges())
            {
                _context.SaveChanges();
            }

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }
        }

        private void CheckDuplicateClaimsWithClaimNumber()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");

            //var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("80B42BDD-F9FE-4A46-917E-E93E4BD2C0CB"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            _context.Dispose();

            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    RemoveDuplicateLineItemsWithClaimNumber(userProfile.UserId);
                }

            }

            WriteLogToFile("CheckDuplicateClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
        }

        private void RemoveDuplicateLineItemsWithClaimNumber(Guid userId)
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var groupByLineItems = _context.ClaimsSearchView.Where(x => x.UserId == userId && x.ClaimStatus != 0)
                                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate, x.UnitCode, x.UnitNumber, x.UnitPremiumCode, x.SubmittedRecordIndex })
                                    .Select(x => new
                                    {
                                        ClaimNumber = x.Key.ClaimNumber,
                                        HospitalNumber = x.Key.HospitalNumber,
                                        ServiceDate = x.Key.ServiceDate,
                                        UnitCode = x.Key.UnitCode,
                                        UnitNumber = x.Key.UnitNumber,
                                        UnitPremCode = x.Key.UnitPremiumCode,
                                        SubmittedRecordIndex = x.Key.SubmittedRecordIndex,
                                        RunCode = x.Select(y => y.RunCode).Max(),
                                        ServiceRecordIds = x.Select(y => y.ServiceRecordId).Distinct().ToList(),
                                        RecordCount = x.Count(),
                                        Records = x.ToList(),
                                    }).Where(x => x.RecordCount > 1).ToList();

            var groupByClaimNumberAndOthers = groupByLineItems
                                                .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate })
                                                .Select(x => new
                                                {
                                                    ClaimNumber = x.Key.ClaimNumber,
                                                    HospitalNumber = x.Key.HospitalNumber,
                                                    ServiceDate = x.Key.ServiceDate,
                                                    LatestRunCode = x.Select(y => y.RunCode).Max(),
                                                    ServiceRecordIds = x.SelectMany(y => y.ServiceRecordIds).Distinct().ToList(),
                                                }).ToList();
                                        
            foreach(var group in groupByClaimNumberAndOthers)
            {
                WriteInfo(System.Environment.NewLine);
                WriteInfo($"Claim #: {group.ClaimNumber}, HSN:{group.HospitalNumber}, ServiceDate:{group.ServiceDate}, Latest RC:{group.LatestRunCode}");

                var serviceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userId && group.ServiceRecordIds.Contains(x.ServiceRecordId)).ToList();

                var serviceRecordIdWithLatestRunCode = serviceRecords.SelectMany(x => x.UnitRecord).Where(y => !string.IsNullOrEmpty(y.RunCode) && y.RunCode.Equals(group.LatestRunCode, StringComparison.OrdinalIgnoreCase)).Select(x => x.ServiceRecordId).Distinct().FirstOrDefault();
                
                ServiceRecord wantedServiceRecord = serviceRecords.FirstOrDefault(x => x.ServiceRecordId == serviceRecordIdWithLatestRunCode);

                if (wantedServiceRecord == null)
                {
                    var records = serviceRecords.OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth);

                    wantedServiceRecord = records.FirstOrDefault(x => x.PaidClaimId.HasValue);
                    if (wantedServiceRecord == null)
                    {
                        WriteInfo("PAID CLAIM NOT FOUND");
                    }
                }

                if (wantedServiceRecord != null)
                {
                    WriteInfo($"Wanted - RecordId: {wantedServiceRecord.ServiceRecordId}, ClaimStatus:{wantedServiceRecord.ClaimStatus}");

                    var serviceRecordsToClean = serviceRecords.Where(x => x.ServiceRecordId != wantedServiceRecord.ServiceRecordId).ToList();

                    if (serviceRecordsToClean.Any())
                    {
                        foreach (var needToClean in serviceRecordsToClean)
                        {
                            var unitRecordIdsToRemove = new List<Guid>();
                            foreach (var needToCleanUnitRecord in needToClean.UnitRecord)
                            {
                                if (wantedServiceRecord.UnitRecord.Any(
                                            x =>
                                                x.UnitCode == needToCleanUnitRecord.UnitCode &&
                                                x.UnitNumber == needToCleanUnitRecord.UnitNumber &&
                                                x.UnitPremiumCode == needToCleanUnitRecord.UnitPremiumCode &&
                                                x.SubmittedRecordIndex == needToCleanUnitRecord.SubmittedRecordIndex))
                                {
                                    WriteInfo($"Removing: Record Id:{needToCleanUnitRecord.UnitRecordId}, Unit C: {needToCleanUnitRecord.UnitCode}, Unit #: {needToCleanUnitRecord.UnitNumber}, Prem C:{needToCleanUnitRecord.UnitPremiumCode}, Index:{needToCleanUnitRecord.SubmittedRecordIndex}");
                                    unitRecordIdsToRemove.Add(needToCleanUnitRecord.UnitRecordId);
                                }
                            }

                            foreach (var id in unitRecordIdsToRemove)
                            {
                                var unitRecord = needToClean.UnitRecord.FirstOrDefault(x => x.UnitRecordId == id);
                                SetUnitRecordStateToDeleted(unitRecord);
                                needToClean.UnitRecord.Remove(unitRecord);

                            }

                            if (!needToClean.UnitRecord.Any())
                            {
                                SetServiceRecordStateToDeleted(needToClean);
                            }
                            else
                            {
                                if (needToClean.PaidClaimId.HasValue)
                                {
                                    do
                                    {
                                        var unitRecord = needToClean.UnitRecord.FirstOrDefault();
                                        WriteInfo($"Moving: Record Id:{unitRecord.UnitRecordId}, Unit C: {unitRecord.UnitCode}, Unit #: {unitRecord.UnitNumber}, Prem C:{unitRecord.UnitPremiumCode}, Index:{unitRecord.SubmittedRecordIndex}");

                                        unitRecord.ServiceRecordId = wantedServiceRecord.ServiceRecordId;

                                        if (unitRecord.PaidAmount == 0)
                                        {
                                            unitRecord.PaidAmount = GetTotalWithPremiumAmount(unitRecord.UnitCode, unitRecord.UnitAmount, unitRecord.UnitPremiumCode, wantedServiceRecord.ServiceLocation);
                                        }

                                        SetUnitRecordStateToModified(unitRecord);
                                    }
                                    while (needToClean.UnitRecord.Count > 0);

                                    wantedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(wantedServiceRecord.UnitRecord, wantedServiceRecord.ServiceLocation);
                                    wantedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(wantedServiceRecord.UnitRecord);
                                    SetServiceRecordStateToModified(wantedServiceRecord);

                                    var index = 1;
                                    foreach (var unitRecord in wantedServiceRecord.UnitRecord.OrderBy(x => x.SubmittedRecordIndex))
                                    {
                                        unitRecord.RecordIndex = index;
                                        SetUnitRecordStateToModified(unitRecord);

                                        index++;
                                    }

                                    SetServiceRecordStateToDeleted(needToClean);
                                }
                            }
                        }
                    }
                    else
                    {
                        RemoveDuplicateUnitRecords(wantedServiceRecord);
                        wantedServiceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSumWithOldUnitRecords(wantedServiceRecord.UnitRecord, wantedServiceRecord.ServiceLocation);
                        wantedServiceRecord.PaidAmount = GetUnitRecordPaidAmountSum(wantedServiceRecord.UnitRecord);
                        SetServiceRecordStateToModified(wantedServiceRecord);
                    }
                }
            }

            if (_context.ChangeTracker.HasChanges())
            {
                _context.SaveChanges();
            }

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }
        }

        private void CheckAndRerunPaidClaims()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("F516D2AD-A9DD-4C57-8370-E309ECC78346"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            _context.Dispose();

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            var createdDate = new DateTime(2024, 2, 1);
            char myBreak = '\n';

            foreach (var userProfile in userProfiles)
            {
                var myPreviousLines = new List<string>();

                WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);
                
                var returnLineItems = new List<ReturnLineItem>();

                var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                #region Get BiWeekly Returns and Convert To ReturnLineItems

                var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                if (msbBiWeeklyReturnFileModel.IsSuccess && msbBiWeeklyReturnFileModel.FileNames.Any())
                {
                    foreach (var msbBiWeeklyReturnFileName in msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) }).Where(x => x.FileDate > new DateTime(2024, 2, 6))
                                .OrderBy(x => x.FileDate).Select(x => x.FileName))
                    {

                        WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFileName);

                        WriteInfo("The biweekly return file had not been processed, download content");
                        var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFileName);
                        if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                        {
                            var currentReturnLines = RemoveUnRelatedLines(
                                                        biWeeklyReturnFileModel.FileContent.Split(myBreak),
                                                        myDoctorNumber,
                                                        myClinicNumber);
                            var distinctCurrentLines = currentReturnLines.Distinct();
                            var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();

                            var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                            exceptLines.AddRange(keep89Lines);

                            var myReturnLineItems = new List<ReturnLineItem>();

                            foreach (var myLine in exceptLines)
                            {
                                if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                                {
                                }
                                else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                                {
                                    //Ignore the lines
                                }
                                else if (myLine.Substring(13, 2) == "P ")
                                {
                                    var temp = CreateReturnLineItem(myLine);
                                    temp.ReturnFileName = msbBiWeeklyReturnFileName;
                                    temp.ReturnFileDate = GetFileDateTime(msbBiWeeklyReturnFileName).AddHours(6);
                                    myReturnLineItems.Add(temp);
                                }
                            }

                            var oopLineItems = myReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                            foreach (var lineItem in myReturnLineItems.Where(x => string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                            {
                                var oopLine = oopLineItems.FirstOrDefault(x => x.CPSClaimNumber == lineItem.CPSClaimNumber);
                                if (oopLine != null)
                                {
                                    lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                                    lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                                    lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                                }
                                else
                                {
                                    WriteInfo("Cannot find OOP line for: " + lineItem.CPSClaimNumber);
                                }
                            }

                            returnLineItems.AddRange(myReturnLineItems);

                            myPreviousLines.AddRange(currentReturnLines);
                        }
                        else
                        {
                            WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                        }
                    }


                }

                #endregion

                var cleanUpParser = new CleanUpParser(_logBuilder);

                cleanUpParser.AdvanceCheckOnClaims(userProfile, returnLineItems);
            }

            WriteLogToFile("AdvanceCheckDuplicateClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }

        private void CheckAndRerunClaims()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("0FD569B8-E5A0-4B52-876F-9EF1F4C3BBA6"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            _context.Dispose();

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            var createdDate = new DateTime(2024, 2, 1);
            char myBreak = '\n';

            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    var myPreviousLines = new List<string>();

                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    var returnLineItems = new List<ReturnLineItem>();

                    var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                    var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                    #region Get BiWeekly Returns and Convert To ReturnLineItems

                    var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                    if (msbBiWeeklyReturnFileModel.IsSuccess && msbBiWeeklyReturnFileModel.FileNames.Any())
                    {
                        foreach (var msbBiWeeklyReturnFileName in msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) })
                                .Where(x => x.FileDate > new DateTime(2024, 2, 6))
                                    .OrderBy(x => x.FileDate).Select(x => x.FileName))
                        {

                            WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFileName);

                            WriteInfo("The biweekly return file had not been processed, download content");
                            var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFileName);
                            if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                            {
                                var currentReturnLines = RemoveUnRelatedLines(
                                                            biWeeklyReturnFileModel.FileContent.Split(myBreak),
                                                            myDoctorNumber,
                                                            myClinicNumber);
                                var distinctCurrentLines = currentReturnLines.Distinct();
                                var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();

                                var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                                exceptLines.AddRange(keep89Lines);

                                var myReturnLineItems = new List<ReturnLineItem>();

                                foreach (var myLine in exceptLines)
                                {
                                    if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                                    {
                                    }
                                    else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                                    {
                                        //Ignore the lines
                                    }
                                    else
                                    {
                                        var temp = CreateReturnLineItem(myLine);
                                        temp.ReturnFileName = msbBiWeeklyReturnFileName;
                                        temp.ReturnFileDate = GetFileDateTime(msbBiWeeklyReturnFileName).AddHours(6);
                                        myReturnLineItems.Add(temp);
                                    }
                                }

                                var oopLineItems = myReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                                foreach (var lineItem in myReturnLineItems.Where(x => string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                                {
                                    var oopLine = oopLineItems.FirstOrDefault(x => x.CPSClaimNumber == lineItem.CPSClaimNumber);
                                    if (oopLine != null)
                                    {
                                        lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                                        lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                                        lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                                    }
                                    else
                                    {
                                        WriteInfo("Cannot find OOP line for: " + lineItem.CPSClaimNumber);
                                    }
                                }

                                returnLineItems.AddRange(myReturnLineItems);

                                myPreviousLines.AddRange(currentReturnLines);
                            }
                            else
                            {
                                WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                            }
                        }
                    }

                    #endregion

                    //Console.WriteLine();
                    //var groupByReturnFile = returnLineItems.Where(x => x.ClaimNumber == 18181) // && x.PatientInfo.HospitalNumber == "644798238" && (x.ServiceDate == new DateTime(2023, 12, 10) || x.ServiceDate == new DateTime(2023, 12, 9))
                    //                                    .GroupBy(x => x.ReturnFileName)
                    //                                    .Select(x => new
                    //                                    {
                    //                                        ReturnFileName = x.Key,
                    //                                        LineItems = x.ToList()
                    //                                    }).ToList();

                    //foreach (var group in groupByReturnFile)
                    //{
                    //    Console.WriteLine("Found In: " + group.ReturnFileName);
                    //    foreach (var lineItem in group.LineItems)
                    //    {
                    //        Console.WriteLine(lineItem.LineContent);
                    //    }

                    //    Console.WriteLine();
                    //}

                    var cleanUpParser = new CleanUpParser(_logBuilder);

                    cleanUpParser.AdvanceCheckOnClaims(userProfile, returnLineItems);
                }
            }

            WriteLogToFile("CheckAndRerunClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }
               
        private void CheckClaimsInReturnFile()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            //var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            //var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");
            //var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("F516D2AD-A9DD-4C57-8370-E309ECC78346"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var pendingClaims = _context.ServiceRecord.Where(x => x.UserId == new Guid("F516D2AD-A9DD-4C57-8370-E309ECC78346") && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber != null).ToList();
            WriteInfo("Pending Claims: " + pendingClaims.Count());

            _context.Dispose();

            WriteInfo("Getting API Service Config Info");
            var config = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            WriteInfo("API Endpoint Base URL: " + config.Url);

            WriteInfo("Initialize ClaimService from APIService");
            var apiService = new ClaimService(config);

            var createdDate = new DateTime(2024, 2, 1);
            char myBreak = '\n';

            foreach (var userProfile in userProfiles)
            {
                var myPreviousLines = new List<string>();

                WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                var returnLineItems = new List<ReturnLineItem>();

                var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                #region Get BiWeekly Returns and Convert To ReturnLineItems

                var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                if (msbBiWeeklyReturnFileModel.IsSuccess && msbBiWeeklyReturnFileModel.FileNames.Any())
                {
                    foreach (var msbBiWeeklyReturnFileName in msbBiWeeklyReturnFileModel.FileNames.Select(x => new { FileName = x, FileDate = GetFileDateTime(x) }).Where(x => x.FileDate > new DateTime(2024, 2, 6))
                                .OrderBy(x => x.FileDate).Select(x => x.FileName))
                    {

                        WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFileName);

                        WriteInfo("The biweekly return file had not been processed, download content");
                        var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFileName);
                        if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                        {
                            var currentReturnLines = RemoveUnRelatedLines(
                                                        biWeeklyReturnFileModel.FileContent.Split(myBreak),
                                                        myDoctorNumber,
                                                        myClinicNumber);
                            var distinctCurrentLines = currentReturnLines.Distinct();
                            var keep89Lines = distinctCurrentLines.Where(x => x.StartsWith("89")).ToList();

                            var exceptLines = distinctCurrentLines.Except(myPreviousLines.Distinct()).ToList();
                            exceptLines.AddRange(keep89Lines);

                            var myReturnLineItems = new List<ReturnLineItem>();

                            foreach (var myLine in exceptLines)
                            {
                                if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                                {
                                }
                                else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                                {
                                    //Ignore the lines
                                }
                                else
                                {
                                    var temp = CreateReturnLineItem(myLine);
                                    temp.ReturnFileName = msbBiWeeklyReturnFileName;
                                    temp.ReturnFileDate = GetFileDateTime(msbBiWeeklyReturnFileName).AddHours(6);
                                    myReturnLineItems.Add(temp);
                                }
                            }

                            var oopLineItems = myReturnLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                            foreach (var lineItem in myReturnLineItems.Where(x => string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                            {
                                var oopLine = oopLineItems.FirstOrDefault(x => x.CPSClaimNumber == lineItem.CPSClaimNumber);
                                if (oopLine != null)
                                {
                                    lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                                    lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                                    lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                                }
                                else
                                {
                                    WriteInfo("Cannot find OOP line for: " + lineItem.CPSClaimNumber);
                                }
                            }

                            returnLineItems.AddRange(myReturnLineItems);

                            myPreviousLines.AddRange(currentReturnLines);
                        }
                        else
                        {
                            WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
                        }
                    }


                }

                #endregion

                foreach (var pc in pendingClaims)
                {
                    WriteInfo($"Checking - Claim #: {pc.ClaimNumber}, S.Date: {pc.ServiceDate.Date}, HSN: {pc.HospitalNumber}, CPS #: {pc.CPSClaimNumber}");
                    var findReturnLineItems = returnLineItems.Where(x => x.ClaimNumber == pc.ClaimNumber && x.ServiceDate == pc.ServiceDate &&
                                    x.PatientInfo.HospitalNumber == pc.HospitalNumber);

                    if (findReturnLineItems.Any())
                    {
                        WriteInfo("CPS Numbers: " + string.Join(",", findReturnLineItems.Select(x => x.CPSClaimNumber).Distinct()));

                        foreach (var item in findReturnLineItems.OrderBy(x => x.CPSClaimNumber).ThenByDescending(x => x.RunCode))
                        {
                            WriteInfo(item.LineContent);
                        }
                    }

                }
            }

            WriteLogToFile("AdvanceCheckDuplicateClaims_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }

        private void CheckDuplicateSubmissions()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9
            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("55D343F1-7EE6-4BC5-B861-1FC6D26686B6"));

            var createdDate = new DateTime(2024, 2, 1);
            char myBreak = '\n';

            foreach (var userProfile in userProfiles)
            {
                var docNumber = userProfile.DoctorNumber.PadLeft(4, '0');

                var claimsInSubmission = _context.ClaimsIn.Where(x => x.UserId == userProfile.UserId && x.SubmittedFileName != "claimsin.txt" && x.Content != null).OrderBy(x => x.CreatedDate).ToList();

                var claimsInLines = new List<ReturnLineItem>();
                foreach (var claimsIn in claimsInSubmission)
                {
                    var allLines = claimsIn.Content.Split(myBreak).Where(x => x.StartsWith("50" + docNumber) || x.StartsWith("57" + docNumber) || x.StartsWith("89" + docNumber))
                                        .Select(x => x + claimsIn.ClaimsInId.ToString()).ToList();

                    var claimInLineItems = allLines.Distinct().Select(x => CreateClaimInLineItems(x));

                    var oopLineItems = claimInLineItems.Where(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                    foreach (var lineItem in claimInLineItems.Where(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.OUT_OF_PROVINCE && string.IsNullOrEmpty(x.PatientInfo.HospitalNumber)))
                    {
                        var oopLine = oopLineItems.FirstOrDefault(x => x.ClaimNumber == lineItem.ClaimNumber);

                        if (oopLine != null)
                        {
                            lineItem.PatientInfo.HospitalNumber = oopLine.PatientInfo.HospitalNumber;
                            lineItem.PatientInfo.LastName = oopLine.PatientInfo.LastName;
                            lineItem.PatientInfo.FirstName = oopLine.PatientInfo.FirstName;
                        }
                    }

                    claimsInLines.AddRange(claimInLineItems.Where(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.OUT_OF_PROVINCE));
                }

                var groupByFields = claimsInLines.GroupBy(x => new { x.ClaimAndSeqNumber, x.PatientInfo.HospitalNumber, x.ServiceDate, x.SubmittedUnitCode, x.SubmittedUnitNumber })
                                                .Select(x => new
                                                {
                                                    ClaimAndSeqNumber = x.Key.ClaimAndSeqNumber,
                                                    HSN = x.Key.HospitalNumber,
                                                    ServiceDate = x.Key.ServiceDate,
                                                    UnitCode = x.Key.SubmittedUnitCode,
                                                    UnitNumber = x.Key.SubmittedUnitNumber,
                                                    LineItems = x.ToList()
                                                }).ToList();

                var duplicatedSubmissions = groupByFields.Where(x => x.LineItems.Count() > 1).ToList();
                                     
            }
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

        private Tuple<IEnumerable<string>, IEnumerable<string>, string> GetPreviousReturnFiles(Guid userId, DateTime maxDailyReturnFileDate, DateTime maxBiWeeklyReturnFileDate)
        {
            //ReturnFileType = 0: Daily Return File
            //ReturnFileType = 1: BiWeekly Return File
            //var claimReturns = _context.ClaimsInReturn.Where(x => x.UserId == userId && x.ReturnFileDate != null && x.ReturnFileName != "" && x.Content != null && x.Content != "" &&
            //                        (x.ReturnFileType == 0 && x.ReturnFileDate > maxDailyReturnFileDate ||
            //                        x.ReturnFileType == 1 && x.ReturnFileDate > maxBiWeeklyReturnFileDate))
            //                    .OrderByDescending(x => x.ReturnFileDate).ToList();

            var claimReturns = _context.ClaimsInReturn.Where(x => x.UserId == Guid.Empty).ToList();

            var previousBiWeeklyReturnFileNames = claimReturns.Where(x => x.ReturnFileType == 1).Select(x => x.ReturnFileName).ToList();

            var previousDailyReturnFileNames = claimReturns.Where(x => x.ReturnFileType == 0).Select(x => x.ReturnFileName).ToList();

            var previousConcatedContent = string.Join(System.Environment.NewLine, claimReturns.Select(x => x.Content.Trim()));

            return new Tuple<IEnumerable<string>, IEnumerable<string>, string>(
                previousDailyReturnFileNames,
                previousBiWeeklyReturnFileNames,
                previousConcatedContent.Trim()
            );
        }

        private void RemoveDuplicateLineItems_Step2_MergeRejected(Guid userId)
        {
            Console.WriteLine("Doing Step 2 - Merge Rejected");

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Merge Rejected Claims
            var rejectedClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userId && !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue)
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate })
                    .Select(x => new GroupItem
                    {
                        ClaimNumber = x.Key.ClaimNumber,
                        HSN = x.Key.HospitalNumber,
                        ServiceDate = x.Key.ServiceDate,
                        RecordCount = x.Count(),
                        RecordList = x.ToList()
                    }).Where(x => x.RecordCount > 1).ToList();

            foreach (var group in rejectedClaimsNeedToMerge)
            {
                var targetCPSNumber = group.RecordList.Select(x => x.CPSClaimNumber).OrderBy(x => x).FirstOrDefault();

                var recordToKeep = group.RecordList.OrderBy(x => x.DateOfBirth).FirstOrDefault();

                var unitRecords = group.RecordList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.RecordList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    _context.Entry(recordToRemove).State = System.Data.Entity.EntityState.Deleted;
                }

                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                recordToKeep.CPSClaimNumber = targetCPSNumber;

                _context.Entry(recordToKeep).State = System.Data.Entity.EntityState.Modified;
            }

            _context.SaveChanges();

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }

        }

        private void RemoveDuplicateLineItems()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");

            //
            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9") || x.UserId == new Guid("51773F1A-8702-49C1-8F5B-381F32C71CB5") || 
            //                                x.UserId == new Guid("3FDCFE2C-228A-44BC-942E-9429D6BC357C") || x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E"))
            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("133889BC-98EB-4EFC-B68E-7252C4197D8F"))
            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    var groupByLineItems = _context.ClaimsSearchView.Where(x => x.CreatedDate > new DateTime(2024, 2, 1) && x.UserId == userProfile.UserId)
                            .GroupBy(x => new { x.ServiceRecordId, x.ClaimAmount, x.HospitalNumber, x.ServiceDate, x.UnitCode, x.UnitNumber })
                            .Select(x => new
                            {
                                ServiceRecordId = x.Key.ServiceRecordId,
                                HospitalNumber = x.Key.HospitalNumber,
                                ServiceDate = x.Key.ServiceDate,
                                UnitCode = x.Key.UnitCode,
                                UnitNumber = x.Key.UnitNumber,
                                RecordCount = x.Count()
                            }).Where(x => x.RecordCount > 1).ToList();

                    var serviceRecordIds = groupByLineItems.Select(x => x.ServiceRecordId).Distinct().ToList();

                    foreach (var serviceRecordId in serviceRecordIds)
                    {
                        var serviceRecord = _context.ServiceRecord.Include("UnitRecord").FirstOrDefault(x => x.ServiceRecordId == serviceRecordId);
                        WriteInfo($"Checking: S.RecordId: {serviceRecord.ServiceRecordId}, Claim #:{serviceRecord.ClaimNumber}, HSN:{serviceRecord.HospitalNumber}, S.Date:{serviceRecord.ServiceDate.ToString("yyyy-MM-dd")}");

                        var groupByUnitCodes = serviceRecord.UnitRecord.GroupBy(x => new { x.UnitCode, x.UnitNumber })
                                .Select(x => new { UnitCode = x.Key.UnitCode, UnitNumber = x.Key.UnitNumber, RecordCount = x.Count(), RecordList = x.ToList() }).ToList();

                        foreach (var group in groupByUnitCodes)
                        {
                            if (group.RecordCount > 1)
                            {
                                var distinctRunCodes = group.RecordList.Select(x => x.RunCode).Distinct();
                                if (distinctRunCodes.Count() > 1)
                                {
                                    var latestRunCode = group.RecordList.Select(x => x.RunCode).Max();

                                    foreach (var record in group.RecordList.OrderByDescending(x => x.RunCode).ThenBy(x => x.UnitCode))
                                    {
                                        WriteInfo($"Unit Code: {record.UnitCode}, Unit #: {record.UnitNumber}, Prem:{record.UnitPremiumCode}, Submitted Record Index:{record.SubmittedRecordIndex}, R Code:{record.RunCode}");
                                    }

                                    foreach (var recordToRemove in group.RecordList.Where(x => x.RunCode != latestRunCode))
                                    {
                                        SetUnitRecordStateToDeleted(recordToRemove);
                                        serviceRecord.UnitRecord.Remove(recordToRemove);
                                    }
                                }

                            }
                        }

                        var index = 1;
                        foreach (var unitRecord in serviceRecord.UnitRecord.Where(x => _context.Entry(x).State != System.Data.Entity.EntityState.Deleted).OrderBy(x => x.SubmittedRecordIndex))
                        {
                            unitRecord.RecordIndex = index;
                            SetUnitRecordStateToModified(unitRecord);

                            index++;
                        }

                        if (serviceRecord.PaidClaimId.HasValue)
                        {
                            serviceRecord.PaidAmount = GetUnitRecordPaidAmountSum(serviceRecord.UnitRecord);
                            serviceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(serviceRecord.UnitRecord);
                            SetServiceRecordStateToModified(serviceRecord);
                        }
                        else
                        {
                            serviceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(serviceRecord.UnitRecord);
                            SetServiceRecordStateToModified(serviceRecord);
                        }
                    }

                    if (_context.ChangeTracker.HasChanges())
                    {
                        _context.SaveChanges();
                    }

                    WriteInfo(System.Environment.NewLine);
                    WriteInfo(System.Environment.NewLine);
                }
            }

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }

            WriteLogToFile("RemoveDuplicateLineItems_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

        }

        private void RemoveDuplicateLineItems_Step2_MergePaid(Guid userId)
        {
            Console.WriteLine("Doing Step 2 - Merge Paid");

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Merge Paid Claim
            var paidClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord").Where(x => x.CreatedDate > new DateTime(2024, 2, 1) && x.UserId == userId && x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue)
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate })
                .Select(x => new
                {
                    ClaimNumber = x.Key.ClaimNumber,
                    HSN = x.Key.HospitalNumber,
                    ServiceDate = x.Key.ServiceDate,
                    RecordCount = x.Count(),
                    RecordList = x.ToList()
                }).Where(x => x.RecordCount > 1).ToList();

            foreach (var group in paidClaimsNeedToMerge)
            {
                var targetCPSNumber = group.RecordList.Select(x => x.CPSClaimNumber).OrderBy(x => x).FirstOrDefault();

                var recordToKeep = group.RecordList.OrderBy(x => x.DateOfBirth).FirstOrDefault();

                var unitRecords = group.RecordList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.RecordList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    _context.Entry(recordToRemove).State = System.Data.Entity.EntityState.Deleted;
                }

                recordToKeep.ClaimAmount = GetUnitRecordSubmittedAmountSum(recordToKeep.UnitRecord);
                recordToKeep.PaidAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                recordToKeep.CPSClaimNumber = targetCPSNumber;

                _context.Entry(recordToKeep).State = System.Data.Entity.EntityState.Modified;
            }

            _context.SaveChanges();

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }

        }

        private void RemoveDuplicateLineItems_Step2_MergePending(Guid userId)
        {
            Console.WriteLine("Doing Step 2 - Merge Pending");

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Merge Pending Claims
            var pendingClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord").Where(x => x.CreatedDate > new DateTime(2024, 2, 1) && x.UserId == userId && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber != null && x.CPSClaimNumber != "")
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate })
                    .Select(x => new
                    {
                        ClaimNumber = x.Key.ClaimNumber,
                        HSN = x.Key.HospitalNumber,
                        ServiceDate = x.Key.ServiceDate,
                        RecordCount = x.Count(),
                        RecordList = x.ToList()
                    }).Where(x => x.RecordCount > 1).ToList();

            foreach (var group in pendingClaimsNeedToMerge)
            {
                var targetCPSNumber = group.RecordList.Select(x => x.CPSClaimNumber).OrderBy(x => x).FirstOrDefault();

                var recordToKeep = group.RecordList.OrderBy(x => x.DateOfBirth).FirstOrDefault();

                var unitRecords = group.RecordList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.RecordList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    _context.Entry(recordToRemove).State = System.Data.Entity.EntityState.Deleted;
                }

                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                recordToKeep.CPSClaimNumber = targetCPSNumber;

                _context.Entry(recordToKeep).State = System.Data.Entity.EntityState.Modified;
            }

            _context.SaveChanges();

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }

        }

        private void RemoveDuplicateLineItems_Step3(Guid userId)
        {
            Console.WriteLine("Doing Step 3");

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var rejectedStatus = (int)SearchClaimType.Rejected;
            var pendingStatus = (int)SearchClaimType.Pending;

            var userProfile = _context.UserProfiles.FirstOrDefault(x => x.UserId == userId);

            var groupByLineItems = _context.ClaimsSearchView.Where(x => x.CreatedDate > new DateTime(2024, 2, 1) && x.UserId == userId)
                                    .GroupBy(x => new { x.ClaimNumber, x.CPSClaimNumber, x.HospitalNumber, x.UnitCode, x.UnitNumber, x.UnitPremiumCode })
                                    .Select(x => new
                                    {
                                        ClaimNumberAndCPSClaimNumber = x.Key.ClaimNumber + "_" + x.Key.CPSClaimNumber,
                                        ClaimNumber = x.Key.ClaimNumber,
                                        CPSClaimNumber = x.Key.CPSClaimNumber,
                                        HospitalNumber = x.Key.HospitalNumber,
                                        UnitCode = x.Key.UnitCode,
                                        UnitNumber = x.Key.UnitNumber,
                                        UnitPremCode = x.Key.UnitPremiumCode,
                                        RecordCount = x.Count()
                                    }).Where(x => x.RecordCount > 1).ToList();

            var claimNumberAndCPSs = groupByLineItems.Select(x => x.ClaimNumberAndCPSClaimNumber).Distinct().ToList();

            foreach (var claimNumberAndCPS in claimNumberAndCPSs)
            {
                Console.WriteLine("Working on: " + claimNumberAndCPS);

                var numberSplit = claimNumberAndCPS.Split('_');
                var claimNumber = int.Parse(numberSplit[0]);
                var cpsNumber = numberSplit[1];

                var serviceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.ClaimNumber == claimNumber && x.CPSClaimNumber == cpsNumber).ToList();

                var serviceRecordsContainLineItems = new List<ServiceRecord>();

                var lineItemsWantToRemove = groupByLineItems.Where(x => x.ClaimNumberAndCPSClaimNumber == claimNumberAndCPS).ToList();

                var isClaimIgnored = serviceRecords.Any(x => x.ClaimToIgnore);

                if (isClaimIgnored)
                {
                    var pendingRecords = serviceRecords.Where(x => !x.ClaimToIgnore && (x.ClaimStatus == pendingStatus || x.ClaimStatus == rejectedStatus));
                    if (pendingRecords.Any())
                    {
                        Console.WriteLine("Removing Pending / Rejected Claims");

                        //Delete All Pendings excluding 
                        foreach (var record in pendingRecords)
                        {
                            do
                            {
                                var unitRecord = record.UnitRecord.FirstOrDefault();
                                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
                            }
                            while (record.UnitRecord.Count() > 0);

                            _context.Entry(record).State = System.Data.Entity.EntityState.Deleted;
                        }
                    }
                }
                else
                {
                    var pendingClaims = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber)).ToList();
                    if (pendingClaims.Any())
                    {
                        foreach (var pendingClaim in pendingClaims)
                        {
                            foreach (var lineItem in lineItemsWantToRemove)
                            {
                                var unitRecord = pendingClaim.UnitRecord.FirstOrDefault(x => x.UnitCode == lineItem.UnitCode && x.UnitNumber == lineItem.UnitNumber && x.UnitPremiumCode == lineItem.UnitPremCode);
                                if (unitRecord != null)
                                {
                                    _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
                                }
                            }

                            if (!pendingClaim.UnitRecord.Any())
                            {
                                _context.Entry(pendingClaim).State = System.Data.Entity.EntityState.Deleted;
                            }
                        }
                    }
                }
            }

            _context.SaveChanges();

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }
        }

        private void RemoveDuplicateLineItems_Step4(Guid userId)
        {
            Console.WriteLine("Doing Step 4");

            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var rejectedStatus = (int)SearchClaimType.Rejected;
            var pendingStatus = (int)SearchClaimType.Pending;

            var groupByLineItems = _context.ClaimsSearchView.Where(x => x.CreatedDate > new DateTime(2024, 2, 1) && x.UserId == userId)
                                    .GroupBy(x => new { x.PatientLastName, x.HospitalNumber, x.ServiceDate, x.UnitCode, x.UnitNumber })
                                    .Select(x => new
                                    {
                                        PatientLastName = x.Key.PatientLastName,
                                        HospitalNumber = x.Key.HospitalNumber,
                                        ServiceDate = x.Key.ServiceDate,
                                        UnitCode = x.Key.UnitCode,
                                        UnitNumber = x.Key.UnitNumber,
                                        RecordCount = x.Count()
                                    }).Where(x => x.RecordCount > 1).ToList();

            foreach (var duplicateLineItem in groupByLineItems)
            {
                Console.WriteLine($"Working on - Last Name: {duplicateLineItem.PatientLastName}, HSN: {duplicateLineItem.HospitalNumber}, ServiceDate: {duplicateLineItem.ServiceDate.ToString("yyyy-MM-dd")}, UnitRecord: {duplicateLineItem.UnitCode}");
                var searchViewLineItems = _context.ClaimsSearchView
                                        .Where(x => 
                                                    x.UserId == userId && 
                                                    x.PatientLastName == duplicateLineItem.PatientLastName && 
                                                    x.HospitalNumber == duplicateLineItem.HospitalNumber &&
                                                    x.ServiceDate == duplicateLineItem.ServiceDate &&
                                                    x.UnitCode == duplicateLineItem.UnitCode && 
                                                    x.UnitNumber == duplicateLineItem.UnitNumber
                                              ).ToList();

                var ids = searchViewLineItems.Select(x => x.ServiceRecordId).Distinct();

                var serviceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => ids.Contains(x.ServiceRecordId)).ToList();

                var paidServiceRecord = serviceRecords.Where(x => x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();

                var rejectedServiceRecord = serviceRecords.Where(x => !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();

                if (paidServiceRecord != null)
                {
                    var remainingServiceRecords = serviceRecords.Where(x => x.ServiceRecordId != paidServiceRecord.ServiceRecordId).ToList();
                    if (remainingServiceRecords.Any())
                    {
                        foreach (var record in remainingServiceRecords)
                        {
                            var unitRecordsToRemove = record.UnitRecord.Where(x =>
                                                        x.UnitCode == duplicateLineItem.UnitCode &&
                                                        x.UnitNumber == duplicateLineItem.UnitNumber).ToList();

                            foreach (var unitRecord in unitRecordsToRemove)
                            {
                                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
                            }

                            if (!record.UnitRecord.Any())
                            {
                                _context.Entry(record).State = System.Data.Entity.EntityState.Deleted;
                            }
                        }
                    }
                }
                else if (rejectedServiceRecord != null)
                {
                    var remainingServiceRecords = serviceRecords.Where(x => x.ServiceRecordId != rejectedServiceRecord.ServiceRecordId).ToList();
                    if (remainingServiceRecords.Any())
                    {
                        foreach (var record in remainingServiceRecords)
                        {
                            var unitRecordsToRemove = record.UnitRecord.Where(x =>
                                                        x.UnitCode == duplicateLineItem.UnitCode &&
                                                        x.UnitNumber == duplicateLineItem.UnitNumber).ToList();

                            foreach (var unitRecord in unitRecordsToRemove)
                            {
                                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
                            }

                            if (!record.UnitRecord.Any())
                            {
                                _context.Entry(record).State = System.Data.Entity.EntityState.Deleted;
                            }
                        }
                    }
                }
                else
                {
                    var pendingServiceRecord = serviceRecords.Where(x => !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && !string.IsNullOrEmpty(x.CPSClaimNumber)).OrderBy(x => x.CPSClaimNumber).FirstOrDefault();
                    var remainingServiceRecords = serviceRecords.Where(x => x.ServiceRecordId != pendingServiceRecord.ServiceRecordId).ToList();
                    if (remainingServiceRecords.Any())
                    {
                        foreach (var record in remainingServiceRecords)
                        {
                            var unitRecordsToRemove = record.UnitRecord.Where(x =>
                                                        x.UnitCode == duplicateLineItem.UnitCode &&
                                                        x.UnitNumber == duplicateLineItem.UnitNumber).ToList();

                            foreach(var unitRecord in unitRecordsToRemove)
                            {
                                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
                            }

                            if (!record.UnitRecord.Any())
                            {
                                _context.Entry(record).State = System.Data.Entity.EntityState.Deleted;
                            }
                        }
                    }
                }
            }                      

            _context.SaveChanges();

            try
            {
                _context.Dispose();
            }
            catch (Exception ex)
            {
                WriteInfo("Dispose Error: " + ex.Message);
            }
        }


        private void GetPaidSummaryFromReprint()
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

            //.Where(x => x.UserId == new Guid("B2333D6B-FAC5-4D7E-BE4C-BD4DC7C0C873")) //C86EFBC1-ECCD-4CB7-8C57-1652983D8C42 //d8c7f68a-e7fe-4362-9161-c085caff86c9
            //.Where(x => x.UserId == new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9")) //new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E") //51773f1a-8702-49c1-8f5b-381f32c71cb5
            //29628C01-497E-423C-B711-F6261AA407AA //BBFA3A4A-004A-48AF-A7BA-EE64F70700CA //51773F1A-8702-49C1-8F5B-381F32C71CB5

            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");

            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2 && x.UserId == new Guid("51773F1A-8702-49C1-8F5B-381F32C71CB5"));

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            var wantedReprintDate = new DateTime(2024, 7, 1);
            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    WriteInfo(string.Empty);
                    WriteInfo("Getting returns for:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);
                    WriteInfo("Get Previous Return Files");

                    try
                    {
                        var returnFilesToProcess = new List<ReturnFileModel>();

                        WriteInfo("Getting BiWeekly Return Files from MSB API");
                        var msbBiWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);
                        if (msbBiWeeklyReturnFileModel.IsSuccess)
                        {
                            //.Where(x => x.Contains("_REPRINT_"))
                            var biWeeklyReturnFiles = msbBiWeeklyReturnFileModel.FileNames.OrderByDescending(x => x).OrderBy(x => x)
                                .Select(x => new { FileName = x, FileDate = GetFileDateTime(x) });
                            if (biWeeklyReturnFiles.Any())
                            {
                                foreach (var msbBiWeeklyReturnFile in biWeeklyReturnFiles.OrderBy(x => x.FileDate).Select(x => x.FileName))
                                {
                                    WriteInfo("Checking MSB BiWeekly Return File: " + msbBiWeeklyReturnFile);

                                    var biWeeklyReturnFileModel = apiService.GetBiWeeklyReturnFile(userProfile.GroupUserKey, msbBiWeeklyReturnFile);
                                    if (biWeeklyReturnFileModel.IsSuccess && !string.IsNullOrEmpty(biWeeklyReturnFileModel.FileContent))
                                    {
                                        biWeeklyReturnFileModel.FileDateTime = GetFileDateTime(msbBiWeeklyReturnFile);
                                        returnFilesToProcess.Add(biWeeklyReturnFileModel);
                                    }
                                    else
                                    {
                                        WriteInfo("Unable to get biWeekly file content: " + biWeeklyReturnFileModel.ErrorMessage);
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

                        foreach (var fileModel in returnFilesToProcess.OrderBy(x => x.FileDateTime))
                        {
                            WriteInfo("Working on return file: " + fileModel.FileName);

                            //ProcessPaymentSummary(_context, userProfile, fileModel);
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

                        if (_context.ChangeTracker.HasChanges())
                        {
                            //foreach (var entry in _context.ChangeTracker.Entries())
                            //{
                            //    Console.WriteLine(entry.CurrentValues.GetType());
                            //}

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
        }

        private void ProcessPaymentSummary(MedicalBillingSystemEntities context, UserProfiles userProfile, ReturnFileModel returnFileModel)
        {
            char myBreak = '\n';
            var userId = userProfile.UserId;
            string[] myLines = returnFileModel.FileContent.Split(myBreak);
            var myTotalLines = new List<ClaimsReturnPaymentSummary>();

            if (myLines.Count() > 0)
            {
                var processedLines = RemoveUnRelatedLines(myLines, userProfile.DoctorNumber.PadLeft(4, '0'), userProfile.ClinicNumber.PadLeft(3, '0'));
                var myLine = string.Empty;
                var groupIndex = 1;
                for (var i = 0; i < processedLines.Count(); i++)
                {
                    myLine = myLines[i];

                    if (!string.IsNullOrEmpty(myLine))
                    {
                        if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                        {
                            var totalLine = new ClaimsReturnPaymentSummary();
                            totalLine.UserId = userProfile.UserId;
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
                    }
                }

                ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                myClaimsInReturn.ReturnFooter = string.Empty;
                myClaimsInReturn.ReturnFileType = (int)returnFileModel.ReturnFileType;
                myClaimsInReturn.ReturnFileName = returnFileModel.FileName;
                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.ReturnFileDate = GetFileDateTime(returnFileModel.FileName);
                myClaimsInReturn.TotalApproved = 0;
                myClaimsInReturn.TotalSubmitted = 0;
                myClaimsInReturn.TotalPaidAmount = 0;
                myClaimsInReturn.TotalPremiumAmount = 0;
                myClaimsInReturn.OtherFeeAndPayment = 0;
                myClaimsInReturn.RunCode = string.Empty;
                myClaimsInReturn.UploadDate = DateTime.UtcNow;
                myClaimsInReturn.UserId = userProfile.UserId;
                myClaimsInReturn.Content = returnFileModel.FileContent;

                if (myTotalLines.Any())
                {
                    foreach (var myTotal in myTotalLines)
                    {
                        if (!myTotal.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase))
                        {
                            myClaimsInReturn.TotalSubmitted += myTotal.FeeSubmitted;
                            myClaimsInReturn.TotalApproved += myTotal.FeeApproved;
                            myClaimsInReturn.TotalPremiumAmount += myTotal.TotalPremiumAmount;
                            myClaimsInReturn.TotalProgramAmount += myTotal.TotalProgramAmount;
                        }

                        myTotal.ClaimsInReturnId = myClaimsInReturn.ClaimsInReturnId;
                        myClaimsInReturn.ClaimsReturnPaymentSummary.Add(myTotal);
                        _context.Entry(myTotal).State = System.Data.Entity.EntityState.Added;
                    }

                    myClaimsInReturn.TotalPaidAmount = myTotalLines.Where(x => x.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase)).Sum(x => x.TotalPaidAmount);

                    var runCodeInTotalPaidLine = myTotalLines.FirstOrDefault(x => x.TotalLineType.Equals("TOTAL PAID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.RunCode));
                    if (runCodeInTotalPaidLine != null)
                    {
                        myClaimsInReturn.RunCode = runCodeInTotalPaidLine.RunCode;
                    }

                    myClaimsInReturn.OtherFeeAndPayment = 0;
                }

                _context.Entry(myClaimsInReturn).State = System.Data.Entity.EntityState.Added;

                _context.SaveChanges();
            }
        }



        private void SetPaymentApproveDate()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null).OrderBy(x => x.DoctorNumber).ToList();
            var paymentApproveDateLimit = new DateTime(2024, 2, 13);

            var builder = new StringBuilder();
            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);
                var paidClaimIds = _context.ServiceRecord.Where(x => x.UserId == userProfile.UserId && x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.ClaimType == 0 &&
                                x.CreatedDate > paymentApproveDateLimit).Select(x => x.PaidClaimId).Distinct();

                builder.Append($"-- {userProfile.DoctorNumber} - {userProfile.DoctorName}: {userProfile.UserId} --").Append(System.Environment.NewLine);
                foreach(var paidClaimId in paidClaimIds)
                {
                    var returnFileDate = _context.PaidClaim.Include("ClaimsInReturn").FirstOrDefault(x => x.PaidClaimId == paidClaimId).ClaimsInReturn.ReturnFileDate;
                    if (returnFileDate.HasValue)
                    { 
                        var paymentApproveDate = returnFileDate.Value.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
                        builder.Append($"UPDATE ServiceRecord Set PaymentApproveDate = '{paymentApproveDate}' WHERE PaidClaimId ='{paidClaimId}' AND UserId = '{userProfile.UserId}';");
                        builder.Append(System.Environment.NewLine);
                    }
                    else
                    {
                        Console.WriteLine("Paid Claim Id:" + paidClaimId);
                    }
                }

                builder.Append(System.Environment.NewLine);
                builder.Append(System.Environment.NewLine);
            }

            WriteToFile("SetPaymentApproveDate.sql", builder.ToString());
        }
       
        private void CheckLeeRejectedClaim()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userId = new Guid("FE5DE5B9-7DDB-42EF-A1DE-3CA0378E41B3");

            var dateCreated = new DateTime(2024, 2, 11);

            var rejectedClaims = _context.ServiceRecord.Where(x => x.UserId == userId && x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && x.CreatedDate > dateCreated).ToList();

            foreach(var rejected in rejectedClaims)
            {
                var message = "Checking Claim #: " + rejected.ClaimNumber + ", ";

                var matchedPaidClaims = _context.ServiceRecord.Where(x => x.UserId == userId && x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.ClaimNumber == rejected.ClaimNumber);

                if (matchedPaidClaims.Any())
                {
                    if (matchedPaidClaims.Any(x => x.HospitalNumber == rejected.HospitalNumber))
                    {
                        message += "Found Paid Claim with HSN matched";
                    }
                    else
                    {
                        message += "Found Paid Claim with Claim Number Only";
                    }
                }
                else
                {
                    message += "No Paid Claim Found";
                }

                Console.WriteLine(message);

            }
        }

        private void GenerateSQLForPaidClaim()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != "").ToList();

            var builder = new StringBuilder();
            
            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                var paidClaimIds = _context.ClaimsInReturn.Include("PaidClaim").Where(x => x.UserId == userProfile.UserId && x.ReturnFileName != null).SelectMany(x => x.PaidClaim).Select(x => x.PaidClaimId).ToList();
                if (paidClaimIds.Any())
                {
                    var index = 1;
                    var total = paidClaimIds.Count();
                    foreach(var paidClaimId in paidClaimIds)
                    {
                        WriteInfo("Working on paid claim batch: " + index + " out of " + total);
                        var paidServiceRecords = _context.ServiceRecord.Include("UnitRecord").Where(x => x.UserId == userProfile.UserId && x.PaidClaimId == paidClaimId).ToList();

                        foreach(var record in paidServiceRecords)
                        {
                            var unitRecordsNeedToFix = record.UnitRecord.Where(x => !x.SubmittedAmount.HasValue).ToList();
                            foreach(var unitRecord in unitRecordsNeedToFix)
                            {
                                unitRecord.SubmittedAmount = unitRecord.UnitAmount;
                                builder.Append($"UPDATE UnitRecord SET SubmittedAmount = {unitRecord.UnitAmount} WHERE UnitRecordId = '{unitRecord.UnitRecordId}';");
                                builder.Append(System.Environment.NewLine);
                            }

                            builder.Append($"UPDATE ServiceRecord Set ClaimAmount = {GetUnitRecordSubmittedAmountSum(record.UnitRecord)} WHERE ServiceRecordId = '{record.ServiceRecordId}' ;");
                            builder.Append(System.Environment.NewLine);
                        }

                        index++;
                    }
                }
                else
                {
                    Console.WriteLine("No Return");
                }
            }

            WriteToFile("SetCorrectClaimAmountOnPaidClaim.sql", builder.ToString());
        }

        private double GetUnitRecordSubmittedAmountSum(IEnumerable<UnitRecord> records)
        {
            return records.Where(x => x.SubmittedAmount.HasValue).Select(x => GetTotalWithPremiumAmount(x.UnitCode, x.SubmittedAmount.Value, x.UnitPremiumCode, string.Empty)).Sum();
        }

        private double GetUnitRecordSubmittedAmountSumWithOldUnitRecords(IEnumerable<UnitRecord> records, string serviceLocation)
        {
            double result = 0;

            foreach(var record in records)
            {
                if (record.SubmittedAmount.HasValue)
                {
                    result += GetTotalWithPremiumAmount(record.UnitCode, record.SubmittedAmount.Value, record.UnitPremiumCode, serviceLocation);
                }
                else
                {
                    result += GetTotalWithPremiumAmount(record.UnitCode, record.UnitAmount, record.UnitPremiumCode, serviceLocation);
                }
            }

            return result;
        }


        private double GetTotalWithPremiumAmount(string unitCode, double unitAmount, string locationOfService, string serviceLocation)
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

        private double GetUnitRecordPaidAmountSum(IEnumerable<UnitRecord> records)
        {
            return Math.Round(records.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
        }

        private void MergePaidClaimsForUser()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null && x.UserId == new Guid("46289d8b-be7f-4daa-889c-53f07147e42e")).ToList();

            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                MergePaidClaims(userProfile.UserId);

                if (_context.ChangeTracker.HasChanges())
                {
                    WriteInfo("Save Changes");

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

                        if (_context.ChangeTracker.HasChanges())
                        {
                            foreach (var entry in _context.ChangeTracker.Entries())
                            {
                                Console.WriteLine(entry.CurrentValues.GetType());
                            }

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
                }
                else
                {
                    WriteInfo("No changes to save");
                }
            }

            WriteLogToFile("MergePaidClaimsLogs_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
        }

        private void MergeRejectedClaimsForUser()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E")).OrderBy(x => x.DoctorName).ToList();

            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                MergeRejectedClaims(userProfile.UserId);

                if (_context.ChangeTracker.HasChanges())
                {
                    WriteInfo("Save Changes");

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

                        if (_context.ChangeTracker.HasChanges())
                        {
                            foreach (var entry in _context.ChangeTracker.Entries())
                            {
                                Console.WriteLine(entry.CurrentValues.GetType());
                            }

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

                }
                else
                {
                    WriteInfo("No Duplicate Claims");
                }
            }

            WriteLogToFile("MergeRejectedClaimsLogs.txt");
        }

        private void MergePendingClaimsForUser()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null).OrderBy(x => x.DoctorName).ToList();

            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                MergePendingClaims(userProfile.UserId);

                if (_context.ChangeTracker.HasChanges())
                {
                    WriteInfo("Save Changes");

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

                        if (_context.ChangeTracker.HasChanges())
                        {
                            foreach (var entry in _context.ChangeTracker.Entries())
                            {
                                Console.WriteLine(entry.CurrentValues.GetType());
                            }

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

                }
                else
                {
                    WriteInfo("No Duplicate Claims");
                }
            }

            WriteLogToFile("MergePendingClaimsLogs.txt");
        }

        private void CheckPendingClaimInPaidOrRejected()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null && x.UserId == new Guid("46289d8b-be7f-4daa-889c-53f07147e42e")).OrderBy(x => x.DoctorName).ToList();

            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                MergePendingClaimInPaidOrRejected(userProfile.UserId);

                if (_context.ChangeTracker.HasChanges())
                {
                    WriteInfo("Save Changes");

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

                        if (_context.ChangeTracker.HasChanges())
                        {
                            foreach (var entry in _context.ChangeTracker.Entries())
                            {
                                Console.WriteLine(entry.CurrentValues.GetType());
                            }

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

                }
                else
                {
                    WriteInfo("No Duplicate Claims");
                }
            }

            WriteLogToFile("CheckPendingClaimsLogs.txt");
        }

        private void BackfillClaimsInId()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null && x.UserId == new Guid("D8C7F68A-E7FE-4362-9161-C085CAFF86C9")).ToList();

            var builder = new StringBuilder();
            foreach (var userProfile in userProfiles)
            {
                WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                SetClaimsInIdBackOnClaims(userProfile, builder);
            }

            WriteToFile("SetClaimInIds.sql", builder.ToString());
        }

        private void SetDOBOnRejectedClaims(UserProfiles userProfile)
        {
            char myBreak = '\n';
            var dateLimit = new DateTime(2024, 2, 11);

            WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

            var docNumber = userProfile.DoctorNumber.PadLeft(4, '0');

            var claimsInContent = _context.ClaimsIn.Where(x => x.UserId == userProfile.UserId && x.SubmittedFileName != "claimsin.txt" && x.Content != null).OrderBy(x => x.CreatedDate)
                    .Select(x => x.Content).ToList();

            var claimsInLines = new List<string>();
            foreach(var content in claimsInContent)
            {
                claimsInLines.AddRange(content.Split(myBreak).Where(x => x.StartsWith("50" + docNumber) || x.StartsWith("57" + docNumber) || x.StartsWith("89" + docNumber)).ToList());
            }

            var groups = claimsInLines.Select(x => CreateClaimInLineItems(x)).GroupBy(x => x.ClaimNumber).Select(x => new { ClaimNumber = x.Key, Records = x.ToList() }).ToList();

            var claimNumberGroups = new List<ClaimNumberGroup>();

            foreach (var group in groups)
            { 
                var item = new ClaimNumberGroup()
                {
                    ClaimNumber = group.ClaimNumber,
                    ReturnLineItems = group.Records
                };

                var patientInfo = group.Records.FirstOrDefault(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.OUT_OF_PROVINCE).PatientInfo;

                var oopPatientLineItem = group.Records.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);
                    
                if (oopPatientLineItem != null)
                {
                    patientInfo.HospitalNumber = oopPatientLineItem.PatientInfo.HospitalNumber;
                }

                item.ClaimPatientInfo = patientInfo;

                claimNumberGroups.Add(item);
            }

            var rejectedClaims = _context.ServiceRecord.Where(x => x.UserId == userProfile.UserId && x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue && x.CreatedDate > dateLimit).ToList();

            foreach (var claim in rejectedClaims.Where(x => x.CreatedDate.AddHours(-6).Date == x.DateOfBirth.Date))
            {
                Console.WriteLine("Checking Rejected Claim: " + claim.ClaimNumber);

                var claimIn = claimNumberGroups.FirstOrDefault(x => x.ClaimNumber == claim.ClaimNumber && x.ClaimPatientInfo.HospitalNumber == claim.HospitalNumber);
                if (claimIn != null)
                {
                    claim.DateOfBirth = claimIn.ClaimPatientInfo.BirthDate;
                    claim.Sex = claimIn.ClaimPatientInfo.Sex;
                    _context.Entry(claim).State = System.Data.Entity.EntityState.Modified;
                }
            }
                
            if (_context.ChangeTracker.HasChanges())
            {
                WriteInfo("Save Changes");

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

                    if (_context.ChangeTracker.HasChanges())
                    {
                        foreach (var entry in _context.ChangeTracker.Entries())
                        {
                            Console.WriteLine(entry.CurrentValues.GetType());
                        }

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
            }
        }

        private void FindClaimInClaimsInFiles()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfile = _context.UserProfiles.FirstOrDefault(x => x.GroupUserKey != null && x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E"));

            char myBreak = '\n';
            var dateLimit = new DateTime(2024, 2, 1);

            WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

            var docNumber = userProfile.DoctorNumber.PadLeft(4, '0');

            var claimsInSubmission = _context.ClaimsIn.Where(x => x.UserId == userProfile.UserId && x.SubmittedFileName != "claimsin.txt" && x.Content != null).OrderBy(x => x.CreatedDate).ToList();

            var claimsInLines = new List<string>();
            foreach (var claimsIn in claimsInSubmission)
            {
                var allLines = claimsIn.Content.Split(myBreak).Where(x => x.StartsWith("50" + docNumber) || x.StartsWith("57" + docNumber) || x.StartsWith("89" + docNumber))
                                    .Select(x => x + claimsIn.CreatedDate.AddHours(-6).ToString("yyyy-MM-dd HH:mm:ss")).ToList();
                claimsInLines.AddRange(allLines);
            }

            var groups = claimsInLines.Select(x => CreateClaimInLineItems(x)).GroupBy(x => x.ClaimNumber).Select(x => new { ClaimNumber = x.Key, Records = x.ToList() }).ToList();

            var claimNumberGroups = new List<ClaimNumberGroup>();

            foreach (var group in groups)
            {
                var item = new ClaimNumberGroup()
                {
                    ClaimNumber = group.ClaimNumber,
                    ReturnLineItems = group.Records
                };

                var patientInfo = group.Records.FirstOrDefault(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.OUT_OF_PROVINCE).PatientInfo;

                var oopPatientLineItem = group.Records.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                if (oopPatientLineItem != null)
                {
                    patientInfo.HospitalNumber = oopPatientLineItem.PatientInfo.HospitalNumber;
                }

                item.ClaimPatientInfo = patientInfo;
                item.FirstLineItem = item.ReturnLineItems.FirstOrDefault();

                claimNumberGroups.Add(item);
            }

            foreach(var claim in claimNumberGroups.Where(x => x.ClaimNumber == 44972))
            {
                var constructLines = claim.ClaimLines.Select(x => new { CreatedOn = x.Substring(x.Length - 19), Line = x }).ToList();

                foreach(var line in constructLines.OrderBy(x => x.CreatedOn))
                {
                    Console.WriteLine(line.Line);
                }
            }

        }

        private void SetClaimsInIdBackOnClaims(UserProfiles userProfile, StringBuilder builder)
        {
            char myBreak = '\n';
            var dateLimit = new DateTime(2024, 2, 1);

            WriteInfo("Working For:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

            var docNumber = userProfile.DoctorNumber.PadLeft(4, '0');

            var claimsInSubmission = _context.ClaimsIn.Where(x => x.UserId == userProfile.UserId && x.SubmittedFileName != "claimsin.txt" && x.Content != null).OrderBy(x => x.CreatedDate).ToList();
          
            var claimsInLines = new List<string>();
            foreach (var claimsIn in claimsInSubmission)
            {
                var allLines = claimsIn.Content.Split(myBreak).Where(x => x.StartsWith("50" + docNumber) || x.StartsWith("57" + docNumber) || x.StartsWith("89" + docNumber))
                                    .Select(x => x + claimsIn.ClaimsInId.ToString()).ToList();
                claimsInLines.AddRange(allLines);
            }

            var groups = claimsInLines.Select(x => CreateClaimInLineItems(x)).GroupBy(x => x.ClaimNumber).Select(x => new { ClaimNumber = x.Key, Records = x.ToList() }).ToList();

            var claimNumberGroups = new List<ClaimNumberGroup>();

            foreach (var group in groups)
            {
                var item = new ClaimNumberGroup()
                {
                    ClaimNumber = group.ClaimNumber,
                    ReturnLineItems = group.Records
                };

                var patientInfo = group.Records.FirstOrDefault(x => x.ReturnedRecordType != RETURNED_RECORD_TYPE.OUT_OF_PROVINCE).PatientInfo;

                var oopPatientLineItem = group.Records.FirstOrDefault(x => x.ReturnedRecordType == RETURNED_RECORD_TYPE.OUT_OF_PROVINCE);

                if (oopPatientLineItem != null)
                {
                    patientInfo.HospitalNumber = oopPatientLineItem.PatientInfo.HospitalNumber;
                }

                item.ClaimPatientInfo = patientInfo;
                item.FirstLineItem = item.ReturnLineItems.FirstOrDefault();

                claimNumberGroups.Add(item);
            }

            var claimsMissingClaimsInId = _context.ServiceRecord.Where(x => x.UserId == userProfile.UserId && x.CreatedDate > dateLimit && !x.ClaimsInId.HasValue).ToList();

            foreach (var claim in claimsMissingClaimsInId)
            {
                Console.WriteLine("Checking Claim: " + claim.ClaimNumber);

                var claimIns = claimNumberGroups.Where(x => x.ClaimNumber == claim.ClaimNumber && x.ClaimPatientInfo.HospitalNumber == claim.HospitalNumber);
                if (claimIns.Any())
                {
                    var claimsInsMatchServiceDate = claimIns.FirstOrDefault(x => x.FirstLineItem.ServiceDate == claim.ServiceDate);
                    if (claimsInsMatchServiceDate != null)
                    {
                        var lineContent = claimsInsMatchServiceDate.FirstLineItem.LineContent;
                        var claimsInId = new Guid(lineContent.Substring(lineContent.Length - 36));
                        builder.Append($"UPDATE ServiceRecord SET ClaimsInId = '{claimsInId}' WHERE ServiceRecordId = '{claim.ServiceRecordId}';");
                        builder.Append(System.Environment.NewLine);
                    }
                }
            }
        }

        private ReturnLineItem CreateClaimInLineItems(string myLine)
        {
            ReturnLineItem myLineItem = new ReturnLineItem();
            myLineItem.LineContent = myLine;

            if (myLine.StartsWith("50") || myLine.StartsWith("57"))
            {
                myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.VISIT_PROCEDURE;
                myLineItem.ClaimNumber = int.Parse(myLine.Substring(6, 5));
                myLineItem.SubmittedUnitCode = myLine.Substring(67, 4).TrimStart('0');
                myLineItem.SubmittedUnitNumber = int.Parse(myLine.Substring(64, 2));
                myLineItem.ServiceDate = new DateTime(
                            int.Parse("20" + myLine.Substring(62, 2)),
                            int.Parse(myLine.Substring(60, 2)),
                            int.Parse(myLine.Substring(58, 2)));
                myLineItem.ReturnFileName = myLine.Substring(90).Trim();
                
                var patientInfo = new PatientInfo();
                patientInfo.HospitalNumber = myLine.Substring(12, 9).Trim();

                var name = myLine.Substring(26, 25);
                var index = name.IndexOf(",");
                if (index > -1)
                {
                    patientInfo.LastName = name.Split(',')[0].Trim();
                    patientInfo.FirstName = name.Split(',')[1].Trim();
                }
                else
                {
                    patientInfo.LastName = name.Trim();
                    patientInfo.FirstName = string.Empty;
                }

                patientInfo.BirthDate = new DateTime(
                                int.Parse(DateTime.Now.Year.ToString().Substring(0, 2) + myLine.Substring(23, 2)),
                                int.Parse(myLine.Substring(21, 2)),
                                1);
                patientInfo.Sex = myLine.Substring(25, 1).Trim();

                if (patientInfo.BirthDate > DateTime.Now)
                {
                    patientInfo.BirthDate = patientInfo.BirthDate.AddYears(-100);
                }
                
                myLineItem.PatientInfo = patientInfo;
            }
            else 
            {
                myLineItem.ClaimNumber = int.Parse(myLine.Substring(6, 5));
                myLineItem.ReturnedRecordType = RETURNED_RECORD_TYPE.OUT_OF_PROVINCE;
                var patientInfo = new PatientInfo();
                patientInfo.HospitalNumber = myLine.Substring(51, 12).Trim();
                patientInfo.LastName = myLine.Substring(23, 18).Trim();
                patientInfo.FirstName = myLine.Substring(41, 9).Trim();

                myLineItem.PatientInfo = patientInfo;
            }

            return myLineItem;
        }
        
        private void Test1()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfile = _context.UserProfiles.FirstOrDefault(x => x.GroupUserKey != null && x.UserId == new Guid("46289D8B-BE7F-4DAA-889C-53F07147E42E"));

            SetDOBOnRejectedClaims(userProfile);
        }

        private void TestGroupUserKeyInUserProfiles()
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

            var userProfiles = _context.UserProfiles.Where(x => x.GroupUserKey != null).ToList();

            foreach (var profile in userProfiles)
            {
                var returnFileModel = apiService.GetDailyReturnFileList(profile.GroupUserKey, profile.GroupNumber);
                if (!returnFileModel.IsSuccess)
                {
                    Console.WriteLine(profile.DoctorNumber + " - " + profile.GroupNumber + " - " + profile.GroupUserKey + " " + returnFileModel.ErrorMessage);
                }
                else
                {
                    Console.WriteLine(profile.DoctorNumber + " - " + profile.GroupNumber + " - " + profile.GroupUserKey + " " + returnFileModel.FileNames.Count());
                }
            }
        }

        private void TestGroupUserKey()
        {
            var records = GetCSVContent("Current Perspect Users with New Clinic Number - 2024-02-09.csv");

            var headerCols = records.FirstOrDefault();
            var items = records.Skip(1).ToList();


            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var builder = new StringBuilder();

            foreach (var col in headerCols)
            {
                builder.Append(col).Append(",");
            }

            builder.Append("Work Group User Key").Append(System.Environment.NewLine);

            foreach (var item in items)
            {
                Console.WriteLine("Working On: " + item[2]);

                foreach (var cell in item)
                {
                    builder.Append(CSVSafe(cell)).Append(",");
                }

                var groupNumber = item[17];

                var groupUserKeyCellTestSequence = new[] { 20, 18, 19 };
                //Navin - 20
                //Ben - 19
                //Colin - 18

                var testResult = "NONE OF THEM WORKING";
                foreach (var seq in groupUserKeyCellTestSequence)
                {
                    var keyToTest = item[seq];
                    if (!string.IsNullOrEmpty(keyToTest))
                    {
                        var returnFileModel = apiService.GetDailyReturnFileList(keyToTest, groupNumber);
                        if (returnFileModel.IsSuccess)
                        {
                            testResult = keyToTest;
                            break;
                        }

                    }
                }

                builder.Append(testResult);
                builder.Append(System.Environment.NewLine);
            }

            WriteToFile("GroupUserKeyTestResult.csv", builder.ToString());
        }

        private void TestAllGroupUserKey()
        {
            var records = GetCSVContent("Current Perspect Users with New Clinic Number - 2024-02-09.csv");

            var headerCols = records.FirstOrDefault();
            var items = records.Skip(1).ToList();


            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var builder = new StringBuilder();

            foreach (var col in headerCols)
            {
                builder.Append(col).Append(",");
            }

            builder.Append("Test Result").Append(System.Environment.NewLine);

            foreach (var item in items)
            {
                Console.WriteLine("Working On: " + item[2]);

                foreach (var cell in item)
                {
                    builder.Append(CSVSafe(cell)).Append(",");
                }

                var groupNumber = item[17];

                var groupUserKeyCellTestSequence = new[] { 20, 18, 19 };
                //Navin - 20
                //Ben - 19
                //Colin - 18

                var testResult = "";
                foreach (var seq in groupUserKeyCellTestSequence)
                {
                    var keyToTest = item[seq];
                    if (!string.IsNullOrEmpty(keyToTest))
                    {
                        var returnFileModel = apiService.GetDailyReturnFileList(keyToTest, groupNumber);
                        if (returnFileModel.IsSuccess)
                        {
                            testResult += keyToTest + " - Work" + System.Environment.NewLine;
                        }
                        else
                        {
                            testResult += keyToTest + " - Not Work - " + returnFileModel.ErrorMessage + System.Environment.NewLine;
                        }
                    }
                }

                builder.Append(CSVSafe(testResult));
                builder.Append(System.Environment.NewLine);
            }

            WriteToFile("AllGroupUserKeyTestResult.csv", builder.ToString());
        }

        private void CreateGroupUserKeySQLUpdate()
        {
            var sqlBuilder = new StringBuilder();

            var userClinicNumbers = GetCSVContent("FinalGroupUserKey.csv").Skip(1).Select(x => new { UserId = x[0], GroupNumber = x[1].Trim(), GroupUserKey = x[2].Trim() }).ToList();

            foreach (var record in userClinicNumbers)
            {
                var sql = $"UPDATE UserProfiles SET GroupNumber = '{record.GroupNumber}', GroupUserKey = '{record.GroupUserKey}' WHERE USERID = '{record.UserId}';";
                sqlBuilder.Append(sql).Append(System.Environment.NewLine);
            }

            WriteToFile("SQL UPDATES Group User Key.sql", sqlBuilder.ToString());
        }

        private void CreateClinicNumberSQLUpdate()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userProfiles = _context.UserProfiles.ToList();

            var sqlBuilder = new StringBuilder();

            var userClinicNumbers = GetCSVContent("From Wanda - Clinic Number Changes 2024-01-19.csv").Skip(1).Select(x => new { UserId = x[0], ClinicNumber = x[1].Trim() }).ToList();

            foreach (var record in userClinicNumbers)
            {
                var sql = $"UPDATE UserProfiles SET ClinicNumber = '{record.ClinicNumber}' WHERE USERID = '{record.UserId}';";
                sqlBuilder.Append(sql).Append(System.Environment.NewLine);
            }

            WriteToFile("SQL UPDATES Clinic Number.txt", sqlBuilder.ToString());
        }

        private void CompareRefDocFiles()
        {
            var scheduleList = File.ReadAllLines("./Data/REFDOC.TXT");
            var stringBuilder = new StringBuilder();
            string myNumber;
            string myLastName;
            string myFirstName;
            string myCity;

            var fromEmailRefDocList = new Dictionary<string, string>();
            foreach (string myRow in scheduleList)
            {
                if (myRow.Length > 10)
                {
                    int myIndex = myRow.IndexOf(',');
                    myNumber = myRow.Substring(0, 4);
                    if (myIndex > 0)
                    {
                        myLastName = myRow.Substring(4, (myIndex - 4));

                        if ((myIndex + 2) < 23)
                            myFirstName = myRow.Substring((myIndex + 2), (23 - (myIndex + 2)));
                        else
                            myFirstName = string.Empty;
                        myCity = myRow.Substring(23);

                    }
                    else
                    {
                        myLastName = myRow.Substring(4);
                        myFirstName = string.Empty;
                        myCity = string.Empty;
                    }

                    var myDisplay = string.Format("{0}, {1} - {2}", myLastName.Trim(), myFirstName.Trim(), myCity.Trim());
                    fromEmailRefDocList.Add(myNumber, myDisplay);
                }
            }

            scheduleList = File.ReadAllLines("./Data/ICS_DOWNLOAD_RefDoc-20230928.txt");
            var fromICSRefDocList = new Dictionary<string, string>();
            foreach (string myRow in scheduleList)
            {
                if (myRow.Length > 10)
                {
                    int myIndex = myRow.IndexOf(',');
                    myNumber = myRow.Substring(0, 4);
                    if (myIndex > 0)
                    {
                        myLastName = myRow.Substring(4, (myIndex - 4));

                        if ((myIndex + 2) < 23)
                            myFirstName = myRow.Substring((myIndex + 2), (23 - (myIndex + 2)));
                        else
                            myFirstName = string.Empty;
                        myCity = myRow.Substring(23);

                    }
                    else
                    {
                        myLastName = myRow.Substring(4);
                        myFirstName = string.Empty;
                        myCity = string.Empty;
                    }

                    var myDisplay = string.Format("{0}, {1} - {2}", myLastName.Trim(), myFirstName.Trim(), myCity.Trim());
                    fromICSRefDocList.Add(myNumber, myDisplay);
                }
            }

            foreach (var item in fromICSRefDocList)
            {
                if (!fromEmailRefDocList.ContainsKey(item.Key))
                {
                    Console.WriteLine(item.Key + " - " + item.Value);
                }
            }

        }

        private void AddIgnoreReturn()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
            myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
            myClaimsInReturn.ReturnFooter = string.Empty;
            myClaimsInReturn.ReturnFileType = 1;
            myClaimsInReturn.ReturnFileName = "I029_H56_20230706140901.txt";
            myClaimsInReturn.UploadDate = DateTime.UtcNow;

            var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Return Files\\" + myClaimsInReturn.ReturnFileName);

            myClaimsInReturn.Content = returnContent;
            myClaimsInReturn.TotalApproved = 0;
            myClaimsInReturn.TotalSubmitted = 0;
            myClaimsInReturn.TotalPaidAmount = 0;

            myClaimsInReturn.UploadDate = DateTime.UtcNow;
            myClaimsInReturn.UserId = new Guid("7B65ABA4-C0A1-4DEC-B404-54E81BA0140B");

            _context.ClaimsInReturn.Add(myClaimsInReturn);
            _context.SaveChanges();

        }

        private void CreatePDF()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var claimIn = _context.ClaimsIn.FirstOrDefault(x => x.ClaimsInId == new Guid("F1CD5A9B-024A-4520-A797-F0CE7D21E98D"));

            byte[] data = Convert.FromBase64String(claimIn.ValidationContent);

            File.WriteAllBytes("ValidationReport.pdf", data);
        }

        private void GetUATReturns()
        {
            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var groupNumbers = new[] { "H56", "261" };
            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetDailyReturnFileList("86662228", groupNumber);

                foreach (var file in returnFiles.FileNames)
                {
                    var getReturnFile = apiService.GetDailyReturnFile("86662228", file);
                    if (getReturnFile.IsSuccess)
                    {
                        WriteToFile("C:\\Personal\\MBS\\Files\\Return Files\\" + file, getReturnFile.FileContent);
                    }
                    else
                    {
                        Console.WriteLine("Error downloading Daily Return file: " + file);
                    }
                }
            }

            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetBiWeeklyReturnFileList("86662228", groupNumber);

                foreach (var file in returnFiles.FileNames)
                {
                    var getReturnFile = apiService.GetBiWeeklyReturnFile("86662228", file);
                    if (getReturnFile.IsSuccess)
                    {
                        WriteToFile("C:\\Personal\\MBS\\Files\\Return Files\\" + file, getReturnFile.FileContent);
                    }
                    else
                    {
                        Console.WriteLine("Error downloading BiWeekly Return file: " + file);
                    }
                }
            }
        }

        private void DownloadReturns()
        {
            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var groupUserKey = "26695970";
            var groupNumbers = new[] { "G81" };
            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetDailyReturnFileList(groupUserKey, groupNumber);

                foreach (var file in returnFiles.FileNames)
                {
                    var getReturnFile = apiService.GetDailyReturnFile(groupUserKey, file);
                    if (getReturnFile.IsSuccess)
                    {
                        WriteToFile("C:\\Personal\\MBS\\Files\\Return Files\\Chaya\\" + file, getReturnFile.FileContent);
                    }
                    else
                    {
                        Console.WriteLine("Error downloading Daily Return file: " + file);
                    }
                }
            }

            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetBiWeeklyReturnFileList(groupUserKey, groupNumber);

                foreach (var file in returnFiles.FileNames)
                {
                    var getReturnFile = apiService.GetBiWeeklyReturnFile(groupUserKey, file);
                    if (getReturnFile.IsSuccess)
                    {
                        WriteToFile("C:\\Personal\\MBS\\Files\\Return Files\\Chaya\\" + file, getReturnFile.FileContent);
                    }
                    else
                    {
                        Console.WriteLine("Error downloading BiWeekly Return file: " + file);
                    }
                }
            }
        }

        private void GetAndCheckReturns()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //55D343F1-7EE6-4BC5-B861-1FC6D26686B6 - Gorman
            //D8C7F68A-E7FE-4362-9161-C085CAFF86C9 - Annebelle
            var ignoreId1 = new Guid("2434726f-ac59-4727-97be-0d99a2958a71");
            var ignoreId2 = new Guid("C4901B48-F1CB-4450-A29E-2DC821075BDD");

            var userProfiles = _context.UserProfiles.Where(x => x.GroupNumber != null && x.GroupUserKey != null && x.UserId != ignoreId1 && x.UserId != ignoreId2)
            //var userProfiles = _context.UserProfiles.Where(x => x.UserId == new Guid("9339BDDE-F422-4C2F-9D3B-1EC765D3C91F"))
                .Select(x => new SimpleUserProfile()
                {
                    UserId = x.UserId,
                    DoctorName = x.DoctorName,
                    DoctorNumber = x.DoctorNumber,
                    DiagnosticCode = x.DiagnosticCode,
                    GroupNumber = x.GroupNumber,
                    GroupUserKey = x.GroupUserKey,
                    ClinicNumber = x.ClinicNumber
                }).ToList();

            var lockOutUsers = _context.Memberships.Where(x => x.IsLockedOut).Select(x => x.UserId).ToList();

            var msbServiceConfig = new ServiceConfig(ConfigHelper.GetMSBApiConnection());

            var apiService = new ClaimService(msbServiceConfig);

            var builder = new StringBuilder();
            foreach (var userProfile in userProfiles)
            {
                if (!lockOutUsers.Contains(userProfile.UserId))
                {
                    WriteInfo("Working on:" + userProfile.DoctorNumber + " - " + userProfile.DoctorName + ": " + userProfile.UserId);

                    var userClaimInReturns = _context.ClaimsInReturn.Where(x => x.UserId == userProfile.UserId && x.ReturnFileName != null).Select(x => x.ReturnFileName).Distinct().ToList();
                    
                    var dailyReturnFiles = apiService.GetDailyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);

                    foreach (var file in dailyReturnFiles.FileNames)
                    {
                        if (!userClaimInReturns.Contains(file))
                        {
                            WriteInfo("DAILY RETURN FILE NOT FOUND: " + file);
                        }
                    }

                    var biWeeklyReturnFiles = apiService.GetBiWeeklyReturnFileList(userProfile.GroupUserKey, userProfile.GroupNumber);

                    foreach (var file in biWeeklyReturnFiles.FileNames)
                    {
                        if (!userClaimInReturns.Contains(file))
                        {
                            WriteInfo("BIWEEKLY RETURN FILE NOT FOUND: " + file);
                        }
                    }

                    WriteInfo("----------------------------------------------------------------");
                    WriteInfo("");
                }
            }

            WriteToFile("Aug15Run.txt", _logBuilder.ToString());

            _context.Dispose();
        }

        private void GetProcessUserIds()
        {
            char myBreak = '\n';
            var lineContainUserIds = System.IO.File.ReadAllText("log.txt").Split(myBreak).Where(x => x.Trim().StartsWith("Getting returns for:")).ToList();

            var userIds = lineContainUserIds.Select(x => x.Trim().Substring(x.Length - 37)).Select(x => "new Guid(\"" + x + "\"),");

            foreach (var id in userIds)
            {
                Console.WriteLine(id);
            }
        }

        private void GetReturnsAndCreateIgnoreRecords()
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

            var groupNumbers = new Dictionary<string, Guid>();
            groupNumbers.Add("H56", new Guid("7B65ABA4-C0A1-4DEC-B404-54E81BA0140B"));
            groupNumbers.Add("261", new Guid("C288884C-46E9-4137-9468-7B115896FE03"));

            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetDailyReturnFileList("68229800", groupNumber.Key);

                foreach (var file in returnFiles.FileNames)
                {
                    ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                    myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                    myClaimsInReturn.ReturnFooter = string.Empty;
                    myClaimsInReturn.ReturnFileType = 0;
                    myClaimsInReturn.ReturnFileName = file;
                    myClaimsInReturn.ReturnFileDate = GetFileDateTime(file);
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;

                    var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Return Files\\" + file);

                    myClaimsInReturn.Content = returnContent;
                    myClaimsInReturn.TotalApproved = 0;
                    myClaimsInReturn.TotalSubmitted = 0;
                    myClaimsInReturn.TotalPaidAmount = 0;

                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.UserId = groupNumber.Value;

                    _context.ClaimsInReturn.Add(myClaimsInReturn);
                    _context.SaveChanges();
                }
            }

            foreach (var groupNumber in groupNumbers)
            {
                var returnFiles = apiService.GetBiWeeklyReturnFileList("68229800", groupNumber.Key);

                foreach (var file in returnFiles.FileNames)
                {
                    ClaimsInReturn myClaimsInReturn = new ClaimsInReturn();
                    myClaimsInReturn.ClaimsInReturnId = Guid.NewGuid();
                    myClaimsInReturn.ReturnFooter = string.Empty;
                    myClaimsInReturn.ReturnFileType = 1;
                    myClaimsInReturn.ReturnFileName = file;
                    myClaimsInReturn.ReturnFileDate = GetFileDateTime(file);
                    myClaimsInReturn.UploadDate = DateTime.UtcNow;

                    var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Return Files\\" + file);

                    myClaimsInReturn.Content = returnContent;
                    myClaimsInReturn.TotalApproved = 0;
                    myClaimsInReturn.TotalSubmitted = 0;
                    myClaimsInReturn.TotalPaidAmount = 0;

                    myClaimsInReturn.UploadDate = DateTime.UtcNow;
                    myClaimsInReturn.UserId = groupNumber.Value;

                    _context.ClaimsInReturn.Add(myClaimsInReturn);
                    _context.SaveChanges();
                }
            }
        }

        private void UpdateReturnFileDate()
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var returnClaims = _context.ClaimsInReturn.Where(x => x.ReturnFileName != null).ToList();

            foreach (var returnClaim in returnClaims)
            {
                returnClaim.ReturnFileDate = GetFileDateTime(returnClaim.ReturnFileName);
                _context.Entry(returnClaim).State = System.Data.Entity.EntityState.Modified;
            }

            _context.SaveChanges();
        }

        private string GetReturnHospitalNumber(string province, string hospitalNumber)
        {
            if (province.Equals("BC"))
            {
                return "B" + hospitalNumber.Substring(1, 7) + hospitalNumber.LastOrDefault();
            }
            else if (province.Equals("AB"))
            {
                return "A" + hospitalNumber.Substring(4) + hospitalNumber.Substring(hospitalNumber.Length - 4, 4);
            }
            else if (province.Equals("MB"))
            {
                return "M" + hospitalNumber.Substring(0, 8);
            }
            else if (province.Equals("ON"))
            {
                return "O" + hospitalNumber.Substring(0, 8);
            }
            else if (province.Equals("NB"))
            {
                return "W" + hospitalNumber.Substring(0, 8);
            }
            else if (province.Equals("NS"))
            {
                return "N" + hospitalNumber.Substring(1, 8);
            }
            else if (province.Equals("PE"))
            {
                return "P" + hospitalNumber.Substring(0, 8);
            }
            else if (province.Equals("NL"))
            {
                return "L" + hospitalNumber.Substring(3, 7) + hospitalNumber.LastOrDefault();
            }
            else if (province.Equals("YT"))
            {
                return "Y" + hospitalNumber.Substring(1, 8);
            }
            else if (province.Equals("NT"))
            {
                return "T" + hospitalNumber.Substring(1, 7) + "0";
            }
            else if (province.Equals("NU"))
            {
                return "R" + hospitalNumber.Substring(1, 8);
            }

            return hospitalNumber;
        }

        #region Export Code Code

        private void GetFeeCodeDictionary()
        {
            var stringBuilder = new StringBuilder();

            var contentList = GetPipeContent("./I031_ServCat_20250401000000.txt").Select(x => new NewFeeCode()
            {
                ServiceCode = x[0].Trim().TrimStart('0'),
                LowFee = x[1].Trim(),
                HighFee = x[2].Trim(),
                ServiceClassification = x[3].Trim(),
                AddOnIndicator = x[4].Trim(),
                MultiUnitIndicator = x[5].Trim(),
                FeeDeterminant = x[6].Trim(),
                AnaesthesiaIndicator = x[7].Trim(),
                SubmitAt100Perpect = x[8].Trim(),
                RequiredReferringDoc = x[9].Trim(),
                RequiredStartTime = x[10].Trim(),
                RequiredEndTime = x[11].Trim(),
                TechnicalFee = x[12].Trim(),
                Description = x[13].Trim(),
            }).OrderBy(x => x.ServiceCode).ToList();

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList)
            {
                if ((string.IsNullOrEmpty(myRow.LowFee) || myRow.LowFee == "0.00") && !string.IsNullOrEmpty(myRow.HighFee))
                {
                    myRow.LowFee = myRow.HighFee;
                    myRow.HighFee = string.Empty;
                }

                stringBuilder.Append(
                    string.Format("{{ \"{0}\", new FeeCodeModel() {{ FeeAmount = {1}f, HighFee = {2}f, ServiceClassification = \"{3}\", AddOnIndicator = \"{4}\", MultiUnitIndicator = \"{5}\", FeeDeterminant = \"{6}\", AnaesthesiaIndicator = \"{7}\", SubmitAt100Perpect = \"{8}\", RequiredReferringDoc = \"{9}\", RequiredStartTime = \"{10}\", RequiredEndTime = \"{11}\", TechnicalFee = {12}f, Description = \"{13}\"  }} }},",
                        myRow.ServiceCode,
                        !string.IsNullOrEmpty(myRow.LowFee) ? Convert.ToSingle(myRow.LowFee) : 0,
                        !string.IsNullOrEmpty(myRow.HighFee) ? Convert.ToSingle(myRow.HighFee) : 0,
                        myRow.ServiceClassification,
                        myRow.AddOnIndicator,
                        myRow.MultiUnitIndicator,
                        myRow.FeeDeterminant,
                        myRow.AnaesthesiaIndicator,
                        myRow.SubmitAt100Perpect,
                        myRow.RequiredReferringDoc,
                        myRow.RequiredStartTime,
                        myRow.RequiredEndTime,
                        !string.IsNullOrEmpty(myRow.TechnicalFee) ? Convert.ToSingle(myRow.TechnicalFee) : 0,
                        myRow.Description.Replace("\"", "'")
                    ));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        private void GetWCBCodeDictionary()
        {
            Dictionary<string, float> contentList = new Dictionary<string, float>()
            {
                #region Code

                { "650 - WCB", 61.44f },
                { "651 - WCB", 12.95f },
                { "660 - WCB", 38.17f },
                { "661 - WCB", 12.95f },
                { "119 - WCB", 96.2f },
                { "97 - WCB", 245.44f },
                { "178 - WCB", 49.15f },
                { "177 - WCB", 44.26f },
                { "126 - WCB", 49.15f },
                { "1126 - WCB", 44.26f },
                { "128 - WCB", 65.46f },
                { "1128 - WCB", 58.72f },
                { "164 - WCB", 65.46f },
                { "1164 - WCB", 58.72f },
                { "179 - WCB", 34.26f },
                { "640 - WCB", 43f },
                { "199 - WCB", 119f },
                { "89 - WCB", 942f },
                { "1189 - WCB", 377f },
                { "189 - WCB", 753f },
                { "1089 - WCB", 301f },
                { "42 - WCB", 377f },
                { "142 - WCB", 301f },
                { "5 - WCB", 1130f },
                { "1150 - WCB", 377f },
                { "150 - WCB", 903f },
                { "1050 - WCB", 301f },
                { "15 - WCB", 1506f },
                { "1115 - WCB", 377f },
                { "1015 - WCB", 1205f },
                { "1215 - WCB", 301f },
                { "190 - WCB", 753f },
                { "1190 - WCB", 301f },
                { "85 - WCB", 75.3f }

                #endregion
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList)
            {
                stringBuilder.Append(
                    string.Format("{{ \"{0}\", new FeeCodeModel() {{ FeeAmount = {1}f, HighFee = {2}f, ServiceClassification = \"{3}\", AddOnIndicator = \"{4}\", MultiUnitIndicator = \"{5}\", FeeDeterminant = \"{6}\", AnaesthesiaIndicator = \"{7}\", SubmitAt100Perpect = \"{8}\", RequiredReferringDoc = \"{9}\", RequiredStartTime = \"{10}\", RequiredEndTime = \"{11}\", TechnicalFee = {12}f, Description = \"{13}\"  }} }},",
                        myRow.Key,
                        myRow.Value,
                        myRow.Value,
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        0f,
                        ""
                    ));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        private void GetICDCodeDictionary()
        {
            var stringBuilder = new StringBuilder();

            var contentList = GetPipeContent("./I031_MSBDiag_20250401000000.txt").Select(x => new { DiagCode = x[0].Trim(), Description = x[1].Trim() })
                            .GroupBy(x => x.DiagCode).Select(x => new { DiagCode = x.Key, Description = string.Join(", ", x.Select(y => y.Description)) }).ToList();

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList.Where(x => x.DiagCode.Length == 3))
            {
                stringBuilder.Append(string.Format("{{ \"{0}\", \"{1}\" }},", myRow.DiagCode.TrimStart('0'), myRow.Description.Replace("\"", "'")));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        private void GetExplainCodeDictionary()
        {
            var stringBuilder = new StringBuilder();

            var splitEachLines = File.ReadAllLines("./I031_ExplanCode_20250401000000.txt").Select(x => x.Split('|')).ToList();

            if (splitEachLines.Any(x => x.Count() > 2))
            {
                Console.WriteLine("MORE THAN ONE PIPE IN THE LINES!!!!");
            }
                              
            var contentList = splitEachLines.Select(x => new { ExplainCode = x[0].Trim(), Description = x[1].Trim() }).ToList();

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList)
            {
                stringBuilder.Append(string.Format("{{ \"{0}\", \"{1}\" }},", myRow.ExplainCode, myRow.Description.Replace("\"", "'")));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        private void GetLocationOfServiceCodeDictionary()
        {
            var stringBuilder = new StringBuilder();

            var contentList = "1,2,3,4,5,9,K,M,P,T,B,C,D,E,F".Split(',');

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList)
            {
                stringBuilder.Append(string.Format("{{ \"{0}\", \"{1}\" }},", myRow, myRow));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        private void GetRefDocDictionary()
        {
            var stringBuilder = new StringBuilder();

            var contentList = GetPipeContent("./I031_RefPrac_20250306122902.txt").Select(x => new { DocNumber = x[0].Trim().TrimStart('0'), DocName = x[1].Trim(), City = x[2].Trim(), Speciality = x[3].Trim() }).ToList();

            var ss = string.Join(System.Environment.NewLine, contentList.Select(x => x.Speciality).GroupBy(x => x).Select(x => x.Key));

            stringBuilder.Append("#region Code").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

            foreach (var myRow in contentList)
            {
                stringBuilder.Append(string.Format("{{ \"{0}\", \"{1}\" }},", myRow.DocNumber, myRow.DocName + " - " + myRow.City + " - " + myRow.Speciality));
                stringBuilder.Append(System.Environment.NewLine);
            }

            stringBuilder.Append(System.Environment.NewLine).Append("#endregion");

            var list = stringBuilder.ToString();
        }

        #endregion

        private void GetUserProfile(Guid userId)
        {
            try
            {
                _context = new MedicalBillingSystemEntities();
            }
            catch (Exception ex)
            {
            }

            var hello = _context.UserProfiles.FirstOrDefault(x => x.UserId == userId);
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

        private void DeleteServiceRecord(ServiceRecord serviceRecord)
        {
            _context.Entry(serviceRecord).State = System.Data.Entity.EntityState.Deleted;
        }

        private void DeleteUnitRecords(IEnumerable<UnitRecord> unitRecords)
        {
            foreach (var item in unitRecords)
            {
                _context.Entry(item).State = System.Data.Entity.EntityState.Deleted;
            }
        }

        private void SetServiceRecordStateToModified(ServiceRecord serviceRecord)
        {
            if (_context.Entry(serviceRecord).State == System.Data.Entity.EntityState.Unchanged)
            {
                _context.Entry(serviceRecord).State = System.Data.Entity.EntityState.Modified;
            }
        }

        private void SetServiceRecordStateToAdded(ServiceRecord serviceRecord)
        {
            _context.Entry(serviceRecord).State = System.Data.Entity.EntityState.Added;
        }

        private void SetServiceRecordStateToDeleted(ServiceRecord serviceRecord)
        {
            _context.Entry(serviceRecord).State = System.Data.Entity.EntityState.Deleted;
        }
        private void SetClaimsReturnPaymentSummaryStateToAdded(ClaimsReturnPaymentSummary record)
        {
            _context.Entry(record).State = System.Data.Entity.EntityState.Added;
        }

        private void SetUnitRecordStateToAdded(UnitRecord unitRecord)
        {
            _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Added;
        }

        private void SetUnitRecordStateToModified(UnitRecord unitRecord)
        {
            if (_context.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
            {
                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
            }
        }

        private void SetUnitRecordListStateToModified(IEnumerable<UnitRecord> unitRecords)
        {
            foreach (var unitRecord in unitRecords)
            {
                if (_context.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged)
                {
                    _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Modified;
                }
            }
        }

        private void SetUnitRecordStateToDeleted(UnitRecord unitRecord)
        {
            if (_context.Entry(unitRecord).State == System.Data.Entity.EntityState.Modified ||
                _context.Entry(unitRecord).State == System.Data.Entity.EntityState.Unchanged ||
                _context.Entry(unitRecord).State == System.Data.Entity.EntityState.Deleted)
            {
                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Deleted;
            }
            else
            {
                _context.Entry(unitRecord).State = System.Data.Entity.EntityState.Detached;
            }
        }

        private string AdjustTextValue(string myValue, int myLength)
        {
            if (myValue.Length > myLength)
            {
                myValue = myValue.Remove(myLength);
            }
            else if (myValue.Length < myLength)
            {
                myValue = myValue.PadRight(myLength, ' ');
            }
            return myValue;
        }

        private string AdjustNumberValue(string myValue, int myLength)
        {
            return myValue.PadLeft(myLength, '0').ToUpper();
        }

        private string ValueFiller(char myFiller, int myLength)
        {
            return String.Empty.PadLeft(myLength, myFiller);
        }

        private string RoundUpValue(double myValue)
        {
            return string.Format("{0:F2}", Math.Round(Convert.ToDecimal(myValue), 2));
        }

        private void WriteToFile(string fileName, string content)
        {
            var file = new StreamWriter(fileName, false);
            file.Write(content);
            file.Close();
        }

        private List<string[]> GetCSVContent(string fileName)
        {
            var result = new List<string[]>();
            using (TextFieldParser csvReader = new TextFieldParser(fileName))
            {
                csvReader.SetDelimiters(new string[] { "," });

                csvReader.HasFieldsEnclosedInQuotes = true;

                while (!csvReader.EndOfData)
                {
                    result.Add(csvReader.ReadFields().Select(x => x).ToArray());
                }
            }

            return result;
        }

        private List<string[]> GetPipeContent(string fileName)
        {
            var result = new List<string[]>();
            using (TextFieldParser csvReader = new TextFieldParser(fileName))
            {
                csvReader.SetDelimiters(new string[] { "|" });

                csvReader.HasFieldsEnclosedInQuotes = true;

                while (!csvReader.EndOfData)
                {
                    try
                    {
                        result.Add(csvReader.ReadFields().Select(x => x).ToArray());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return result;
        }

        private string CSVSafe(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = value.Replace("\"", "\"\"");
                return "\"" + value + "\"";
            }

            return value;
        }

        public static DateTime GetFileDateTime(string fileName)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var splitString = fileName.Replace(".txt", string.Empty).Split('_');

            return DateTime.ParseExact(splitString.Last(), "yyyyMMddHHmmss", provider);
        }

        private void WriteInfo(string message)
        {
            _logBuilder.Append(message).Append(System.Environment.NewLine);
            Console.WriteLine(message);
        }

        private void WriteLogToFile(string fileName)
        {
            if (_logBuilder.Length > 0)
            {
                try
                {
                    WriteToFile(fileName, _logBuilder.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        private bool IsStartWith(string source, string target)
        {
            var source1 = Regex.Replace(source, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            var target1 = Regex.Replace(target, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            return source1.StartsWith(target1, StringComparison.OrdinalIgnoreCase);
        }

        private ReturnContent ParseReturn(string myReturnContent, ReturnFileType returnFileType, UserProfiles userProfile, string previousReturnContent)
        {
            _timeZoneOffset = ConfigHelper.GetTimeZoneOffset();

            ReturnContent returnContent = null;

            var myDiagCode = userProfile.DiagnosticCode;

            if (!string.IsNullOrEmpty(myReturnContent))
            {
                string[] myLines = myReturnContent.Split(myBreak);
                if (myLines.Count() > 0)
                {
                    var myDoctorNumber = userProfile.DoctorNumber.PadLeft(4, '0');
                    var myClinicNumber = userProfile.ClinicNumber.PadLeft(3, '0');

                    var myContents = RemoveUnRelatedLines(myLines, myDoctorNumber, myClinicNumber);
                    if (myContents.Count() > 0)
                    {
                        var myUserId = userProfile.UserId;

                        var myUnprocessLines = GetUnProcessLines(myContents, previousReturnContent, myDoctorNumber, myClinicNumber);
                        if (myUnprocessLines.Count() > 0)
                        {
                            var totalLines = ParseTotalLines(myUnprocessLines.ToArray(), myUserId);

                            returnContent = ParseReturnLineItems(myUnprocessLines.ToArray());
                            
                            //Deal with Payment Draw Back Adjustment claims
                            //To get the explain code from Rejected lines and put them in the Paid Claims and removed any return claims based on the CPS Claims, no need to process them
                            
                            foreach (var paidItem in returnContent.PaidItems)
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
                    }
                }
            }

            return returnContent;
        }

        private IEnumerable<string> RemoveUnRelatedLines(string[] myLines, string myDoctorNumber, string myClinicNumber)
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
                        if (!line.StartsWith("10" + myDoctorNumber) && !line.StartsWith("90" + myDoctorNumber) && line.IndexOf(messageLinePattern1) != 1 && line.IndexOf(messageLinePattern2) != 1)
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

        private IEnumerable<ClaimsReturnPaymentSummary> ParseTotalLines(string[] myLines, Guid myUserId)
        {
            var result = new List<ClaimsReturnPaymentSummary>();
            var myLine = string.Empty;
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

                        result.Add(totalLine);
                    }
                    else if (myLine.IndexOf("99999M ") == 8 || myLine.IndexOf("     M ") == 8)
                    {
                        //Ignore the lines
                    }                    
                }
            }

            return result;
        }

        private ReturnContent ParseReturnLineItems(string[] myLines)
        {
            var lineItems = new List<ReturnLineItem>();
            var myLine = string.Empty;

            for (var i = 0; i < myLines.Count(); i++)
            {
                myLine = myLines[i];

                if (!string.IsNullOrEmpty(myLine))
                {
                    if (myLine.IndexOf("99999T ") == 8 || myLine.IndexOf("     T ") == 8)
                    {
                        //Ignore the lines
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

                foreach (var group in groupByClaimNumberAndSeqToCheckAdjustment)
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

                foreach (var claimsGroup in pendingClaimsGroupByCPSClaimNumber)
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

                foreach (var claimsGroup in paidAndRejectedClaimsGroupByCPSClaimNumber)
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

            return returnContent;
        }

        private IEnumerable<string> GetUnProcessLines(IEnumerable<string> currentReturnLines, string previousReturnContent, string myDoctorNumber, string myClinicNumber)
        {
            if (previousReturnContent.Any())
            {
                var previousLines = previousReturnContent.Split(myBreak);
                if (previousLines.Count() > 0)
                {
                    var myPreviousLines = RemoveUnRelatedLines(previousLines, myDoctorNumber, myClinicNumber);
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
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount(myUnitRecord.UnitCode, myUnitRecord.UnitAmount, myUnitRecord.UnitPremiumCode, string.Empty);
                }
                else if (myLine.ReturnedRecordType == RETURNED_RECORD_TYPE.HOSPITAL_CARE)
                {
                    //Returned record
                    myUnitRecord.UnitPremiumCode = "2";
                    myUnitRecord.PaidAmount = GetTotalWithPremiumAmount(myUnitRecord.UnitCode, myUnitRecord.UnitAmount, "2", string.Empty);
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

        private IList<ServiceRecord> GetMatchServiceRecords(Guid userId, int claimNumber)
        {
            return _context.ServiceRecord.Include("UnitRecord").Where(x => x.ClaimNumber == claimNumber && x.UserId == userId).OrderByDescending(x => x.CreatedDate).ToList();
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

        private void RemoveDuplicateUnitRecordsWithRecordIndex(ServiceRecord serviceRecord)
        {
            var groupByUnitCodes = serviceRecord.UnitRecord.GroupBy(x => new { x.UnitCode, x.UnitPremiumCode, x.UnitNumber, x.SubmittedRecordIndex })
                .Select(x => new { UnitCode = x.Key, RecordCount = x.Count(), RecordList = x.ToList() }).ToList();

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
            foreach (var unitRecord in serviceRecord.UnitRecord.Where(x => _context.Entry(x).State != System.Data.Entity.EntityState.Deleted).OrderBy(x => x.SubmittedRecordIndex))
            {
                unitRecord.RecordIndex = index;
               
                if (serviceRecord.PaidClaimId.HasValue && !unitRecord.SubmittedAmount.HasValue)
                {
                    unitRecord.SubmittedAmount = unitRecord.UnitAmount;
                }

                SetUnitRecordStateToModified(unitRecord);

                index++;
            }
        }

        #region Remove Dupliate Records

        public void RemoveDuplicateUnitRecordsInServiceRecord(ServiceRecord serviceRecord)
        {
            var groupByUnitCodes = serviceRecord.UnitRecord.GroupBy(x => new { x.UnitCode, x.UnitNumber, x.SubmittedRecordIndex }).Select(x => new { UnitCode = x.Key, RecordCount = x.Count(), RecordList = x.ToList() }).ToList();

            foreach (var group in groupByUnitCodes)
            {
                if (group.RecordCount > 1)
                {
                    IEnumerable<UnitRecord> unitRecordsToCheck;

                    if (serviceRecord.PaidClaimId.HasValue)
                    {
                        unitRecordsToCheck = group.RecordList.Where(x => x.SubmittedAmount.HasValue).OrderByDescending(x => x.RunCode).ToList();
                    }
                    else
                    {
                        unitRecordsToCheck = group.RecordList.OrderByDescending(x => x.RunCode).ToList();
                    }

                    var recordToKeep = unitRecordsToCheck.FirstOrDefault(x => !string.IsNullOrEmpty(x.ExplainCode) && !string.IsNullOrEmpty(x.ExplainCode2) &&
                                               !string.IsNullOrEmpty(x.ExplainCode3));

                    if (recordToKeep == null)
                    {
                        recordToKeep = unitRecordsToCheck.FirstOrDefault(x => !string.IsNullOrEmpty(x.ExplainCode) && !string.IsNullOrEmpty(x.ExplainCode2) &&
                                            !string.IsNullOrEmpty(x.ExplainCode3));
                        if (recordToKeep == null)
                        {
                            recordToKeep = unitRecordsToCheck.OrderByDescending(x => x.ExplainCode).FirstOrDefault();
                        }
                    }

                    foreach (var recordToRemove in group.RecordList.Where(x => x.UnitRecordId != recordToKeep.UnitRecordId))
                    {
                        SetUnitRecordStateToDeleted(recordToRemove);
                        serviceRecord.UnitRecord.Remove(recordToRemove);
                    }
                }
            }

            var index = 1;
            foreach (var unitRecord in serviceRecord.UnitRecord.Where(x => _context.Entry(x).State != System.Data.Entity.EntityState.Deleted).OrderBy(x => x.SubmittedRecordIndex))
            {
                unitRecord.RecordIndex = index;
                SetUnitRecordStateToModified(unitRecord);

                index++;
            }

            if (serviceRecord.PaidClaimId.HasValue)
            {
                serviceRecord.PaidAmount = GetUnitRecordPaidAmountSum(serviceRecord.UnitRecord);
                serviceRecord.ClaimAmount = GetUnitRecordSubmittedAmountSum(serviceRecord.UnitRecord);
            }
            else
            {
                serviceRecord.ClaimAmount = GetUnitRecordPaidAmountSum(serviceRecord.UnitRecord);
            }

            SetServiceRecordStateToModified(serviceRecord);
        }

        #endregion

        #region Deal with Duplicate Claims

        public void MergePaidClaims(Guid myUserId)
        {
            /*
             *  CF2DDA5B-D3BE-4677-A672-1E1E3135551D	29120	9064629641	CORNELIUS,Cornelis	1028692721	2
                CF2DDA5B-D3BE-4677-A672-1E1E3135551D	29129	9064629641	Cornelis,CORNELIUS	1028692727	2
                CF2DDA5B-D3BE-4677-A672-1E1E3135551D	29119	9064629641	Cornelis,CORNELIUS	1028692705	2
             */
            var dateToUsed = new DateTime(2024, 1, 1, 6, 0, 0);
            var paidClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord")
                    .Where(x => x.UserId == myUserId && x.CreatedDate >= dateToUsed && x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue)
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber, x.ServiceDate })
                    .Select(x => new
                    {
                        ClaimNumber = x.Key.ClaimNumber,
                        HSN = x.Key.HospitalNumber,
                        ServiceDate = x.Key.ServiceDate,
                        PaidClaimCount = x.Count(),
                        PaidClaimList = x.ToList()
                    }).Where(x => x.PaidClaimCount > 1).ToList();

            foreach (var group in paidClaimsNeedToMerge)
            {
                var recordToKeep = group.PaidClaimList.OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth).FirstOrDefault();

                var unitRecords = group.PaidClaimList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.PaidClaimList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    DeleteServiceRecord(recordToRemove);
                }

                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                recordToKeep.PaidAmount = recordToKeep.ClaimAmount;
                
                SetServiceRecordStateToModified(recordToKeep);
            }
        }

        public void MergeRejectedClaims(Guid myUserId)
        {
            var dateToUsed = new DateTime(2024, 1, 1, 6, 0, 0);
            var rejectedClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord")
                    .Where(x => x.UserId == myUserId && x.CreatedDate >= dateToUsed && !x.PaidClaimId.HasValue && x.RejectedClaimId.HasValue)
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber })
                    .Select(x => new
                    {
                        ClaimNumber = x.Key.ClaimNumber,
                        HSN = x.Key.HospitalNumber,
                        RejectedClaimCount = x.Count(),
                        RejectedClaimList = x.ToList()
                    }).Where(x => x.RejectedClaimCount > 1).ToList();

            foreach (var group in rejectedClaimsNeedToMerge)
            {
                var recordToKeep = group.RejectedClaimList.OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth).FirstOrDefault();

                var unitRecords = group.RejectedClaimList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.RejectedClaimList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    DeleteServiceRecord(recordToRemove);
                }

                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                SetServiceRecordStateToModified(recordToKeep);
            }
        }

        public void MergePendingClaims(Guid myUserId)
        {
            var dateToUsed = new DateTime(2024, 1, 1, 6, 0, 0);
            var pendingClaimsNeedToMerge = _context.ServiceRecord.Include("UnitRecord")
                    .Where(x => x.UserId == myUserId && x.CreatedDate >= dateToUsed && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber != null && x.CPSClaimNumber != string.Empty)
                    .GroupBy(x => new { x.ClaimNumber, x.HospitalNumber })
                    .Select(x => new
                    {
                        ClaimNumber = x.Key.ClaimNumber,
                        HSN = x.Key.HospitalNumber,
                        ClaimCount = x.Count(),
                        ClaimList = x.ToList()
                    }).Where(x => x.ClaimCount > 1).ToList();

            foreach (var group in pendingClaimsNeedToMerge)
            {
                ServiceRecord recordToKeep = group.ClaimList.Where(x => x.ClaimsInId.HasValue).OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth).FirstOrDefault();
                if (recordToKeep == null)
                {
                    recordToKeep = group.ClaimList.OrderBy(x => x.CPSClaimNumber).ThenBy(x => x.DateOfBirth).FirstOrDefault();
                }

                var unitRecords = group.ClaimList.SelectMany(x => x.UnitRecord);

                RemoveDuplicateUnitRecordsAndRemap(unitRecords, recordToKeep.ServiceRecordId);

                foreach (var recordToRemove in group.ClaimList.Where(x => x.ServiceRecordId != recordToKeep.ServiceRecordId))
                {
                    DeleteServiceRecord(recordToRemove);
                }

                recordToKeep.ClaimAmount = GetUnitRecordPaidAmountSum(recordToKeep.UnitRecord);
                SetServiceRecordStateToModified(recordToKeep);
            }
        }

        public void MergePendingClaimInPaidOrRejected(Guid myUserId)
        {
            var dateToUsed = new DateTime(2024, 2, 1, 6, 0, 0);
            var pendingClaims = _context.ServiceRecord.Include("UnitRecord")
                    .Where(x => x.UserId == myUserId && x.CreatedDate >= dateToUsed && !x.PaidClaimId.HasValue && !x.RejectedClaimId.HasValue && x.CPSClaimNumber != null);

            foreach (var pending in pendingClaims)
            {
                var myMatchedClaimNumberServiceRecords = GetMatchServiceRecords(myUserId, pending.ClaimNumber);
                if (myMatchedClaimNumberServiceRecords.Any())
                {
                    var unitRecordUsedIds = new List<Guid>();

                    #region Check Paid Claim

                    var paidClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                            !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue &&
                                            x.HospitalNumber.Equals(pending.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                            IsStartWith(x.PatientLastName, pending.PatientLastName))
                                            .FirstOrDefault();

                    if (paidClaim == null)
                    {
                        //Too restrictive, match only HSN
                        paidClaim = myMatchedClaimNumberServiceRecords.Where(x =>
                                            !x.RejectedClaimId.HasValue && x.PaidClaimId.HasValue &&
                                            x.HospitalNumber.Equals(pending.HospitalNumber, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    }

                    if (paidClaim != null) //Paid Claim
                    {
                        var foundUnitRecords = new List<UnitRecord>();
                        foreach (var returnUnitRecord in pending.UnitRecord)
                        {
                            var foundExistingUnitRecord = GetMatchedUnitRecord(paidClaim.UnitRecord, returnUnitRecord, foundUnitRecords);

                            if (foundExistingUnitRecord != null)
                            {
                                foundUnitRecords.Add(returnUnitRecord);
                                unitRecordUsedIds.Add(returnUnitRecord.UnitRecordId);
                            }
                        }

                        if (foundUnitRecords.Any() && pending.UnitRecord.Count == foundUnitRecords.Count()) //Update the unit records
                        {
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                SetUnitRecordStateToDeleted(unitRecord);
                            }

                            //All unit records in Pending found, then delete the pending claim
                            DeleteServiceRecord(pending);

                            Console.WriteLine("Found Paid Claim Contain All Pending Line Items - Delete Pending Claim #:" + pending.ClaimNumber);
                        }
                        else if (foundUnitRecords.Any())
                        {
                            //Only Partial Line Items in Pending found, delete them only
                            foreach (var unitRecord in foundUnitRecords)
                            {
                                SetUnitRecordStateToDeleted(unitRecord);
                            }

                            pending.ClaimAmount = GetUnitRecordPaidAmountSum(pending.UnitRecord);
                            SetServiceRecordStateToModified(pending);

                            Console.WriteLine("Found Paid Claim Contain Partial Pending Line Items - Delete Pending Claim #:" + pending.ClaimNumber);
                        }
                    }

                    #endregion

                    if (_context.Entry(pending).State != System.Data.Entity.EntityState.Deleted)
                    {

                        #region Check Rejected Claims
                        //Never get pending claim from. If the claim is paid, it will do a draw back, and then pending. No rejected record.

                        //Getting all the rejected claims
                        var rejectedServiceRecords = myMatchedClaimNumberServiceRecords.Where(x => x.RejectedClaimId.HasValue && !x.PaidClaimId.HasValue);

                        //Get the most filter ones - Match Last Name (start) And Hospital Number
                        var matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                x.HospitalNumber.Equals(pending.HospitalNumber, StringComparison.OrdinalIgnoreCase) &&
                                                IsStartWith(x.PatientLastName, pending.PatientLastName));

                        if (matchedRejectedServiceRecord == null)
                        {
                            //Too restricted, then try either Last Name or Hospital Number
                            matchedRejectedServiceRecord = rejectedServiceRecords.FirstOrDefault(x =>
                                                x.HospitalNumber.Equals(pending.HospitalNumber, StringComparison.OrdinalIgnoreCase));
                        }

                        if (matchedRejectedServiceRecord != null)
                        {
                            #region Found Rejected Claims

                            var foundUnitRecords = new List<UnitRecord>();
                            foreach (var returnUnitRecord in pending.UnitRecord)
                            {
                                var foundExistingUnitRecord = GetMatchedUnitRecord(matchedRejectedServiceRecord.UnitRecord, returnUnitRecord, foundUnitRecords);
                                if (foundExistingUnitRecord != null)
                                {
                                    foundUnitRecords.Add(returnUnitRecord);
                                }
                            }

                            if (pending.UnitRecord.Count() == foundUnitRecords.Count() && foundUnitRecords.Any())
                            {
                                #region All Line Items

                                foreach (var unitRecord in foundUnitRecords)
                                {
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }

                                //All unit records in Pending found, then delete the pending claim
                                DeleteServiceRecord(pending);

                                Console.WriteLine("Found Rejected Claim Contain All Pending Line Items - Delete Pending Claim #:" + pending.ClaimNumber);

                                #endregion
                            }
                            else if (foundUnitRecords.Any())
                            {
                                #region Partial Line Items

                                //Only Partial Line Items in Pending found, delete them only
                                foreach (var unitRecord in foundUnitRecords)
                                {
                                    SetUnitRecordStateToDeleted(unitRecord);
                                }

                                pending.ClaimAmount = GetUnitRecordPaidAmountSum(pending.UnitRecord);
                                SetServiceRecordStateToModified(pending);

                                Console.WriteLine("Found Rejected Claim Contain Partial Pending Line Items - Delete Pending Claim #:" + pending.ClaimNumber);

                                #endregion
                            }

                            #endregion
                        }

                        #endregion
                    }
                }
            }
        }

        private void RemoveDuplicateUnitRecordsAndRemap(IEnumerable<UnitRecord> unitRecords, Guid wantedServiceRecordId)
        {
            var groupByUnitCodes = unitRecords.GroupBy(x => new { x.UnitCode, x.UnitPremiumCode, x.UnitNumber })
                                    .Select(x => new { UnitCode = x.Key, RecordCount = x.Count(), RecordList = x.OrderByDescending(y => y.RunCode).ToList() }).ToList();

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
            foreach (var unitRecord in unitRecords.Where(x => _context.Entry(x).State != System.Data.Entity.EntityState.Deleted).OrderBy(x => x.SubmittedRecordIndex))
            {
                unitRecord.RecordIndex = index;
                unitRecord.ServiceRecordId = wantedServiceRecordId;
                SetUnitRecordStateToModified(unitRecord);

                index++;
            }
        }

        #endregion        
    }

    public class ProcessResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }
    }

    public class NewFeeCode
    {
        public string ServiceCode { get; set; }
        public string LowFee { get; set; }
        public string HighFee { get; set; }
        public string ServiceClassification { get; set; }
        public string AddOnIndicator { get; set; }
        public string MultiUnitIndicator { get; set; }
        public string FeeDeterminant { get; set; }
        public string AnaesthesiaIndicator { get; set; }
        public string SubmitAt100Perpect { get; set; }
        public string RequiredReferringDoc { get; set; }
        public string RequiredStartTime { get; set; }
        public string RequiredEndTime { get; set; }
        public string TechnicalFee { get; set; }
        public string Description { get; set; }

    }

    public enum SearchClaimType
    {
        All = -1,
        Unsubmitted = 0,
        Submitted = 1,
        Pending = 2,
        Paid = 3,
        Rejected = 4,
        Deleted = 5
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

        public string ReturnFileName { get; set;}

        public DateTime ReturnFileDate { get; set; }
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

    public class GroupItem
    {
        public int ClaimNumber { get; set; }

        public string HSN { get; set; }

        public DateTime ServiceDate { get; set; }

        public int RecordCount { get; set; }

        public IList<ServiceRecord> RecordList { get; set; }
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
