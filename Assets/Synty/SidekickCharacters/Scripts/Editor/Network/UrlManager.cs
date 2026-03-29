// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Net;
using UnityEngine.Networking;

namespace Synty.SidekickCharacters.Network
{
    /// <summary>
    ///     Manages all URLs used in the application to communicate with the Synty API.
    /// </summary>
    internal static class UrlManager
    {
        // Base URL
        private const string _BASE_URL = "https://api.syntystudios.com/";

        // API version we're connecting to
        private const int _API_VERSION = 1;

        // API URLs
        private const string _API_GET_DB_UPDATE = "sidekick-database";

        /// <summary>
        ///     Creates a new POST request built from the provided URL and form data with the api key added into the header if required.
        /// </summary>
        /// <param name="url">The URL to attach the post request to.</param>
        /// <param name="formData">Any form data to attach to the request.</param>
        /// <param name="apiKey">The api key to use, if required.</param>
        /// <returns>A <c>UnityWebRequest.POST</c> with the given details.</returns>
        public static UnityWebRequest CreatePostRequest(string url, List<IMultipartFormSection> formData, string apiKey = "")
        {
            UnityWebRequest webRequest = UnityWebRequest.Post(url, formData);
            if (apiKey != "")
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            return webRequest;
        }

        /// <summary>
        ///     Creates a new GET request built from the provided URL with the api key added into the header if required.
        /// </summary>
        /// <param name="url">The URL to attach the post request to.</param>
        /// <param name="apiKey">The api key to use, if required.</param>
        /// <returns>A <c>UnityWebRequest.GET</c> with the given details.</returns>
        public static UnityWebRequest CreateGetRequest(string url, string apiKey = "")
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            if (apiKey != "")
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            return webRequest;
        }

        /// <summary>
        ///     Returns the base API URL.
        /// </summary>
        /// <returns>The base API URL.</returns>
        private static string GetBaseUrl()
        {
            return _BASE_URL + "api/v" + _API_VERSION + "/";
        }

        /// <summary>
        ///     Gets the Sidekick DB update URL.
        /// </summary>
        /// <returns>The Sidekick DB URL.</returns>
        public static string GetDbUpdateUrl()
        {
            // TODO : do we still need this encoding?
            // As this URL contains "unsafe" characters, we encode them first.
            return GetBaseUrl() + WebUtility.UrlEncode(_API_GET_DB_UPDATE);
        }
    }
}
#endif
