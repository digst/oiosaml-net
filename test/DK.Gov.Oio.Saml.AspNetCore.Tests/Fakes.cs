using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = [];

    public bool IsAvailable => true;
    public string Id => "session";
    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _values.Remove(key);
    public void Set(string key, byte[] value) => _values[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
}

public sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
