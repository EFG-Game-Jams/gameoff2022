using Xunit;

namespace Game.Server.Integration.Tests;

[CollectionDefinition("Default")]
public class DefaultCollectionDefinition : ICollectionFixture<DefaultWebApplicationFactory> { }
