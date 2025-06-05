using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MBS.Common
{    
    public class ClaimSubmitter
    {
        private string _userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:28.0) Gecko/20100101 Firefox/28.0";
        private string _icsHostAddress = "https://efq-ics.ehealthsask.ca";
        private string _contentType = "text/html";
        private string _dashboardUrl = string.Empty;
        private string _uploadClaimUrl = string.Empty;
        private string _uploadSummaryUrl = string.Empty;
        private string _downloadUrl = string.Empty; 
        private string _viewStateFlag = "id=\"__VIEWSTATE\" value=\"";
        private string _eventValidationFlag = "id=\"__EVENTVALIDATION\" value=\"";
        
        public ClaimSubmitter(string icsHostAddress)
        {
            _icsHostAddress = icsHostAddress;
            _dashboardUrl = _icsHostAddress + "/ICSDashboard.aspx";
            _uploadClaimUrl = _icsHostAddress + "/UploadClaims.aspx";
            _uploadSummaryUrl = _icsHostAddress + "/rptControlSummary.aspx";
            _downloadUrl = _icsHostAddress + "/DownloadReturns.aspx";
        }

        public ReturnModel SubmitClaimIn(string claimContent, byte[] clientCertificate, string privateKeyPassword, int timeZoneOffset)
        {
            var result = new ReturnModel();

            //result.ISCContent = "fake";
            //result.IsSuccess = true;
            //return result;

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cert = new X509Certificate2(clientCertificate, privateKeyPassword, X509KeyStorageFlags.MachineKeySet);
           
            var myCookieContainer = new CookieContainer();
            var request = WebRequest.Create(_icsHostAddress) as HttpWebRequest;
            request.CookieContainer = myCookieContainer;
            request.Method = "GET";
            request.KeepAlive = false;
            request.ClientCertificates.Add(cert);

            var indexPageContent = GetResponse(request);

            if (indexPageContent.IndexOf("Your Security Certificate has not been registered with your group number. This may take up to five business days.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("The Internet Claims Submission Service is temporarily unavailable.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.UNAVAILABLE;
                result.ISCContent = "The ICS Site is temporary unavailable";
            }
            else if (indexPageContent.IndexOf("Your ICS Security Certificate has been successfully installed.<br><br>Please contact the eHealth Service Desk at") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("Access was denied by the access policy. This may be due to a failure to meet access policy requirements.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else
            {
                var viewStateContent = GetPostData(indexPageContent);
                var responseCookie = GetResponseCookie(request);

                request = WebRequest.Create(_dashboardUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _icsHostAddress;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = _contentType;
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var dashboardContent = GetResponse(request);
                viewStateContent = GetPostData(dashboardContent);

                request = WebRequest.Create(_uploadClaimUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _dashboardUrl;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var uploadContent = GetResponse(request);
                var boundarySeparator = DateTime.UtcNow.AddHours(timeZoneOffset).ToFileTime().ToString();
                var postClaimContent = GetPostClaimContent(uploadContent, claimContent, boundarySeparator);

                // Set the request parameters
                request = WebRequest.Create(_uploadClaimUrl) as HttpWebRequest;
                request.Method = "POST";
                request.Referer = _uploadClaimUrl;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = "multipart/form-data; boundary=---------------------------" + boundarySeparator;
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);
                request.ContentLength = postClaimContent.Length;

                // Submit the request data
                var outputStream = request.GetRequestStream();
                request.AllowAutoRedirect = false;
                outputStream.Write(postClaimContent, 0, postClaimContent.Length);
                outputStream.Close();

                var submitTime = DateTime.UtcNow.AddHours(timeZoneOffset);
                var completedContent = GetResponse(request);

                var summaryContent = string.Empty;
                if (completedContent.IndexOf(" is empty. Please select another file to upload.") == -1)
                {
                    request = WebRequest.Create(_uploadSummaryUrl) as HttpWebRequest;
                    request.Method = "GET";
                    request.Referer = _uploadClaimUrl;
                    request.KeepAlive = false;
                    request.UserAgent = _userAgent;
                    request.ContentType = _contentType;
                    request.CookieContainer = myCookieContainer;
                    request.ClientCertificates.Add(cert);
                    request.CookieContainer.Add(responseCookie);

                    summaryContent = GetResponse(request);

                    result.ISCContent = summaryContent + "<br><br><INPUT type='button' value='Print Report' onclick='window.print();'>";

                    GetSubmissionRecordIndex(result, submitTime, summaryContent);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorType = ErrorType.EMPTY_CONTENT;
                    result.ISCContent = "You had not upload any file, please select another file to upload";
                }
            }

            return result;
        }

        private void GetSubmissionRecordIndex(ReturnModel result, DateTime submitTime, string submissionReportContent)
        {
            result.IsSuccess = false;
            result.ErrorType = ErrorType.REJECTED_CLAIM;

            var submissionReceivedTextIndex = submissionReportContent.IndexOf("- Submissions Received for Run Code");
            var submissionRejectedTextIndex = submissionReportContent.IndexOf("- Submissions Rejected for Run Code");
            var noRejectedSubmissionTextIndex = submissionReportContent.IndexOf("No Rejected Submissions to Report");
            var noSubmissionRecordTextIndex = submissionReportContent.IndexOf("No Submissions to Report");

            if ((submissionReceivedTextIndex == -1 && submissionRejectedTextIndex == -1) || (noRejectedSubmissionTextIndex > -1 && noSubmissionRecordTextIndex > -1))
            {
                //Unable to find Received Run Code and Rejected Run Code OR not found No Submission and No Rejected
                result.IsSuccess = false;
                result.ErrorType = ErrorType.REJECTED_CLAIM;
                result.ISCContent = submissionReportContent + "<h2 style='color:red'>UNABLE TO DETECT VALIDATION REPORT FOR THE SUBMITTED CLAIMS! SUBMISSION TIME: " + submitTime.ToString("yyyy-MM-dd HH:mm:ss") + "</h2>";
            }
            else if (noSubmissionRecordTextIndex > -1 && submissionRejectedTextIndex > -1)
            {
                //Contain No Submission and contain Submission Rejected - must be rejected
                result.IsSuccess = false;
                result.ErrorType = ErrorType.REJECTED_CLAIM;
            }
            else if (submissionReceivedTextIndex > -1)
            {
                //Contain Submission Received
                var submitTimeIndexList = new List<int>();

                var i = 0;
                for (i = 0; i < 6; i++)
                {
                    var submissionRecordIndex = submissionReportContent.IndexOf("<b>Submission Date: " + submitTime.AddMinutes(i).ToString("dddd, MMM dd yyyy,  h:mm:"));

                    if (noRejectedSubmissionTextIndex == -1 && submissionRejectedTextIndex < submissionRecordIndex)
                    {
                        // If Submission Record Index after Submission Rejected
                        result.IsSuccess = false;
                        result.ErrorType = ErrorType.REJECTED_CLAIM;
                        break;
                    }
                    else if (submissionReceivedTextIndex < submissionRecordIndex)
                    {
                        //Submission Record Index after Submission Recevied
                        result.IsSuccess = true;
                        break;
                    }
                }

                if (i == 6)
                {
                    result.ISCContent = submissionReportContent + "<h2 style='color:red'>UNABLE TO DETECT SUBMISSION RECORD WITHIN 5 MIN OF SUBMISSION TIME: " + submitTime.ToString("yyyy-MM-dd HH:mm:ss") + "! CONTACT PERSPECT SUPPORT WITH THIS REPORT!</h2>";
                }
            }
        }

        public ReturnModel DownloadReturn(byte[] clientCertificate, string privateKeyPassword)
        {
            var result = new ReturnModel();
            //result.IsSuccess = true;
            //result.ISCContent = GetTestReturn();
            //return result;

            result.ISCContent = string.Empty;

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cert = new X509Certificate2(clientCertificate, privateKeyPassword, X509KeyStorageFlags.MachineKeySet);            

            var myCookieContainer = new CookieContainer();
            var request = WebRequest.Create(_icsHostAddress) as HttpWebRequest;
            request.CookieContainer = myCookieContainer;
            request.Method = "GET";
            request.KeepAlive = false;
            request.ClientCertificates.Add(cert);

            var indexPageContent = GetResponse(request);

            if (indexPageContent.IndexOf("Your Security Certificate has not been registered with your group number. This may take up to five business days.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("Access was denied by the access policy. This may be due to a failure to meet access policy requirements.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("The Internet Claims Submission Service is temporarily unavailable.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.UNAVAILABLE;
                result.ISCContent = "The ICS Site is temporary unavailable";
            }
            else
            {
                var viewStateContent = GetPostData(indexPageContent);
                var responseCookie = GetResponseCookie(request);

                request = WebRequest.Create(_dashboardUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _icsHostAddress;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = _contentType;
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var dashboardContent = GetResponse(request);
                viewStateContent = GetReturnPostData(dashboardContent);

                request = WebRequest.Create(_downloadUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _dashboardUrl;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var returnContent = GetResponse(request);

                if (string.IsNullOrEmpty(returnContent) || returnContent.IndexOf("No returns to pick up for") > -1)
                {
                    result.IsSuccess = false;
                    result.ErrorType = ErrorType.EMPTY_CONTENT;
                }
                else
                {
                    result.IsSuccess = true;
                    result.ISCContent = returnContent;
                }
            }

            return result;
        }

        public ReturnModel ViewSummary(byte[] clientCertificate, string privateKeyPassword)
        {
            var result = new ReturnModel();
            result.ISCContent = string.Empty;

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cert = new X509Certificate2(clientCertificate, privateKeyPassword, X509KeyStorageFlags.MachineKeySet);

            var myCookieContainer = new CookieContainer();
            var request = WebRequest.Create(_icsHostAddress) as HttpWebRequest;
            request.CookieContainer = myCookieContainer;
            request.Method = "GET";
            request.KeepAlive = false;
            request.ClientCertificates.Add(cert);

            var indexPageContent = GetResponse(request);

            if (indexPageContent.IndexOf("Your Security Certificate has not been registered with your group number. This may take up to five business days.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("Access was denied by the access policy. This may be due to a failure to meet access policy requirements.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.CERTIFICATE_ERROR;
                result.ISCContent = "Your certificate is not valid to access the MSB site!";
            }
            else if (indexPageContent.IndexOf("The Internet Claims Submission Service is temporarily unavailable.") > 0)
            {
                result.IsSuccess = false;
                result.ErrorType = ErrorType.UNAVAILABLE;
                result.ISCContent = "The ICS Site is temporary unavailable";
            }
            else
            {
                var viewStateContent = GetPostData(indexPageContent);
                var responseCookie = GetResponseCookie(request);

                request = WebRequest.Create(_dashboardUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _icsHostAddress;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = _contentType;
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var dashboardContent = GetResponse(request);
                viewStateContent = GetReturnPostData(dashboardContent);

                request = WebRequest.Create(_uploadSummaryUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Referer = _dashboardUrl;
                request.KeepAlive = false;
                request.UserAgent = _userAgent;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = myCookieContainer;
                request.ClientCertificates.Add(cert);
                request.CookieContainer.Add(responseCookie);

                var summaryContent = GetResponse(request);
                
                result.IsSuccess = true;
                result.ISCContent = summaryContent;
            }

            return result;
        }

        public bool TestCertificate(byte[] clientCertificate, string privateKeyPassword)
        {
            try
            {
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var cert = new X509Certificate2(clientCertificate, privateKeyPassword, X509KeyStorageFlags.MachineKeySet);

                var myCookieContainer = new CookieContainer();
                var request = WebRequest.Create(_icsHostAddress) as HttpWebRequest;
                request.CookieContainer = myCookieContainer;
                request.Method = "GET";
                request.KeepAlive = false;
                request.ClientCertificates.Add(cert);

                var response = GetResponse(request);
                if (response.IndexOf("You are logged off.  Please close your web browser, open a new web browser and try again.", StringComparison.OrdinalIgnoreCase) > 0 ||
                    response.IndexOf("Your Security Certificate has not been registered with your group number. This may take up to five business days.", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
        
        private byte[] GetPostClaimContent(string requestContent, string claimContent, string boundarySeparator)
        {
            var builder = new StringBuilder();

            var viewState = GetValue(requestContent, _viewStateFlag);
            builder.Append("-----------------------------").Append(boundarySeparator).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"__VIEWSTATE\" \r\n\r\n");
            builder.Append(viewState);
            builder.Append("\r\n");

            var eventValidation = GetValue(requestContent, _eventValidationFlag);
            builder.Append("-----------------------------").Append(boundarySeparator).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"__EVENTVALIDATION\" \r\n\r\n");
            builder.Append(eventValidation);
            builder.Append("\r\n");

            //file content
            builder.Append("-----------------------------").Append(boundarySeparator).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"ctl00$cntPlaceHolder$txtClaimsFile\"; filename=\"claimins\"").Append("\r\n");
            builder.Append("Content-Type: text/plain").Append("\r\n\r\n");
            builder.Append(claimContent);
            builder.Append("\r\n\r\n");

            builder.Append("-----------------------------").Append(boundarySeparator).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"ctl00$cntPlaceHolder$btnSubmit\" \r\n\r\n");
            builder.Append("Submit Claims");
            builder.Append("\r\n");

            builder.Append("-----------------------------").Append(boundarySeparator).Append("\r\n");
            builder.Append("Content-Disposition: form-data; name=\"ctl00$cntPlaceHolder$txtMsg\" \r\n\r\n");
            builder.Append("\r\n");

            builder.Append("-----------------------------").Append(boundarySeparator).Append("--");

            return ASCIIEncoding.ASCII.GetBytes(builder.ToString());
        }

        private string GetPostDataString(string responseData)
        {
            // get the page ViewState                            
            var viewState = GetValue(responseData, _viewStateFlag);

            // get page EventValidation                
            var eventValidation = GetValue(responseData, _eventValidationFlag);

            // Convert the submit string data into the byte array
            return string.Format("__VIEWSTATE={0}&__EVENTVALIDATION={1}",
                                    HttpUtility.UrlEncode(viewState),
                                    HttpUtility.UrlEncode(eventValidation));
        }
        
        private byte[] GetPostData(string responseData)
        {
            return Encoding.ASCII.GetBytes(GetPostDataString(responseData));
        }

        private byte[] GetReturnPostData(string responseData)
        {
            var data = GetPostDataString(responseData) + "&__EVENTARGUMENT=&__EVENTTARGET=ctl00$cntPlaceHolder$btnReturns";
            return Encoding.ASCII.GetBytes(data);
        }

        private string GetValue(string source, string target)
        {
            int i = source.IndexOf(target) + target.Length;
            int j = source.IndexOf("\"", i);
            return source.Substring(i, j - i);
        }

        private CookieCollection GetResponseCookie(HttpWebRequest objRequest)
        {
            return ((HttpWebResponse)objRequest.GetResponse()).Cookies;
        }

        private string GetResponse(HttpWebRequest objRequest)
        {
            var result = string.Empty;
            var objResponse = (HttpWebResponse)objRequest.GetResponse();
            using (var sr = new StreamReader(objResponse.GetResponseStream()))
            {
                result = sr.ReadToEnd();
                sr.Close();
            }

            return result;
        }

        private string GetTestReturn()
        {
            return
@"501ABC1023701234567890353MTSE, BEN                 Z17    11022101K111A00200018         ABAof  AD9                                                                                                                                                            *
501ABC1023711234567890353MTSE, BEN                 Z17    11022101K940B01000018         ABAof  AD9                                                                                                                                                            *
501ABC1023721234567890353MTSE, BEN                 Z17    11022101K940C99990018         ABAof  AD9                                                                                                                                                            *
501ABC1023731234567890353MTSE, BEN                 Z17    11022101K099L09033018         ABAof  AD9                                                                                                                                                            *
501ABC1023741234567890353MTSE, BEN                 Z17    11022101K303A01325018         ABAof  AD9                                                                                                                                                            *
601ABC102370123456789TESTING -COLONOSCOPY                                                      AD9                                                                                                                                                            *";
        }
    }
}
