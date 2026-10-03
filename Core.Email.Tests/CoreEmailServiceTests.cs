using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Email.Tests;

public class CoreEmailServiceTests
{
    private readonly FakeProvider _a = new();
    private readonly FakeProvider _b = new();
    private readonly ICoreEmail _email;

    public CoreEmailServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Default"] = "A" })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddCoreEmail();
        services.AddKeyedSingleton<ICoreEmailProvider>("A", _a);
        services.AddKeyedSingleton<ICoreEmailProvider>("B", _b);
        _email = services.BuildServiceProvider().GetRequiredService<ICoreEmail>();
    }

    [Fact]
    public async Task SingleMessageUsesDefaultProvider()
    {
        await _email.SendAsync(new CoreEmailMessage());

        Assert.Single(_a.Batches);
        Assert.Empty(_b.Batches);
    }

    [Fact]
    public async Task SingleMessageUsesProviderKey()
    {
        await _email.SendAsync(new CoreEmailMessage { ProviderKey = "B" });

        Assert.Empty(_a.Batches);
        Assert.Single(_b.Batches);
    }

    [Fact]
    public async Task BatchIsGroupedByProviderKey()
    {
        var res = await _email.SendAsync([
            new CoreEmailMessage(),
            new CoreEmailMessage { ProviderKey = "B" },
            new CoreEmailMessage()
        ]);

        Assert.Equal(3, res.Count);
        Assert.Equal(2, Assert.Single(_a.Batches).Count);
        Assert.Single(Assert.Single(_b.Batches));
    }

    [Fact]
    public async Task UnknownProviderFailsBeforeAnythingIsSent()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _email.SendAsync([
            new CoreEmailMessage(),
            new CoreEmailMessage { ProviderKey = "Missing" }
        ]));

        Assert.Empty(_a.Batches);
    }

    private class FakeProvider : ICoreEmailProvider
    {
        public List<List<CoreEmailMessage>> Batches { get; } = new();

        public string Name => "Fake";
        public long MaxSize => 0;

        public Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
            CancellationToken cancellationToken = default)
        {
            Batches.Add(messages);
            return Task.FromResult(messages.Select(x => new CoreEmailStatus { Id = x.Id, IsSuccess = true }).ToList());
        }
    }
}