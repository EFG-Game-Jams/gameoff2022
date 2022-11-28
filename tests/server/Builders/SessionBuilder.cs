using Game.Server.Integration.Tests.Mocks;
using Game.Server.Models.Session;
using Shouldly;

namespace Game.Server.Integration.Tests.Builders;

internal class SessionBuilder
{
    private SessionBuilder()
    {
    }

    /// <returns>The secret of the created session</returns>
    public async static Task<Guid> ForRandomUser(DefaultWebApplicationFactory applicationFactory)
    {
        var user = ItchUserBuilder.BuildRandom();
        return await ForUser(applicationFactory, user);
    }

    /// <returns>The secret of the created session</returns>
    public async static Task<Guid> ForUser(DefaultWebApplicationFactory applicationFactory, MockItchUser user)
    {
        var secret = Guid.NewGuid();
        var response = await applicationFactory
            .CreateClient()
            .PostAsync($"/api/game/42/session/{secret}/create/{user.AccessToken}", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        result.ShouldNotBeNull();
        result.Secret.ShouldNotBe(Guid.Empty);

        return result.Secret;
    }
}