using System;
using System.Linq;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class VoteSessionStore
{
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<MessageKey, object?[]> _chat;

    private VoteSession? _vote;

    public VoteSessionStore(
        Func<MessageKey, object?[], string> msg,
        Action<MessageKey, object?[]> chat)
    {
        _msg = msg;
        _chat = chat;
    }

    public VoteSession? Current => _vote;

    public void Set(VoteSession vote) => _vote = vote;

    public void Clear() => _vote = null;

    public void Reset() => _vote = null;

    public void CleanupExpiredVoteIfNeeded()
    {
        var vote = _vote;
        if (vote == null)
            return;

        if (DateTime.UtcNow >= vote.ExpiresUtc)
        {
            var missingVotes = MissingVotes(vote);
            _chat(MessageKey.VoteExpiredChat, new object?[]
            {
                vote.ModeDisplayName,
                vote.TargetMap,
                vote.VoterIds.Count,
                vote.RequiredVotes,
                missingVotes
            });

            _vote = null;
        }
    }

    public bool TryReplyActiveVoteStatusForVoter(CCSPlayerController player, Action<string> reply)
    {
        if (player == null || !player.IsValid)
            return false;

        CleanupExpiredVoteIfNeeded();

        var vote = _vote;
        if (vote == null)
            return false;

        var voterId = PlayerIdResolver.GetStableId(player);
        if (voterId == null || !vote.VoterIds.Contains(voterId))
            return false;

        var remaining = RemainingSeconds(vote);
        var missingVotes = MissingVotes(vote);

        reply(_msg(MessageKey.VoteStatusAlreadyVoted, new object?[]
        {
            vote.ModeDisplayName,
            vote.TargetMap,
            vote.VoterIds.Count,
            vote.RequiredVotes,
            missingVotes,
            remaining
        }));

        return true;
    }

    public bool TryGetActiveVoteMode(ModeManagerConfig config, out ModeDefinition mode)
    {
        mode = null!;

        CleanupExpiredVoteIfNeeded();

        var vote = _vote;
        if (vote == null)
            return false;

        if (config.Modes.TryGetValue(vote.ModeKey, out var directMode) && directMode != null)
        {
            mode = directMode;
            return true;
        }

        var fallbackMode = config.Modes
            .FirstOrDefault(entry => entry.Value != null &&
                                     entry.Key.Equals(vote.ModeKey, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (fallbackMode == null)
        {
            _vote = null;
            return false;
        }

        mode = fallbackMode;
        return true;
    }

    public static int RemainingSeconds(VoteSession vote) =>
        (int)Math.Max(0, (vote.ExpiresUtc - DateTime.UtcNow).TotalSeconds);

    public static int MissingVotes(VoteSession vote) =>
        Math.Max(0, vote.RequiredVotes - vote.VoterIds.Count);
}
