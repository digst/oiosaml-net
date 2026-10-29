using System;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace DK.Gov.Oio.Saml.Services;

public sealed class DistributedCacheAssertionReplayCache(IDistributedCache cache) : IAssertionReplayCache
{
    // Get-then-Set is not atomic; two responses replaying the same assertion within the same instant could both pass.
    public bool TryAdd(string assertionId, DateTimeOffset expiresAt)
    {
        var key = "oiosaml:assertion:" + assertionId;
        if (cache.Get(key) != null) return false;
        cache.Set(key, [1], new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt });
        return true;
    }
}
