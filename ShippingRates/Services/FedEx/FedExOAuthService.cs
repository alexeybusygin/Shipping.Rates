using ShippingRates.ShippingProviders;
using ShippingRates.ShippingProviders.FedExRest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShippingRates.Services.FedEx
{
    internal class FedExOAuthService
    {
        static string GetOAuthRequestUri(bool isProduction)
            => $"https://{(isProduction ? "apis" : "apis-sandbox")}.fedex.com/oauth/token";

        public static async Task<string> GetTokenAsync(FedExRestProviderConfiguration configuration, HttpClient httpClient, Action<Error> reportError)
        {
            var token = TokenCacheService.GetToken(configuration.ClientId);
            if (!string.IsNullOrEmpty(token))
                return token;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, GetOAuthRequestUri(configuration.UseProduction));
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", configuration.ClientId),
                new KeyValuePair<string, string>("client_secret", configuration.ClientSecret)
            };
            requestMessage.Content = new FormUrlEncodedContent(postData);

            var responseMessage = await httpClient.SendAsync(requestMessage);
            var response = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FedExOAuthResponse>(response);

                TokenCacheService.AddToken(configuration.ClientId, result.AccessToken, result.ExpiresIn);

                return result.AccessToken;
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<Models.FedEx.FedExErrorResponse>(response);
                if ((errorResponse?.Errors?.Length ?? 0) > 0)
                {
                    foreach (var error in errorResponse.Errors)
                    {
                        reportError(new Error()
                        {
                            Number = error.Code,
                            Description = error.Message
                        });
                    }
                }
                else
                {
                    reportError(new Error() { Description = $"Unknown error while fetching FedEx OAuth token: {responseMessage.StatusCode} {response}" });
                }

                return null;
            }
        }

        class FedExOAuthResponse
        {
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
            [JsonPropertyName("scope")]
            public string Scope { get; set; }
        }
    }
}
