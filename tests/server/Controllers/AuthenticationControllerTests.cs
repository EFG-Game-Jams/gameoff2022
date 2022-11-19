using Shouldly;
using Xunit;

namespace Game.Server.Integration.Tests.Controllers;

[Collection("Default")]
public class AuthenticationControllerTests
{
    private readonly DefaultWebApplicationFactory applicationFactory;

    public AuthenticationControllerTests(DefaultWebApplicationFactory applicationFactory)
    {
        this.applicationFactory = applicationFactory;
    }

    [Fact]
    public async Task Login_returns_expected_url()
    {
        var result = await applicationFactory.CreateClient().GetAsync("/login");
        result.StatusCode.ShouldBe(System.Net.HttpStatusCode.Found);

        var location = result.Headers.Single(h => h.Key == "Location");
        location.Value.Single().ShouldBe("https://example.com/login");
    }
}