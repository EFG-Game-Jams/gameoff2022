using Game.Server.Services.Abstractions;
using Game.Server.Services.Models;

namespace Game.Server.Integration.Tests.Mocks;

internal class ItchServiceMock : IItchService
{
    public static List<MockItchUser> MockUsers { get; } = new();

    public Task<ItchCredentialsInfo> CheckCredentials(string accessToken)
    {
        var mockUser = MockUsers.FirstOrDefault(u => u.AccessToken == accessToken);

        if (mockUser == null)
        {
            throw new HttpRequestException("Non existing Itch user");
        }

        return Task.FromResult(new ItchCredentialsInfo { Scopes = new[] { "profile:me" } });
    }

    public Task<ItchProfile> FetchProfile(string accessToken)
    {
        var mockUser = MockUsers.FirstOrDefault(u => u.AccessToken == accessToken);

        if (mockUser == null)
        {
            throw new HttpRequestException("Non existing Itch user");
        }

        return Task.FromResult(
            new ItchProfile
            {
                User = new()
                {
                    CoverUrl = null,
                    Developer = false,
                    DisplayName = null,
                    Gamer = true,
                    Id = mockUser.Id,
                    PressUser = false,
                    Url = null,
                    Username = mockUser.UserName,
                },
            }
        );
    }

    public string GetLoginUrl() => "https://example.com/login";
}

internal record class MockItchUser(int Id, string UserName, string AccessToken);
