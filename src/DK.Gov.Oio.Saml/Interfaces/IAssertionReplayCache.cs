using System;

namespace DK.Gov.Oio.Saml.Interfaces;

public interface IAssertionReplayCache
{
    /// <returns>false when <paramref name="assertionId"/> has already been accepted.</returns>
    bool TryAdd(string assertionId, DateTimeOffset expiresAt);
}
