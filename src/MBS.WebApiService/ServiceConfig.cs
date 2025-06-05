using System;
using System.Linq;
using System.Security;

namespace MBS.WebApiService
{
   public class ServiceConfig
    {
        private static string connectionString;
        private string audience = "https://www.cps.saskhealth.com/vendorAPIs";
        private string url = null;
        private string authenticationUrl = null;
        private string clientId = null;

        private string submitClaimPath = null;
        private string dailyReturnFileListPath = null;
        private string dailyReturnDownloadPath = null;
        private string biweeklyRetunrFileListPath = null;
        private string biweeklyReturnDownloadPath = null;

        /// <summary>
        /// Constructor that parses a connection string
        /// </summary>
        /// <param name="connectionStringParam">The connection string to instantiate the configuration</param>
        public ServiceConfig(string connectionStringParam)
        {
            connectionString = connectionStringParam;            

            url = GetParameterValue("Url");
            clientId = GetParameterValue("ClientId");            
            ClientSecret = GetParameterValue("ClientSecret");          
            authenticationUrl = GetParameterValue("AuthenticationUrl");
            submitClaimPath = GetParameterValue("SubmitClaimPath");
            dailyReturnFileListPath = GetParameterValue("DailyReturnFileListPath");
            dailyReturnDownloadPath = GetParameterValue("DailyReturnDownloadPath");
            biweeklyRetunrFileListPath = GetParameterValue("BiWeeklyReturnFiListPath");
            biweeklyReturnDownloadPath = GetParameterValue("BiWeeklyReturnDownloadPath");

            byte maxRetries;
            if (byte.TryParse(GetParameterValue("MaxRetries"), out maxRetries))
            {
                MaxRetries = maxRetries;
            }
        }


        /// <summary>
        /// The Url to the MSB environment
        /// </summary>
        public string Url
        {
            get
            {
                return url;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    url = value;
                }
                else
                {
                    throw new Exception("Service.Url value cannot be null.");
                }
            }
        }

        /// <summary>
        /// The Url to the MSB environment
        /// </summary>
        public string AuthenticationUrl
        {
            get
            {
                return authenticationUrl;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    authenticationUrl = value;
                }
                else
                {
                    throw new Exception("Service.AuthenticationUrl value cannot be null.");
                }
            }
        }

        /// <summary>
        /// The id of the application registered with Azure AD
        /// </summary>
        public string ClientId
        {
            get
            {
                return clientId;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    clientId = value;
                }
                else
                {
                    throw new Exception("Service.ClientId value cannot be null.");
                }
            }
        }

        /// <summary>
        /// The password for the user principal
        /// </summary>
        public string ClientSecret { get; set; } = null;

        /// <summary>
        /// The maximum number of attempts to retry a request blocked by service protection limits.
        /// Default is 3.
        /// </summary>
        public byte MaxRetries { get; set; } = 3;

        public string Audience
        {
            get
            {
                return audience;
            }
        }

        public string SubmitClaimPath
        {
            get
            {
                return submitClaimPath;
            }
        }

        public string DailyReturnFileListPath
        {
            get
            {
                return dailyReturnFileListPath;
            }
        }

        public string DailyReturnDownloadPath
        {
            get
            {
                return dailyReturnDownloadPath;
            }
        }

        public string BiWeeklyRetunrFileListPath
        {
            get
            {
                return biweeklyRetunrFileListPath;
            }
        }
        public string BiWeeklyReturnDownloadPath
        {
            get
            {
                return biweeklyReturnDownloadPath;
            }
        }
        
        private static string GetParameterValue(string parameter)
        {
            try
            {
                string value = connectionString
                    .Split(';')
                    .Where(s => s.Trim()
                    .StartsWith(parameter))
                    .FirstOrDefault()
                    .Split('=')[1];
                if (value.ToLower() == "null")
                {
                    return string.Empty;
                }
                return value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
