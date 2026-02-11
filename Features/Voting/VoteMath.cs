using System;

namespace ModeManager;

internal static class VoteMath
{
    public static int RequiredVotes(int eligiblePlayers, double ratio)
    {
        var needed = (int)Math.Ceiling(eligiblePlayers * ratio);
        return Math.Max(1, needed);
    }
}
