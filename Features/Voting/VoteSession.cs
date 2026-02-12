using System;
using System.Collections.Generic;

namespace ModeManager;

internal sealed class VoteSession
{
    public VoteSession(string modeKey, string modeDisplayName, string targetMap, int requiredVotes, DateTime expiresUtc)
    {
        ModeKey = modeKey;
        ModeDisplayName = modeDisplayName;
        TargetMap = targetMap;
        RequiredVotes = requiredVotes;
        ExpiresUtc = expiresUtc;
    }

    public string ModeKey { get; }
    public string ModeDisplayName { get; }
    public string TargetMap { get; }
    public int RequiredVotes { get; }
    public DateTime ExpiresUtc { get; }
    public HashSet<string> VoterIds { get; } = new(StringComparer.Ordinal);
}
