using System;
using Newtonsoft.Json.Linq;

namespace MBS.WebApiService
{
    /// <summary>
    /// An exception that captures data returned by the Web API
    /// </summary>
    public class ServiceException : Exception
    {
        public int StatusCode { get; private set; }

        public string DetailMessage { get; private set; }



        public JToken ErrorResponse { get; private set; }

        public ServiceException(int statuscode, string reasonphrase, string detailMessage, JToken errorResponse = null) : base(reasonphrase)
        {
            StatusCode = statuscode;
            DetailMessage = detailMessage;
            ErrorResponse = errorResponse;
        }
    }
}
