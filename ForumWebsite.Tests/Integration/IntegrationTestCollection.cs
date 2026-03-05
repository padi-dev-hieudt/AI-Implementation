namespace ForumWebsite.Tests.Integration;

/// <summary>
/// Groups all integration test classes into a single xUnit collection so that:
/// 1. They share one <see cref="ForumWebApplicationFactory"/> instance (one host, one DB).
/// 2. They run sequentially, preventing SQLite contention that occurs when two hosts
///    try to start concurrently during parallel test collection execution.
///
/// Usage: decorate every integration test class with
///   [Collection(Name)]
/// and inject the shared factory via constructor (not IClassFixture — that's handled
/// here via ICollectionFixture).
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<ForumWebApplicationFactory>
{
    public const string Name = "Integration";
    // No code — xUnit uses this class only as a marker for the collection.
}
