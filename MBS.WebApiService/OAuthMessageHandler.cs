using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using MBS.Common;
using System.Text;

namespace MBS.WebApiService
{
    public class OAuthMessageHandler : DelegatingHandler
    {
        private readonly ServiceConfig config;
        private MSBAccessToken msbToken;

        public OAuthMessageHandler(ServiceConfig configParam,
                HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            config = configParam;
            msbToken = null;
        }

        /// <summary>
        /// Will refresh the ADAL AccessToken when it expires.
        /// </summary>
        /// <returns></returns>
        private AuthenticationHeaderValue GetAuthHeader()
        {
            var tokenValid = true;

            if (msbToken == null)
            {
                tokenValid = false;
            }
            else
            {
                var timeSpan = msbToken.token_expired_in - DateTime.UtcNow;
                if (timeSpan.TotalSeconds < 30)
                {
                    tokenValid = false;
                }
            }

            if (!tokenValid)
            {
                try
                {
                    var tokenSubmitTime = DateTime.UtcNow;
                    var tokenString = GetAuthorizeToken().Result;

                    if (!string.IsNullOrEmpty(tokenString))
                    {
                        using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(tokenString)))
                        {
                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MSBAccessToken));
                            msbToken = (MSBAccessToken)serializer.ReadObject(memoryStream);
                        }

                        msbToken.token_expired_in = tokenSubmitTime.AddSeconds(msbToken.expires_in);
                    }
                    else
                    {
                        throw new ServiceException(401, "No bearer token found, check client credential.", "No bearer token found, check client credential.");
                    }
                }
                catch (ServiceException ex1)
                {
                    throw;
                }
                catch (Exception ex2)
                {
                    if (ex2.GetBaseException().InnerException != null)
                    {
                        throw ex2.GetBaseException().InnerException;
                    }
                    else
                    {
                        throw ex2.GetBaseException();
                    }
                }
            }

            return new AuthenticationHeaderValue("Bearer", msbToken.access_token); //
        }

        private async Task<string> GetAuthorizeToken()
        {
            // Initialization.  
            string responseText = string.Empty;

            // Posting.  
            using (var client = new HttpClient())
            {
                // Setting Base address.  
                client.BaseAddress = new Uri(config.AuthenticationUrl);

                // Setting content type.  
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Initialization.  
                HttpResponseMessage response = new HttpResponseMessage();

                List<KeyValuePair<string, string>> allIputParams = new List<KeyValuePair<string, string>>();
                allIputParams.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                allIputParams.Add(new KeyValuePair<string, string>("client_id", config.ClientId));
                allIputParams.Add(new KeyValuePair<string, string>("client_secret", config.ClientSecret));
                allIputParams.Add(new KeyValuePair<string, string>("audience", config.Audience));

                // Convert Request Params to Key Value Pair.  
                // URL Request parameters.  
                HttpContent requestParams = new FormUrlEncodedContent(allIputParams);

                // HTTP POST  
                response = await client.PostAsync("Token", requestParams).ConfigureAwait(false);

                // Verification  
                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode != HttpStatusCode.NotModified)
                    {
                        responseText = await response.Content.ReadAsStringAsync();                        
                    }
                }
            }

            return responseText;
        }

        /// <summary>
        /// Overrides the default HttpClient.SendAsync operation so that authentication can be done.
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task<HttpResponseMessage> SendAsync(
                  HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                request.Headers.Authorization = GetAuthHeader();
                request.Headers.Add("Accept-Language", "en-US");
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
