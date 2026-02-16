using System;

namespace ModeManager;

internal sealed class VoteQuorumPolicy
{
    public bool TryResolveRequiredVotes(
        ModeManagerConfig config,
        Func<MessageKey, object?[], string> msg,
        Action<string> reply,
        out int requiredVotes)
    {
        requiredVotes = 0;

        var eligiblePlayers = PlayerEligibility.CountEligiblePlayers();
        if (eligiblePlayers < config.VoteMinPlayers)
        {
            reply(msg(MessageKey.VoteMinPlayers, new object?[] { config.VoteMinPlayers, eligiblePlayers }));
            return false;
        }

        requiredVotes = VoteMath.RequiredVotes(eligiblePlayers, config.VoteRatio);
        return true;
    }
}
