using Game.Server.Integration.Tests.Builders;
using Game.Server.Models.Replay;
using Shouldly;
using Xunit;

namespace Game.Server.Integration.Tests.Controllers;

[Collection("Default")]
public class ReplayControllerTests
{
    private readonly DefaultWebApplicationFactory applicationFactory;

    public ReplayControllerTests(DefaultWebApplicationFactory applicationFactory)
    {
        this.applicationFactory = applicationFactory;
    }

    [Fact]
    public async Task Create_replay_should_create_replay()
    {
        var user = ItchUserBuilder.BuildRandom();
        var sessionSecret = await SessionBuilder.ForUser(applicationFactory, user);
        var client = applicationFactory.CreateClient();

        var response = await client
            .PostAsJsonAsync(
                $"/api/game/42/session/{sessionSecret}/replay",
                new CreateReplayRequest
                {
                    TimeInMilliseconds = 1234,
                    LevelName = "create_replay_level",
                    Data = "{\"blerp\":420}"
                });

        response.EnsureSuccessStatusCode();
        var createdReplay = await response.Content.ReadFromJsonAsync<ReplayCreatedResponse>();
        createdReplay.ShouldNotBeNull();
        createdReplay.ReplayId.ShouldNotBe(0);

        // Switch user to verify user details returned are not from the session
        // but from the replay
        sessionSecret = await SessionBuilder.ForUser(applicationFactory, ItchUserBuilder.BuildRandom());

        var replayResponse = await client
            .GetFromJsonAsync<ReplayResponse>($"/api/game/42/session/{sessionSecret}/replay/{createdReplay.ReplayId}");
        replayResponse.ShouldNotBeNull();
        replayResponse.PlayerId.ShouldBe(user.Id);
        replayResponse.PlayerName.ShouldBe(user.UserName);
        replayResponse.LevelName.ShouldBe("create_replay_level");
        replayResponse.GameRevision.ShouldBe(42u);
        replayResponse.TimeInMilliseconds.ShouldBe(1234u);
        replayResponse.Data.ShouldBe("{\"blerp\":420}");
    }

    [Fact]
    public async Task Create_replay_overwrites_old_score()
    {
        var sessionSecret = await SessionBuilder.ForRandomUser(applicationFactory);
        var client = applicationFactory.CreateClient();

        var response = await client
            .PostAsJsonAsync(
                $"/api/game/42/session/{sessionSecret}/replay",
                new CreateReplayRequest
                {
                    TimeInMilliseconds = 1234,
                    LevelName = "create_replay_level",
                    Data = "{\"blerp\":420}"
                });

        var firstCreatedReplay = await response.Content.ReadFromJsonAsync<ReplayCreatedResponse>();
        firstCreatedReplay.ShouldNotBeNull();

        var replayResponse = await client
            .GetFromJsonAsync<ReplayResponse>($"/api/game/42/session/{sessionSecret}/replay/{firstCreatedReplay.ReplayId}");
        replayResponse.ShouldNotBeNull();
        replayResponse.TimeInMilliseconds.ShouldBe(1234u);

        response = await client
            .PostAsJsonAsync(
                $"/api/game/42/session/{sessionSecret}/replay",
                new CreateReplayRequest
                {
                    TimeInMilliseconds = 102,
                    LevelName = "create_replay_level",
                    Data = "{\"blerp\":421}"
                });

        var secondCreatedReplay = await response.Content.ReadFromJsonAsync<ReplayCreatedResponse>();
        secondCreatedReplay.ShouldNotBeNull();

        secondCreatedReplay.ReplayId.ShouldBe(firstCreatedReplay.ReplayId);

        replayResponse = await client
            .GetFromJsonAsync<ReplayResponse>($"/api/game/42/session/{sessionSecret}/replay/{firstCreatedReplay.ReplayId}");
        replayResponse.ShouldNotBeNull();
        replayResponse.TimeInMilliseconds.ShouldBe(102u);
        replayResponse.Data.ShouldBe("{\"blerp\":421}");
    }
}