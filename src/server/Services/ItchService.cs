using System.Net.Http.Headers;
using System.Text.Json;
using Game.Server.Services.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Game.Server.Services;

public class ItchService : Abstractions.IItchService
{
    private readonly HttpClient client;
    private readonly IConfiguration configuration;

    public ItchService(HttpClient client, IConfiguration configuration)
    {
        this.client = client;
        this.configuration = configuration;
    }

    public string GetLoginUrl()
    {
        return QueryHelpers.AddQueryString(
            configuration["Itch:Authority"]
                ?? throw new InvalidOperationException(
                    "Incomplete Itch configuration: missing Authority field"
                ),
            new Dictionary<string, string?>
            {
                {
                    "client_id",
                    configuration["Itch:ClientId"]
                        ?? throw new InvalidOperationException(
                            "Incomplete Itch configuration: missing ClientId field"
                        )
                },
                { "scope", "profile:me" },
                { "response_type", "token" },
                {
                    "redirect_uri",
                    $"{configuration["Server:Url"] ?? throw new InvalidOperationException("Incomplete Server configuration: missing Url field")}/login-callback"
                },
            }
        );
    }

    public async Task<ItchProfile> FetchProfile(string accessToken)
    {
        var response = await MakeRequest("me", accessToken, HttpMethod.Get);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ItchProfile>(GetJsonSerializerOptions())
            ?? throw new InvalidOperationException("Itch profile response was null");
    }

    public async Task<ItchCredentialsInfo> CheckCredentials(string accessToken)
    {
        var response = await MakeRequest("credentials/info", accessToken, HttpMethod.Get);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ItchCredentialsInfo>(
                GetJsonSerializerOptions()
            ) ?? throw new InvalidOperationException("Itch profile credentials info was null");
    }

    /// <summary>
    /// Calls the path relative to https://itch.io/api/1/key/
    /// Example: "me" will result in https://itch.io/api/1/key/me
    /// </summary>
    private async Task<HttpResponseMessage> MakeRequest(
        string path,
        string apiToken,
        HttpMethod method
    )
    {
        var request = new HttpRequestMessage(method, $"{configuration["Itch:Api"]}/key/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        return await client.SendAsync(request);
    }

    private static JsonSerializerOptions GetJsonSerializerOptions() =>
        new()
        {
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
}
