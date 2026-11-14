namespace IntegrationTests.Infrastructure;

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<SqlServerFixture>
{
}
