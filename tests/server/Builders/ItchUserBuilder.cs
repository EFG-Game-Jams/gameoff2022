using Bogus;
using Game.Server.Integration.Tests.Mocks;

namespace Game.Server.Integration.Tests.Builders;

internal class ItchUserBuilder
{
    private static int idCounter = 1;

    private readonly Faker faker = new("en");

    private int userId = 0;
    private string userName = string.Empty;
    private string accessToken = string.Empty;

    private ItchUserBuilder()
    {
    }

    public ItchUserBuilder WithId()
    {
        userId = idCounter++;
        return this;
    }

    public ItchUserBuilder WithName()
    {
        userName = faker.Internet.UserName();
        return this;
    }

    public ItchUserBuilder WithAccessToken()
    {
        accessToken = Guid.NewGuid()
            .ToString()
            .Replace("-", string.Empty)
            .ToLower();
        return this;
    }

    public MockItchUser Build()
    {
        var user = new MockItchUser(userId, userName, accessToken);
        ItchServiceMock.MockUsers.Add(user);

        return user;
    }

    public static MockItchUser BuildRandom()
    {
        return new ItchUserBuilder()
            .WithId()
            .WithName()
            .WithAccessToken()
            .Build();
    }

    public static MockItchUser Build(string name)
    {
        var builer = new ItchUserBuilder()
            .WithId()
            .WithAccessToken();

        builer.userName = name;

        return builer.Build();
    }
}