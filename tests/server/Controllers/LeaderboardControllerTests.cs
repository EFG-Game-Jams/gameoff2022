using Game.Server.Integration.Tests.Builders;
using Game.Server.Models.Leaderboard;
using Shouldly;
using Xunit;

namespace Game.Server.Integration.Tests.Controllers;

[Collection("Default")]
public class LeaderboardControllerTests
{
    private const int gameRevision = 42;

    private readonly DefaultWebApplicationFactory applicationFactory;

    public LeaderboardControllerTests(DefaultWebApplicationFactory applicationFactory)
    {
        this.applicationFactory = applicationFactory;
    }

    [Fact]
    public async Task GetGlobalLeaderboard_returns_expected_result()
    {
        var userAlpha = ItchUserBuilder.BuildRandom();
        var userBravo = ItchUserBuilder.BuildRandom();
        var userCharlie = ItchUserBuilder.BuildRandom();

        var sessionAlpha = await SessionBuilder.ForUser(applicationFactory, userAlpha);
        var sessionBravo = await SessionBuilder.ForUser(applicationFactory, userBravo);
        var sessionCharlie = await SessionBuilder.ForUser(applicationFactory, userCharlie);

        var levelOneName = "gglt_level_one";
        var levelTwoName = "gglt_level_two";
        var levelThreeName = "gglt_level_three";

        await CreateReplays(sessionAlpha, (levelOneName, 1000), (levelTwoName, 500), (levelThreeName, 1500));
        await CreateReplays(sessionBravo, (levelOneName, 500), (levelTwoName, 1500), (levelThreeName, 1000));
        await CreateReplays(sessionCharlie, (levelOneName, 1500), (levelTwoName, 1000), (levelThreeName, 500));

        var client = applicationFactory.CreateClient();

        // Level one
        var list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{sessionAlpha}/leaderboard?levelName={levelOneName}&take=10&skip=0&sortOrder=TimeAscending");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(3);

        list.Items[0].LevelName.ShouldBe(levelOneName);
        list.Items[0].PlayerName.ShouldBe(userBravo.UserName);
        list.Items[0].TimeInMilliseconds.ShouldBe(500u);

        list.Items[1].LevelName.ShouldBe(levelOneName);
        list.Items[1].PlayerName.ShouldBe(userAlpha.UserName);
        list.Items[1].TimeInMilliseconds.ShouldBe(1000u);

        list.Items[2].LevelName.ShouldBe(levelOneName);
        list.Items[2].PlayerName.ShouldBe(userCharlie.UserName);
        list.Items[2].TimeInMilliseconds.ShouldBe(1500u);

        // Level two
        list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{sessionAlpha}/leaderboard?levelName={levelTwoName}&take=10&skip=0&sortOrder=TimeAscending");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(3);

        list.Items[0].LevelName.ShouldBe(levelTwoName);
        list.Items[0].PlayerName.ShouldBe(userAlpha.UserName);
        list.Items[0].TimeInMilliseconds.ShouldBe(500u);

        list.Items[1].LevelName.ShouldBe(levelTwoName);
        list.Items[1].PlayerName.ShouldBe(userCharlie.UserName);
        list.Items[1].TimeInMilliseconds.ShouldBe(1000u);

        list.Items[2].LevelName.ShouldBe(levelTwoName);
        list.Items[2].PlayerName.ShouldBe(userBravo.UserName);
        list.Items[2].TimeInMilliseconds.ShouldBe(1500u);

        // Level three
        list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{sessionAlpha}/leaderboard?levelName={levelThreeName}&take=10&skip=0&sortOrder=TimeAscending");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(3);

        list.Items[0].LevelName.ShouldBe(levelThreeName);
        list.Items[0].PlayerName.ShouldBe(userCharlie.UserName);
        list.Items[0].TimeInMilliseconds.ShouldBe(500u);

        list.Items[1].LevelName.ShouldBe(levelThreeName);
        list.Items[1].PlayerName.ShouldBe(userBravo.UserName);
        list.Items[1].TimeInMilliseconds.ShouldBe(1000u);

        list.Items[2].LevelName.ShouldBe(levelThreeName);
        list.Items[2].PlayerName.ShouldBe(userAlpha.UserName);
        list.Items[2].TimeInMilliseconds.ShouldBe(1500u);

        // Level one (reversed)
        list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{sessionAlpha}/leaderboard?levelName=gglt_level_one&take=10&skip=0&sortOrder=TimeDescending");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(3);

        list.Items[0].LevelName.ShouldBe(levelOneName);
        list.Items[0].PlayerName.ShouldBe(userCharlie.UserName);
        list.Items[0].TimeInMilliseconds.ShouldBe(1500u);

        list.Items[1].LevelName.ShouldBe(levelOneName);
        list.Items[1].PlayerName.ShouldBe(userAlpha.UserName);
        list.Items[1].TimeInMilliseconds.ShouldBe(1000u);

        list.Items[2].LevelName.ShouldBe(levelOneName);
        list.Items[2].PlayerName.ShouldBe(userBravo.UserName);
        list.Items[2].TimeInMilliseconds.ShouldBe(500u);
    }

