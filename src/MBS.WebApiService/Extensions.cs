﻿using System;
using System.Collections.Generic;
using System.Net.Http;

namespace MBS.WebApiService
{
    /// <summary>
    /// Contains extension methods to clone HttpRequestMessage and HttpContent types.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Clones a HttpRequestMessage instance
        /// </summary>
        /// <param name="request">The HttpRequestMessage to clone.</param>
        /// <returns>A copy of the HttpRequestMessage</returns>
        public static HttpRequestMessage Clone(this HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content.Clone(),
                Version = request.Version
            };
            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        /// <summary>
        /// Clones a HttpContent instance
        /// </summary>
        /// <param name="content">The HttpContent to clone</param>
        /// <returns>A copy of the HttpContent</returns>
        public static HttpContent Clone(this HttpContent content)
        {

            if (content == null) return null;

            HttpContent clone = null;

            if (content is StringContent)
            {
                clone = new StringContent(content.ReadAsStringAsync().Result);
                clone.Headers.Clear();
                foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
                {
                    clone.Headers.Add(header.Key, header.Value);
                }
            }
                      
            return clone;

        }
    }
}
