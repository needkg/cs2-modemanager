using System;
using System.Collections.Generic;

namespace ModeManager;

internal sealed class VoteSession
{
    public VoteSession(string modeKey, int requiredVotes, DateTime expiresUtc)
    {
        ModeKey = modeKey;
        RequiredVotes = requiredVotes;
        ExpiresUtc = expiresUtc;
    }

    public string ModeKey { get; }
    public int RequiredVotes { get; }
    public DateTime ExpiresUtc { get; }
    public HashSet<string> VoterIds { get; } = new(StringComparer.Ordinal);
}
