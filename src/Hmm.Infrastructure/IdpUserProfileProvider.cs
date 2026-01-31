using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Utility.Validation;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;

namespace Hmm.Infrastructure
{
    /// <summary>
    /// Provides user profile information from the Identity Provider.
    /// Caches the discovery document to reduce network calls.
    /// </summary>
    public class IdpUserProfileProvider
    {
        private static DiscoveryDocumentResponse _cachedDiscoveryDocument;
        private static DateTime _cacheExpiration = DateTime.MinValue;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        /// <summary>
        /// Default cache duration for discovery document (1 hour).
        /// Discovery documents rarely change and are safe to cache.
        /// </summary>
        public static TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

        public static async Task<string> GetUserClaimAsync(string type, HttpContext context, HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(httpClient);

            var response = await GetUserProfileAsync(context, httpClient);

            return response.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.CurrentCultureIgnoreCase))?.Value;
        }

        private static async Task<UserInfoResponse> GetUserProfileAsync(HttpContext context, HttpClient httpClient)
        {
            var metaDataResponse = await GetCachedDiscoveryDocumentAsync(httpClient);
            if (metaDataResponse.IsError)
            {
                throw new Exception("Problem accessing the discover endpoint", metaDataResponse.Exception);
            }
            var accessToken = await context.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            var userInfoResponse = await httpClient.GetUserInfoAsync(
                new UserInfoRequest
                {
                    Address = metaDataResponse.UserInfoEndpoint,
                    Token = accessToken
                });

            if (userInfoResponse.IsError)
            {
                throw new Exception("Problem accessing the user endpoint", userInfoResponse.Exception);
            }

            return userInfoResponse;
        }

        /// <summary>
        /// Gets the discovery document with caching.
        /// Thread-safe implementation with double-check locking pattern.
        /// </summary>
        private static async Task<DiscoveryDocumentResponse> GetCachedDiscoveryDocumentAsync(HttpClient httpClient)
        {
            // Fast path: check if cache is valid without acquiring lock
            if (_cachedDiscoveryDocument != null && DateTime.UtcNow < _cacheExpiration)
            {
                return _cachedDiscoveryDocument;
            }

            await _cacheLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cachedDiscoveryDocument != null && DateTime.UtcNow < _cacheExpiration)
                {
                    return _cachedDiscoveryDocument;
                }

                // Fetch and cache the discovery document
                var response = await httpClient.GetDiscoveryDocumentAsync();
                if (!response.IsError)
                {
                    _cachedDiscoveryDocument = response;
                    _cacheExpiration = DateTime.UtcNow.Add(CacheDuration);
                }

                return response;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Clears the cached discovery document.
        /// Useful for testing or when the IDP configuration changes.
        /// </summary>
        public static void ClearCache()
        {
            _cacheLock.Wait();
            try
            {
                _cachedDiscoveryDocument = null;
                _cacheExpiration = DateTime.MinValue;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}