using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void HandleVote(CCSPlayerController? player, CommandInfo cmd, ModeDefinition mode)
    {
        if (IsModeAlreadyActive(mode))
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteAlreadyActiveMode, mode.DisplayName));
            return;
        }

        if (player == null)
        {
            ScheduleModeSwitch(mode, "console");
            cmd.ReplyToCommand(Msg(MessageKey.VoteConsoleScheduled, mode.DisplayName));
            return;
        }

        if (!player.IsValid)
        {
            cmd.ReplyToCommand(Msg(MessageKey.ErrorInvalidPlayer));
            return;
        }

        if (_pending != null)
        {
            cmd.ReplyToCommand(Msg(MessageKey.VotePendingAlready, _pending.Mode.DisplayName));
            return;
        }

        if (DateTime.UtcNow < _cooldownUntilUtc)
        {
            var seconds = (int)Math.Ceiling((_cooldownUntilUtc - DateTime.UtcNow).TotalSeconds);
            cmd.ReplyToCommand(Msg(MessageKey.VoteCooldown, seconds));
            return;
        }

        if (PlayerEligibility.IsIneligible(player))
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteIneligible));
            return;
        }

        var voterId = PlayerIdResolver.GetStableId(player);
        if (voterId == null)
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteIdentityMissing));
            return;
        }

        CleanupExpiredVoteIfNeeded();

        var eligiblePlayers = PlayerEligibility.CountEligiblePlayers();
        if (eligiblePlayers < Config.VoteMinPlayers)
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteMinPlayers, Config.VoteMinPlayers, eligiblePlayers));
            return;
        }

        var requiredVotes = VoteMath.RequiredVotes(eligiblePlayers, Config.VoteRatio);

        if (_vote == null || !_vote.ModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase))
        {
            _vote = new VoteSession(
                modeKey: mode.Key,
                requiredVotes: requiredVotes,
                expiresUtc: DateTime.UtcNow.AddSeconds(Config.VoteDurationSeconds));

            _vote.VoterIds.Add(voterId);

            ChatAll(Msg(MessageKey.VoteStartedChat, mode.DisplayName, requiredVotes, mode.Key));
            cmd.ReplyToCommand(Msg(MessageKey.VoteRegisteredSelf, mode.DisplayName, requiredVotes));

            if (_vote.VoterIds.Count >= _vote.RequiredVotes)
            {
                _vote = null;
                ScheduleModeSwitch(mode, "vote");
            }

            return;
        }

        if (_vote.VoterIds.Contains(voterId))
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteAlreadyCast));
            return;
        }

        _vote.VoterIds.Add(voterId);

        var remaining = (int)Math.Max(0, (_vote.ExpiresUtc - DateTime.UtcNow).TotalSeconds);
        ChatAll(Msg(MessageKey.VoteRegisteredChat, mode.DisplayName, _vote.VoterIds.Count, _vote.RequiredVotes, remaining));

        if (_vote.VoterIds.Count >= _vote.RequiredVotes)
        {
            _vote = null;
            ScheduleModeSwitch(mode, "vote");
        }
    }

    private void CleanupExpiredVoteIfNeeded()
    {
        if (_vote == null)
            return;

        if (DateTime.UtcNow >= _vote.ExpiresUtc)
        {
            ChatAll(Msg(MessageKey.VoteExpiredChat, _vote.ModeKey));
            _vote = null;
        }
    }

    private bool IsModeAlreadyActive(ModeDefinition mode) =>
        !string.IsNullOrWhiteSpace(_activeModeKey) &&
        _activeModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase);
}