    [Fact]
    public async Task GetLeaderboardNeighbours_returns_expected_result()
    {
        var user = ItchUserBuilder.BuildRandom();
        var session = await SessionBuilder.ForUser(applicationFactory, user);

        var levelName = "glnt_level_one";

        await CreateRandomUserReplays((levelName, 0));
        await CreateRandomUserReplays((levelName, 100));
        await CreateRandomUserReplays((levelName, 200));
        await CreateRandomUserReplays((levelName, 300));
        await CreateRandomUserReplays((levelName, 400));
        await CreateRandomUserReplays((levelName, 500));
        await CreateRandomUserReplays((levelName, 600));
        await CreateRandomUserReplays((levelName, 700));
        await CreateRandomUserReplays((levelName, 800));
        await CreateRandomUserReplays((levelName, 900));
        await CreateReplays(session, (levelName, 1000));
        await CreateRandomUserReplays((levelName, 1100));
        await CreateRandomUserReplays((levelName, 1200));
        await CreateRandomUserReplays((levelName, 1300));
        await CreateRandomUserReplays((levelName, 1400));
        await CreateRandomUserReplays((levelName, 1500));
        await CreateRandomUserReplays((levelName, 1600));
        await CreateRandomUserReplays((levelName, 1700));
        await CreateRandomUserReplays((levelName, 1800));
        await CreateRandomUserReplays((levelName, 1900));
        await CreateRandomUserReplays((levelName, 2000));

        var client = applicationFactory.CreateClient();

        var list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{session}/leaderboard/neighbours?levelName={levelName}&take=10&skip=0");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(10);

        list.Items.ShouldAllBe(i => i.LevelName == levelName);
        list.Items
            .Select(i => i.PlayerId)
            .Distinct()
            .Count()
            .ShouldBe(10, "All scores should belong to unique players");

        list.Items[0].TimeInMilliseconds.ShouldBe(500u);
        list.Items[1].TimeInMilliseconds.ShouldBe(600u);
        list.Items[2].TimeInMilliseconds.ShouldBe(700u);
        list.Items[3].TimeInMilliseconds.ShouldBe(800u);
        list.Items[4].TimeInMilliseconds.ShouldBe(900u);

        list.Items[5].TimeInMilliseconds.ShouldBe(1000u);
        list.Items[5].PlayerName.ShouldBe(user.UserName);

        list.Items[6].TimeInMilliseconds.ShouldBe(1100u);
        list.Items[7].TimeInMilliseconds.ShouldBe(1200u);
        list.Items[8].TimeInMilliseconds.ShouldBe(1300u);
        list.Items[9].TimeInMilliseconds.ShouldBe(1400u);
    }

