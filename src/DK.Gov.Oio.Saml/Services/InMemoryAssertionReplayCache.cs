using System;
using System.Collections.Concurrent;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.Services;

public sealed class InMemoryAssertionReplayCache(TimeProvider timeProvider) : IAssertionReplayCache
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _accepted = new();

    public bool TryAdd(string assertionId, DateTimeOffset expiresAt)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in _accepted)
            if (entry.Value <= now) _accepted.TryRemove(entry.Key, out _);
        return _accepted.TryAdd(assertionId, expiresAt);
    }
}
