using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hmm.Utility.Validation;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;

namespace Hmm.Infrastructure
{
    public class IdpUserProfileProvider
    {
        public static async Task<string> GetUserClaimAsync(string type, HttpContext context, HttpClient httpClient)
        {
            Guard.Against<ArgumentNullException>(context == null, nameof(context));
            Guard.Against<ArgumentNullException>(httpClient == null, nameof(httpClient));

            var response = await GetUserProfileAsync(context, httpClient);

            return response.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.CurrentCultureIgnoreCase))?.Value;
        }

        private static async Task<UserInfoResponse> GetUserProfileAsync(HttpContext context, HttpClient httpClient)
        {
            var metaDataResponse = await httpClient.GetDiscoveryDocumentAsync();
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
    }
}