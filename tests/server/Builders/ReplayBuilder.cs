using Game.Server.Models.Replay;
using Shouldly;

namespace Game.Server.Integration.Tests.Builders;

internal class ReplayBuilder
{
    private readonly DefaultWebApplicationFactory applicationFactory;

    private Guid? sessionSecret;
    private string? levelName;
    private int? timeInMilliseconds;
    private int? gameRevision;

    public ReplayBuilder(DefaultWebApplicationFactory applicationFactory)
    {
        this.applicationFactory = applicationFactory;
    }

    public ReplayBuilder ForSession(Guid sessionSecret)
    {
        this.sessionSecret = sessionSecret;
        return this;
    }

    public ReplayBuilder ForLevel(string name)
    {
        levelName = name;
        return this;
    }

    public ReplayBuilder WithRevision(int revision)
    {
        gameRevision = revision;
        return this;
    }

    public ReplayBuilder WithTime(int milliseconds)
    {
        timeInMilliseconds = milliseconds;
        return this;
    }

    /// <returns>The ID of the created replay</returns>
    public async Task<int> Build()
    {
        if (
            sessionSecret == null
            || levelName == null
            || timeInMilliseconds == null
            || gameRevision == null
        )
        {
            throw new InvalidOperationException("Cannot construct partial replay");
        }

        var client = applicationFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/game/{gameRevision}/session/{sessionSecret}/replay",
            new CreateReplayRequest
            {
                TimeInMilliseconds = timeInMilliseconds.Value,
                LevelName = levelName,
                Data = "{\"blerp\":420}",
            }
        );

        var createdReplay = await response.Content.ReadFromJsonAsync<ReplayCreatedResponse>();
        createdReplay.ShouldNotBeNull();
        createdReplay.ReplayId.ShouldNotBe(0);

        return createdReplay.ReplayId;
    }
}
