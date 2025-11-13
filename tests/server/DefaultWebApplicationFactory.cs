using Game.Server.Integration.Tests.Mocks;
using Game.Server.Services.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Game.Server.Integration.Tests;

public class DefaultWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<IItchService, ItchServiceMock>();
        });

        builder.ConfigureServices(services =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            using var databaseContext = scope.ServiceProvider.GetRequiredService<ReplayDatabase>();

            databaseContext.Database.EnsureDeleted();
            databaseContext.Database.EnsureCreated();
        });
    }
}
