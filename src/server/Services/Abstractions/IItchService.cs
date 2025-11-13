using Game.Server.Services.Models;

namespace Game.Server.Services.Abstractions;

public interface IItchService
{
    string GetLoginUrl();

    Task<ItchProfile> FetchProfile(string accessToken);

    Task<ItchCredentialsInfo> CheckCredentials(string accessToken);
}
