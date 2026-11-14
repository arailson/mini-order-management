using System.Text.Json;

namespace IntegrationTests.Infrastructure;

[Collection("Api")]
[Trait("Category", "Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly SqlServerFixture _fixture;

    protected IntegrationTestBase(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    protected SqlServerFixture Fixture => _fixture;

    protected ApiFactory Factory => _fixture.Factory;

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await DatabaseReset.ResetAsync(_fixture);
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
    }

    protected static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
}
