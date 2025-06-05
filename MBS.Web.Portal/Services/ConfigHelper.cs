using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MBS.Web.Portal.Services
{
    public static class ConfigHelper
    {
        public static int GetTimeZoneOffset()
        {
            var result = -6;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["TimeZoneOffset"]);
            }
            catch
            {
            }

            return result;
        }

        public static int GetPasswordTokenExpiryMinute()
        {
            var result = 10;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["PasswordTokenExpiryMinute"]);
            }
            catch
            {
            }

            return result;
        }
        
        public static string GetICSUrl()
        {
            var result = "https://efq-ics.ehealthsask.ca";

            try
            {
                result = ConfigurationManager.AppSettings["ICSSite"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetInterfaxUserName()
        {
            var result = string.Empty;

            try
            {
                result = ConfigurationManager.AppSettings["InterFaxUserName"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetInterfaxPassword()
        {
            var result = string.Empty;

            try
            {
                result = ConfigurationManager.AppSettings["InterFaxPassword"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetWCBFaxNumber()
        {
            var result = string.Empty;

            try
            {
                result = ConfigurationManager.AppSettings["WCBFaxNumber"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetSupportEmail()
        {
            var result = "poch_ben@hotmail.com";

            try
            {
                result = ConfigurationManager.AppSettings["SupportEmail"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetCertExpiryEmail()
        {
            var result = "ben@perspect.ca";

            try
            {
                result = ConfigurationManager.AppSettings["CertExpiryNotificationEmail"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetTriggerExpression()
        {
            var result = "0 15 3-6 ? * *";

            try
            {
                result = ConfigurationManager.AppSettings["TriggerExpression"];                
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetTriggerPendingClaimsExpression()
        {
            var result = "0 0/10 * 1/1 * ? *";

            try
            {
                result = ConfigurationManager.AppSettings["TriggerPendingClaimsExpression"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetMSBApiConnection()
        {
            var result = "";

            try
            {
                result = ConfigurationManager.ConnectionStrings["MSBApiConnection"].ConnectionString;
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetMSBLogsPath()
        {
            var result = "C:\\MSBLogs\\";

            try
            {
                result = ConfigurationManager.AppSettings["LogPath"];
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static int GetNumberOfDaysToGetForDailyReturnFiles()
        {
            var result = -14;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["NumberOfDaysToGetForDailyReturnFiles"]);
            }
            catch
            {
            }

            return result;
        }

        public static int GetNumberOfDaysToGetForBiWeeklyReturnFiles()
        {
            var result = -65;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["NumberOfDaysToGetForBiWeeklyReturnFiles"]);
            }
            catch
            {
            }

            return result;
        }

        public static int GetNumberOfDailyReturnFilesToGet()
        {
            var result = 14;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["NumberOfDailyReturnFilesToGet"]);
            }
            catch
            {
            }

            return result;
        }

        public static int GetNumberOfBiWeeklyReturnFilesToGet()
        {
            var result = 4;

            try
            {
                result = int.Parse(ConfigurationManager.AppSettings["NumberOfBiWeeklyReturnFilesToGet"]);
            }
            catch
            {
            }

            return result;
        }
    }
}