    [Fact]
    public async Task GetLeaderboardNeighbours_returns_padded_result_with_slower_times()
    {
        var user = ItchUserBuilder.BuildRandom();
        var session = await SessionBuilder.ForUser(applicationFactory, user);

        var levelName = "glnt_ps_level_one";

        await CreateRandomUserReplays((levelName, 900));
        await CreateReplays(session, (levelName, 1000));
        await CreateRandomUserReplays((levelName, 1100));
        await CreateRandomUserReplays((levelName, 1200));
        await CreateRandomUserReplays((levelName, 1300));
        await CreateRandomUserReplays((levelName, 1400));
        await CreateRandomUserReplays((levelName, 1500));
        await CreateRandomUserReplays((levelName, 1600));
        await CreateRandomUserReplays((levelName, 1700));
        await CreateRandomUserReplays((levelName, 1800));
        await CreateRandomUserReplays((levelName, 1900));
        await CreateRandomUserReplays((levelName, 2000));

        var client = applicationFactory.CreateClient();

        var list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{session}/leaderboard/neighbours?levelName={levelName}&take=10&skip=0");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(10);

        list.Items.ShouldAllBe(i => i.LevelName == levelName);
        list.Items
            .Select(i => i.PlayerId)
            .Distinct()
            .Count()
            .ShouldBe(10, "All scores should belong to unique players");

        list.Items[0].TimeInMilliseconds.ShouldBe(900u);

        list.Items[1].TimeInMilliseconds.ShouldBe(1000u);
        list.Items[1].PlayerName.ShouldBe(user.UserName);

        list.Items[2].TimeInMilliseconds.ShouldBe(1100u);
        list.Items[3].TimeInMilliseconds.ShouldBe(1200u);
        list.Items[4].TimeInMilliseconds.ShouldBe(1300u);
        list.Items[5].TimeInMilliseconds.ShouldBe(1400u);
        list.Items[6].TimeInMilliseconds.ShouldBe(1500u);
        list.Items[7].TimeInMilliseconds.ShouldBe(1600u);
        list.Items[8].TimeInMilliseconds.ShouldBe(1700u);
        list.Items[9].TimeInMilliseconds.ShouldBe(1800u);
    }

    [Fact]
    public async Task GetLeaderboardNeighbours_returns_padded_result_with_faster_times()
    {
        var user = ItchUserBuilder.BuildRandom();
        var session = await SessionBuilder.ForUser(applicationFactory, user);

        var levelName = "glnt_pf_level_one";

        await CreateRandomUserReplays((levelName, 0));
        await CreateRandomUserReplays((levelName, 100));
        await CreateRandomUserReplays((levelName, 200));
        await CreateRandomUserReplays((levelName, 300));
        await CreateRandomUserReplays((levelName, 400));
        await CreateRandomUserReplays((levelName, 500));
        await CreateRandomUserReplays((levelName, 600));
        await CreateRandomUserReplays((levelName, 700));
        await CreateRandomUserReplays((levelName, 800));
        await CreateRandomUserReplays((levelName, 900));
        await CreateReplays(session, (levelName, 1000));
        await CreateRandomUserReplays((levelName, 1100));

        var client = applicationFactory.CreateClient();

        var list = await client
            .GetFromJsonAsync<LeaderboardListResponse>($"/api/game/{gameRevision}/session/{session}/leaderboard/neighbours?levelName={levelName}&take=10&skip=0");
        list.ShouldNotBeNull();
        list.Items.ShouldNotBeNull();
        list.Items.Length.ShouldBe(10);

        list.Items.ShouldAllBe(i => i.LevelName == levelName);
        list.Items
            .Select(i => i.PlayerId)
            .Distinct()
            .Count()
            .ShouldBe(10, "All scores should belong to unique players");

        list.Items[0].TimeInMilliseconds.ShouldBe(200u);
        list.Items[1].TimeInMilliseconds.ShouldBe(300u);
        list.Items[2].TimeInMilliseconds.ShouldBe(400u);
        list.Items[3].TimeInMilliseconds.ShouldBe(500u);
        list.Items[4].TimeInMilliseconds.ShouldBe(600u);
        list.Items[5].TimeInMilliseconds.ShouldBe(700u);
        list.Items[6].TimeInMilliseconds.ShouldBe(800u);
        list.Items[7].TimeInMilliseconds.ShouldBe(900u);

        list.Items[8].PlayerName.ShouldBe(user.UserName);
        list.Items[8].TimeInMilliseconds.ShouldBe(1000u);

        list.Items[9].TimeInMilliseconds.ShouldBe(1100u);
    }

    private async Task CreateRandomUserReplays(params (string levelName, int timeInMilliseconds)[] replays)
    {
        var sessionSecret = await SessionBuilder.ForRandomUser(applicationFactory);
        foreach (var replay in replays)
        {
            await new ReplayBuilder(applicationFactory)
                .ForSession(sessionSecret)
                .ForLevel(replay.levelName)
                .WithTime(replay.timeInMilliseconds)
                .WithRevision(gameRevision)
                .Build();
        }
    }

    private async Task CreateReplays(Guid sessionSecret, params (string levelName, int timeInMilliseconds)[] replays)
    {
        foreach (var replay in replays)
        {
            await new ReplayBuilder(applicationFactory)
                .ForSession(sessionSecret)
                .ForLevel(replay.levelName)
                .WithTime(replay.timeInMilliseconds)
                .WithRevision(gameRevision)
                .Build();
        }
    }
}