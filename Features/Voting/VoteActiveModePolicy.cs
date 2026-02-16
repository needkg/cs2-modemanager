using System;

namespace ModeManager;

internal sealed class VoteActiveModePolicy
{
    public bool TryAllowVoteForMode(
        VoteSession? activeVote,
        ModeDefinition requestedMode,
        Func<MessageKey, object?[], string> msg,
        Action<string> reply)
    {
        if (activeVote == null)
            return true;

        if (activeVote.ModeKey.Equals(requestedMode.Key, StringComparison.OrdinalIgnoreCase))
            return true;

        var remaining = VoteSessionStore.RemainingSeconds(activeVote);
        var missingVotes = VoteSessionStore.MissingVotes(activeVote);

        reply(
            msg(
                MessageKey.VoteAnotherModeInProgress,
                new object?[]
                {
                    activeVote.ModeDisplayName,
                    activeVote.TargetMap,
                    activeVote.VoterIds.Count,
                    activeVote.RequiredVotes,
                    missingVotes,
                    remaining
                }));

        return false;
    }
}
