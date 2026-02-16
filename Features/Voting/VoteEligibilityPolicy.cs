using System;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class VoteEligibilityPolicy
{
    public bool TryResolveVoterIdentity(
        CCSPlayerController? player,
        PendingSwitch? pendingSwitch,
        DateTime cooldownUntilUtc,
        Func<MessageKey, object?[], string> msg,
        Action<string> reply,
        out string voterId)
    {
        voterId = string.Empty;

        if (player == null || !player.IsValid)
        {
            reply(msg(MessageKey.ErrorInvalidPlayer, Array.Empty<object?>()));
            return false;
        }

        if (pendingSwitch != null)
        {
            reply(msg(MessageKey.VotePendingAlready, new object?[] { pendingSwitch.Mode.DisplayName }));
            return false;
        }

        if (DateTime.UtcNow < cooldownUntilUtc)
        {
            var seconds = (int)Math.Ceiling((cooldownUntilUtc - DateTime.UtcNow).TotalSeconds);
            reply(msg(MessageKey.VoteCooldown, new object?[] { seconds }));
            return false;
        }

        if (PlayerEligibility.IsIneligible(player))
        {
            reply(msg(MessageKey.VoteIneligible, Array.Empty<object?>()));
            return false;
        }

        var resolvedId = PlayerIdResolver.GetStableId(player);
        if (resolvedId == null)
        {
            reply(msg(MessageKey.VoteIdentityMissing, Array.Empty<object?>()));
            return false;
        }

        voterId = resolvedId;
        return true;
    }
}
