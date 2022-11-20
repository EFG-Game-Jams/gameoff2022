using Game.Server.Integration.Tests.Builders;
using Game.Server.Models.Session;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Game.Server.Integration.Tests.Controllers;

[Collection("Default")]
public class SessionControllerTests
{
    private readonly DefaultWebApplicationFactory applicationFactory;

    public SessionControllerTests(DefaultWebApplicationFactory applicationFactory)
    {
        this.applicationFactory = applicationFactory;
    }

    [Fact]
    public async Task Register_should_return_session_guid()
    {
        var user = ItchUserBuilder.BuildRandom();
        var response = await applicationFactory
            .CreateClient()
            .PostAsync($"/api/game/42/session/create/{user.AccessToken}", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        result.ShouldNotBeNull();
        result.PlayerName.ShouldBe(user.UserName);
        result.Secret.ShouldNotBe(Guid.Empty);

        using var scope = applicationFactory.Services.CreateScope();
        using var database = scope.ServiceProvider.GetRequiredService<ReplayDatabase>();

        var sessionEntity = await database.Sessions
            .AsNoTracking()
            .Include(s => s.Player)
            .SingleAsync(s => s.Secret == result.Secret);
        sessionEntity.Player.Name.ShouldBe(user.UserName);
        sessionEntity.Player.ItchIdentifier.ShouldBe(user.Id);
        sessionEntity.CreatedUtc.Date.ShouldBe(DateTime.Today);
    }

    [Fact]
    public async Task Register_allows_duplicate_user_names()
    {
        var userOne = ItchUserBuilder.Build("duplicate_name");
        var response = await applicationFactory
            .CreateClient()
            .PostAsync($"/api/game/42/session/create/{userOne.AccessToken}", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        result.ShouldNotBeNull();
        result.PlayerName.ShouldBe(userOne.UserName);
        result.Secret.ShouldNotBe(Guid.Empty);

        using var scope = applicationFactory.Services.CreateScope();
        using var database = scope.ServiceProvider.GetRequiredService<ReplayDatabase>();

        var userOneSessionEntity = await database.Sessions
            .AsNoTracking()
            .Include(s => s.Player)
            .SingleAsync(s => s.Secret == result.Secret);
        userOneSessionEntity.Player.Name.ShouldBe(userOne.UserName);
        userOneSessionEntity.Player.ItchIdentifier.ShouldBe(userOne.Id);

        var userTwo = ItchUserBuilder.Build("duplicate_name");
        response = await applicationFactory
            .CreateClient()
            .PostAsync($"/api/game/42/session/create/{userTwo.AccessToken}", null);
        response.EnsureSuccessStatusCode();

        result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        result.ShouldNotBeNull();
        result.PlayerName.ShouldBe(userTwo.UserName);
        result.Secret.ShouldNotBe(Guid.Empty);

        var userTwoSessionEntity = await database.Sessions
            .AsNoTracking()
            .Include(s => s.Player)
            .SingleAsync(s => s.Secret == result.Secret);
        userTwoSessionEntity.Player.Name.ShouldBe(userTwo.UserName);
        userTwoSessionEntity.Player.ItchIdentifier.ShouldBe(userTwo.Id);

        userOneSessionEntity.Id.ShouldNotBe(userTwoSessionEntity.Id);
        userOneSessionEntity.PlayerId.ShouldNotBe(userTwoSessionEntity.PlayerId);
        userOneSessionEntity.Secret.ShouldNotBe(userTwoSessionEntity.Secret);
    }

    [Fact]
    public async Task GetSessionDetails_should_return_player_name()
    {
        var user = ItchUserBuilder.BuildRandom();
        var client = applicationFactory.CreateClient();

        var response = await client
            .PostAsync($"/api/game/42/session/create/{user.AccessToken}", null);
        response.EnsureSuccessStatusCode();

        var createdSession = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        createdSession.ShouldNotBeNull();

        var sessionDetails = await client
            .GetFromJsonAsync<SessionDetailsResponse>($"/api/game/42/session/{createdSession.Secret}/details");
        sessionDetails.ShouldNotBeNull();
        sessionDetails.PlayerName.ShouldBe(user.UserName);
    }
}