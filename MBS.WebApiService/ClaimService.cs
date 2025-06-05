using MBS.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MBS.WebApiService
{
    public class ClaimService : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly ServiceConfig config;

        /// <summary>
        /// The BaseAddresss property of the HttpClient.
        /// </summary>
        public Uri BaseAddress { get { return httpClient.BaseAddress; } }

        public ClaimService(ServiceConfig config)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            this.config = config;
            HttpMessageHandler messageHandler = new OAuthMessageHandler(
                    config,
                    new HttpClientHandler() { UseCookies = false }
                );

            httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri(config.Url) 
            };
        }

        #region Fake Methods For Testing

        public ReturnModel FakeUploadFile()
        {
            var returnModel = new ReturnModel()
            {
                IsSuccess = true,
                ErrorType = ErrorType.EMPTY_CONTENT,
                ISCContent = "hello",
                FileName = "hello.txt"
            };

            return returnModel;
        }

        public ReturnFileNameListModel FakeGetDailyReturnFileList()
        {
            return new ReturnFileNameListModel()
            {
                IsSuccess = true,
                ErrorMessage = string.Empty,
                FileNames = Enumerable.Empty<string>()
            };
        }

        public ReturnFileNameListModel FakeGetBiWeeklyReturnFileList(string groupUserKey, string groupNumber)
        {
            
            return new ReturnFileNameListModel()
            {
                IsSuccess = true,
                ErrorMessage = string.Empty,
                FileNames = new List<string>() { "I021_G78_test_REPRINT_20240701052451.txt" }
            };
        }

        public ReturnFileModel FakeGetBiWeeklyReturnFile(string groupUserKey, string fileName)
        {
            var returnContent = System.IO.File.ReadAllText("C:\\Personal\\MBS\\Files\\Test Return\\dawn\\I021_G78_test_REPRINT_20240701052451.txt");

            var returnModel = new ReturnFileModel()
            {
                IsSuccess = true,
                ErrorMessage = string.Empty,
                FileContent = returnContent,
                FileName = fileName,
                ReturnFileType = ReturnFileType.BIWEEKLY
            };

            return returnModel;
        }

        #endregion


        public ReturnModel UploadFile(string groupUserKey, string claimContent, string groupNumber, string fileName)
        {
            var returnModel = new ReturnModel()
            {
                IsSuccess = false,
                ErrorType = ErrorType.UNAVAILABLE,
                ISCContent = string.Empty,
                FileName = string.Empty
            };

            try
            {
                var parameters = new Dictionary<string, string> { { "group_user_key", groupUserKey } };

                var returnObject = UploadFileAsync(config.SubmitClaimPath, claimContent, fileName, parameters).ConfigureAwait(false).GetAwaiter().GetResult();
                if (returnObject != null)
                {
                    returnModel.IsSuccess = true;
                    returnModel.FileName = returnObject["fileName"].ToString();
                    returnModel.ISCContent = returnObject["report"].ToString(); //Base64 PDF Content
                }
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == 400)
                {
                    if (ex.Message.Equals("Not a Valid Group User Key.", StringComparison.OrdinalIgnoreCase) || ex.Message.StartsWith("User doesn't have access to this Group ", StringComparison.OrdinalIgnoreCase))
                    {
                        returnModel.ErrorType = ErrorType.UNAUTHORIZED;
                        returnModel.ISCContent = "Unable to authenticate with MSB, check authentication url and the provided group_user_key, group number, etc.";
                    }
                    else
                    {
                        returnModel.ErrorType = ErrorType.VALIDATION_FAILED;
                        if (ex.ErrorResponse != null)
                        {
                            returnModel.FileName = ex.ErrorResponse["fileName"].ToString();
                            returnModel.ISCContent = ex.ErrorResponse["report"].ToString(); //Base64 PDF Content
                        }
                    }
                }
                else if (ex.StatusCode == 500)
                {
                    returnModel.ErrorType = ErrorType.MSB_SERVER_ERROR;
                    returnModel.ISCContent = ex.Message;
                }
                else if (ex.StatusCode == 401)
                {
                    returnModel.ErrorType = ErrorType.UNAUTHORIZED;
                    returnModel.ISCContent = "Unable to authenticate with MSB, check authentication url and the provided group_user_key, group number, etc.";
                }
                else if (ex.StatusCode == 409)
                {
                    returnModel.FileName = fileName;
                    returnModel.ISCContent = ex.Message;
                    returnModel.ErrorType = ErrorType.DUPLICATE_FILENAME;
                }
                else
                {
                    returnModel.ISCContent = ex.Message; //Text
                }
            }
            catch (Exception ex2)
            {
                returnModel.ErrorType = ErrorType.SERVER_ERROR;
                returnModel.ISCContent = ex2.Message; //Text
            }

            return returnModel;
        }

        /// <summary>
        /// Posts a payload to the specified resource asynchronously.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="body">The payload to send.</param>
        /// <param name="headers">Any headers to control optional behaviors.</param>
        /// <returns>The response from the request.</returns>
        private async Task<JToken> UploadFileAsync(string path, string body, string fileName, Dictionary<string, string> requestParameters, Dictionary<string, List<string>> headers = null)
        {
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    var content = new MultipartFormDataContent();

                    foreach (var parameter in requestParameters)
                    {
                        content.Add(new StringContent(parameter.Value), name: parameter.Key);
                    }

                    content.Add(new StringContent(body), "file", fileName);
                    
                    message.Content = content;

                    if (headers != null)
                    {
                        foreach (KeyValuePair<string, List<string>> header in headers)
                        {
                            message.Headers.Add(header.Key, header.Value);
                        }
                    }

                    using (HttpResponseMessage response = await SendAsync(message).ConfigureAwait(false))
                    {
                        var jResponse = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        if (jResponse.ContainsKey("response"))
                        {
                            return jResponse.Property("response").Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return null;
        }

        public ReturnFileNameListModel GetDailyReturnFileList(string groupUserKey, string groupNumber)
        {
            var returnModel = new ReturnFileNameListModel()
            {
                IsSuccess = false,
                ErrorMessage = string.Empty,
                FileNames = Enumerable.Empty<string>()
            };

            try
            {
                var parameters = new Dictionary<string, string> { { "group_user_key", groupUserKey }, { "group_number", groupNumber } };

                var result = GetAsync(config.DailyReturnFileListPath, parameters).ConfigureAwait(false).GetAwaiter().GetResult();

                returnModel.IsSuccess = true;
                returnModel.FileNames = result["reports"].ToObject<string[]>();      
            }
            catch (Exception ex)
            {
                returnModel.ErrorMessage = ex.Message;   
            }
                
            return returnModel;
        }

        public ReturnFileModel GetDailyReturnFile(string groupUserKey, string fileName)
        {
            var returnModel = new ReturnFileModel()
            {
                IsSuccess = false,
                ErrorMessage = string.Empty,
                FileContent = string.Empty,
                FileName = fileName,
                ReturnFileType = ReturnFileType.DAILY
            };

            try
            {
                var parameters = new Dictionary<string, string> { { "group_user_key", groupUserKey }, { "filename", fileName } };

                var result = GetAsync(config.DailyReturnDownloadPath, parameters).ConfigureAwait(false).GetAwaiter().GetResult();

                var base64Data = Convert.FromBase64String(result.ToString());

                returnModel.IsSuccess = true;
                returnModel.FileContent = System.Text.Encoding.UTF8.GetString(base64Data);
            }
            catch (Exception ex)
            {
                returnModel.ErrorMessage = ex.Message;
            }

            return returnModel;
        }
        
        public ReturnFileNameListModel GetBiWeeklyReturnFileList(string groupUserKey, string groupNumber)
        {
            var returnModel = new ReturnFileNameListModel()
            {
                IsSuccess = false,
                ErrorMessage = string.Empty,
                FileNames = Enumerable.Empty<string>()
            };

            try
            {
                var parameters = new Dictionary<string, string> { { "group_number", groupNumber }, { "group_user_key", groupUserKey } };

                var result = GetAsync(config.BiWeeklyRetunrFileListPath, parameters).ConfigureAwait(false).GetAwaiter().GetResult();

                returnModel.IsSuccess = true;
                returnModel.FileNames = result["reports"].ToObject<string[]>();
            }
            catch (Exception ex)
            {
                returnModel.ErrorMessage = ex.Message;
            }

            return returnModel;
        }
    
        public ReturnFileModel GetBiWeeklyReturnFile(string groupUserKey, string fileName)
        {
            var returnModel = new ReturnFileModel()
            {
                IsSuccess = false,
                ErrorMessage = string.Empty,
                FileContent = string.Empty,
                FileName = fileName,
                ReturnFileType = ReturnFileType.BIWEEKLY
            };

            try
            {
                var parameters = new Dictionary<string, string> { { "group_user_key", groupUserKey }, { "filename", fileName } };

                var result = GetAsync(config.BiWeeklyReturnDownloadPath, parameters).ConfigureAwait(false).GetAwaiter().GetResult();

                returnModel.IsSuccess = true;
                var base64Data = Convert.FromBase64String(result.ToString());

                returnModel.IsSuccess = true;
                returnModel.FileContent = System.Text.Encoding.UTF8.GetString(base64Data);
            }
            catch (Exception ex)
            {
                returnModel.ErrorMessage = ex.Message;
            }

            return returnModel;
        }
        
        /// <summary>
        /// Retrieves data from a specified resource asychronously.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="headers">Any custom headers to control optional behaviors.</param>
        /// <returns>The response to the request.</returns>
        private async Task<JToken> GetAsync(string path, Dictionary<string, string> requestParameters, Dictionary<string, List<string>> headers = null)
        {
            try
            {                
                using (var message = new HttpRequestMessage(HttpMethod.Get, path + "?" + CreateQueryParameters(requestParameters)))
                {
                    if (headers != null)
                    {
                        foreach (KeyValuePair<string, List<string>> header in headers)
                        {
                            message.Headers.Add(header.Key, header.Value);
                        }
                    }

                    using (HttpResponseMessage response = await SendAsync(message, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.NotModified)
                        {
                            var jResponse = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            if (jResponse.ContainsKey("response"))
                            {
                                return jResponse.Property("response").Value;
                            }
                        }

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            } 
        }        

        /// <summary>
        /// Sends all requests with retry capabilities
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="httpCompletionOption">Indicates if HttpClient operations should be considered completed either as soon as a response is available, or after reading the entire response message including the content.</param>
        /// <param name="retryCount">The number of retry attempts</param>
        /// <returns>The response for the request.</returns>
        private async Task<HttpResponseMessage> SendAsync(
                        HttpRequestMessage request,
                        HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseHeadersRead,
                        int retryCount = 0)
        {
            HttpResponseMessage response;

            try
            {
                //The request is cloned so it can be sent again.
                response = await httpClient.SendAsync(request, httpCompletionOption).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {               
                throw ParseError(response);
            }
            else
            {
                return response;
            }
        }
        
        /// <summary>
        /// Parses the Web API error
        /// </summary>
        /// <param name="response">The response that failed.</param>
        /// <returns></returns>
        private ServiceException ParseError(HttpResponseMessage response)
        {
            int statusCode = (int)response.StatusCode;
            string reasonPhrase = response.ReasonPhrase;
            string responseContent = string.Empty;

            try
            {
                responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                JObject jResposne = JObject.Parse(responseContent);
                IDictionary<string, JToken> dTokenContent = jResposne;

                if (dTokenContent.ContainsKey("message"))
                {
                    reasonPhrase = jResposne.Property("message").Value.ToString();
                }

                var detailMessage = string.Empty;
                if (dTokenContent.ContainsKey("details")) //array
                {
                    var ss = jResposne["details"].ToObject<string[]>();
                    detailMessage = string.Join(", ", ss);
                }

                JToken jErrorResponse = null;
                if (dTokenContent.ContainsKey("response"))
                {
                    jErrorResponse = jResposne.Property("response").Value;
                }

                return new ServiceException(statusCode, reasonPhrase, detailMessage, jErrorResponse);
            }
            catch (Exception ex)
            {
                if (statusCode == 404)
                {
                    return new ServiceException(statusCode, reasonPhrase, reasonPhrase);
                }
                else
                {
                    return new ServiceException(statusCode, reasonPhrase, StripHTML(responseContent).Trim());
                }
            }
        }

        private string CreateQueryParameters(Dictionary<string, string> parameters)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            foreach(var parameter in parameters)
            {
                queryString.Add(parameter.Key, parameter.Value);
            }

            return queryString.ToString();
        }

        ~ClaimService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                ReleaseClient();
            }
            else
            {
            }

            ReleaseClient();
        }

        private void ReleaseClient()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
            }
        }

        private string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}