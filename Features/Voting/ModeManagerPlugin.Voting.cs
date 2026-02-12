using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private VoteCoordinator? _voteCoordinator;

    private VoteCoordinator Votes => _voteCoordinator ??= new VoteCoordinator(
        (key, args) => Msg(key, args),
        (key, args) => ChatTone(key, args),
        ScheduleModeSwitch);

    private void HandleVote(CCSPlayerController? player, CommandInfo cmd, ModeDefinition mode)
    {
        Votes.HandleVoteRequest(
            player,
            mode,
            requestedMap: null,
            hasExplicitMapSelection: false,
            config: Config,
            activeModeKey: _activeModeKey,
            pendingSwitch: _pending,
            cooldownUntilUtc: _cooldownUntilUtc,
            msg => ReplyTone(cmd, msg));
    }

    private void HandleVoteFromMenu(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!player.IsValid)
            return;

        Votes.HandleVoteRequest(
            player,
            mode,
            requestedMap: selectedMap,
            hasExplicitMapSelection: true,
            config: Config,
            activeModeKey: _activeModeKey,
            pendingSwitch: _pending,
            cooldownUntilUtc: _cooldownUntilUtc,
            msg => TellTone(player, msg));
    }

    private IReadOnlyList<string> GetSelectableMapsForMode(ModeDefinition mode) =>
        Votes.GetSelectableMapsForMode(mode);

    private string ResolveTargetMapForMode(ModeDefinition mode)
    {
        return Votes.ResolveTargetMapForMode(mode);
    }

    private bool IsModeAlreadyActive(ModeDefinition mode) =>
        !string.IsNullOrWhiteSpace(_activeModeKey) &&
        _activeModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase);

    private void ResetVotes() => Votes.Reset();
}
