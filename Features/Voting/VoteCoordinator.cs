using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class VoteCoordinator
{
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<MessageKey, object?[]> _chat;
    private readonly Action<ModeDefinition, string, string?> _scheduleModeSwitch;

    private VoteSession? _vote;

    public VoteCoordinator(
        Func<MessageKey, object?[], string> msg,
        Action<MessageKey, object?[]> chat,
        Action<ModeDefinition, string, string?> scheduleModeSwitch)
    {
        _msg = msg;
        _chat = chat;
        _scheduleModeSwitch = scheduleModeSwitch;
    }

    public void HandleVoteRequest(
        CCSPlayerController? player,
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        ModeManagerConfig config,
        string? activeModeKey,
        PendingSwitch? pendingSwitch,
        DateTime cooldownUntilUtc,
        Action<string> reply)
    {
        if (IsModeAlreadyActive(activeModeKey, mode))
        {
            if (hasExplicitMapSelection &&
                TryResolveVoteTargetMap(mode, requestedMap, hasExplicitMapSelection: true, out var selectedMap) &&
                !IsCurrentMap(selectedMap))
            {
                // Same mode is already active, but player picked a different map.
                // Allow vote flow to continue as a map change request.
            }
            else
            {
                reply(Msg(MessageKey.VoteAlreadyActiveMode, mode.DisplayName));
                return;
            }
        }

        if (player == null)
        {
            if (!TryResolveVoteTargetMap(mode, requestedMap, hasExplicitMapSelection, out var mapForSwitch))
            {
                reply(Msg(MessageKey.VoteMapSelectionInvalid));
                return;
            }

            _scheduleModeSwitch(mode, "console", mapForSwitch);
            reply(Msg(MessageKey.VoteConsoleScheduled, mode.DisplayName, mapForSwitch));
            return;
        }

        if (!player.IsValid)
        {
            reply(Msg(MessageKey.ErrorInvalidPlayer));
            return;
        }

        if (pendingSwitch != null)
        {
            reply(Msg(MessageKey.VotePendingAlready, pendingSwitch.Mode.DisplayName));
            return;
        }

        if (DateTime.UtcNow < cooldownUntilUtc)
        {
            var seconds = (int)Math.Ceiling((cooldownUntilUtc - DateTime.UtcNow).TotalSeconds);
            reply(Msg(MessageKey.VoteCooldown, seconds));
            return;
        }

        if (PlayerEligibility.IsIneligible(player))
        {
            reply(Msg(MessageKey.VoteIneligible));
            return;
        }

        var voterId = PlayerIdResolver.GetStableId(player);
        if (voterId == null)
        {
            reply(Msg(MessageKey.VoteIdentityMissing));
            return;
        }

        CleanupExpiredVoteIfNeeded();

        var vote = _vote;

        if (vote != null && !vote.ModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase))
        {
            var remaining = RemainingSeconds(vote);
            var missingVotes = MissingVotes(vote);
            reply(
                Msg(
                    MessageKey.VoteAnotherModeInProgress,
                    vote.ModeDisplayName,
                    vote.TargetMap,
                    vote.VoterIds.Count,
                    vote.RequiredVotes,
                    missingVotes,
                    remaining));
            return;
        }

        if (!TryResolveVoteTargetMap(mode, requestedMap, hasExplicitMapSelection, out var resolvedMap))
        {
            reply(Msg(MessageKey.VoteMapSelectionInvalid));
            return;
        }

        if (vote != null)
        {
            if (hasExplicitMapSelection)
                vote.TargetMap = resolvedMap;

            resolvedMap = vote.TargetMap;
        }

        var eligiblePlayers = PlayerEligibility.CountEligiblePlayers();
        if (eligiblePlayers < config.VoteMinPlayers)
        {
            reply(Msg(MessageKey.VoteMinPlayers, config.VoteMinPlayers, eligiblePlayers));
            return;
        }

        var requiredVotes = VoteMath.RequiredVotes(eligiblePlayers, config.VoteRatio);

        if (vote == null)
        {
            vote = new VoteSession(
                modeKey: mode.Key,
                modeDisplayName: mode.DisplayName,
                targetMap: resolvedMap,
                requiredVotes: requiredVotes,
                expiresUtc: DateTime.UtcNow.AddSeconds(config.VoteDurationSeconds));

            vote.VoterIds.Add(voterId);
            _vote = vote;

            var dynamicAlias = CommandNameSanitizer.ToSafeToken(mode.Key);
            var remaining = RemainingSeconds(vote);
            var missingVotes = MissingVotes(vote);

            Chat(
                MessageKey.VoteStartedChat,
                mode.DisplayName,
                vote.TargetMap,
                vote.VoterIds.Count,
                vote.RequiredVotes,
                missingVotes,
                remaining,
                dynamicAlias);

            reply(Msg(MessageKey.VoteRegisteredSelf, mode.DisplayName, vote.TargetMap, missingVotes, remaining));
            TryFinalizeVoteIfReached(mode, vote);
            return;
        }

        if (vote.VoterIds.Contains(voterId))
        {
            reply(Msg(MessageKey.VoteAlreadyCast));
            return;
        }

        vote.VoterIds.Add(voterId);

        var remainingForChat = RemainingSeconds(vote);
        var missingVotesForChat = MissingVotes(vote);

        Chat(
            MessageKey.VoteRegisteredChat,
            vote.ModeDisplayName,
            vote.TargetMap,
            vote.VoterIds.Count,
            vote.RequiredVotes,
            missingVotesForChat,
            remainingForChat);

        TryFinalizeVoteIfReached(mode, vote);
    }

    public IReadOnlyList<string> GetSelectableMapsForMode(ModeDefinition mode)
    {
        var maps = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawMap in mode.MapPool)
        {
            var map = NormalizeMapName(rawMap);
            if (map == null)
                continue;

            if (seen.Add(map))
                maps.Add(map);
        }

        if (maps.Count > 0)
            return maps;

        var fallback = NormalizeMapName(ModeSwitcher.ResolveTargetMap(mode));
        if (fallback != null)
            maps.Add(fallback);

        return maps;
    }

    public bool TryResolveTargetMapForMode(
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        out string targetMap) =>
        TryResolveVoteTargetMap(mode, requestedMap, hasExplicitMapSelection, out targetMap);

    public void Reset() => _vote = null;

    public void CleanupExpiredVote() => CleanupExpiredVoteIfNeeded();

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

        reply(
            Msg(
                MessageKey.VoteStatusAlreadyVoted,
                vote.ModeDisplayName,
                vote.TargetMap,
                vote.VoterIds.Count,
                vote.RequiredVotes,
                missingVotes,
                remaining));

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

    public bool IsCurrentMapForTarget(string map) => IsCurrentMap(map);

    private static bool IsModeAlreadyActive(string? activeModeKey, ModeDefinition mode) =>
        !string.IsNullOrWhiteSpace(activeModeKey) &&
        activeModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase);

    private static bool IsCurrentMap(string map)
    {
        var currentMap = NormalizeMapName(MapResolver.TryGetCurrentMapName());
        var normalizedTargetMap = NormalizeMapName(map);

        return currentMap != null &&
               normalizedTargetMap != null &&
               currentMap.Equals(normalizedTargetMap, StringComparison.OrdinalIgnoreCase);
    }

    private void TryFinalizeVoteIfReached(ModeDefinition mode, VoteSession vote)
    {
        if (vote.VoterIds.Count < vote.RequiredVotes)
            return;

        _vote = null;
        _scheduleModeSwitch(mode, "vote", vote.TargetMap);
    }

    private void CleanupExpiredVoteIfNeeded()
    {
        var vote = _vote;
        if (vote == null)
            return;

        if (DateTime.UtcNow >= vote.ExpiresUtc)
        {
            var missingVotes = MissingVotes(vote);
            Chat(
                MessageKey.VoteExpiredChat,
                vote.ModeDisplayName,
                vote.TargetMap,
                vote.VoterIds.Count,
                vote.RequiredVotes,
                missingVotes);
            _vote = null;
        }
    }

    private bool TryResolveVoteTargetMap(
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        out string targetMap)
    {
        var selectableMaps = GetSelectableMapsForMode(mode);
        if (selectableMaps.Count == 0)
        {
            targetMap = string.Empty;
            return false;
        }

        var normalizedRequestedMap = NormalizeMapName(requestedMap);
        if (hasExplicitMapSelection)
        {
            if (normalizedRequestedMap == null)
            {
                targetMap = string.Empty;
                return false;
            }

            var selected = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedRequestedMap, StringComparison.OrdinalIgnoreCase));

            if (selected == null)
            {
                targetMap = string.Empty;
                return false;
            }

            targetMap = selected;
            return true;
        }

        if (normalizedRequestedMap != null)
        {
            var requestedInPool = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedRequestedMap, StringComparison.OrdinalIgnoreCase));
            if (requestedInPool != null)
            {
                targetMap = requestedInPool;
                return true;
            }
        }

        targetMap = GetPreferredMapForMode(mode, selectableMaps);
        return true;
    }

    private static string GetPreferredMapForMode(ModeDefinition mode, IReadOnlyList<string> selectableMaps)
    {
        var normalizedDefaultMap = NormalizeMapName(mode.DefaultMap);
        if (normalizedDefaultMap != null)
        {
            var defaultMapInPool = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedDefaultMap, StringComparison.OrdinalIgnoreCase));
            if (defaultMapInPool != null)
                return defaultMapInPool;
        }

        return selectableMaps[0];
    }

    private static string? NormalizeMapName(string? map)
    {
        var trimmed = (map ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (trimmed.Contains(' ') || trimmed.Contains('"'))
            return null;

        return trimmed;
    }

    private static int RemainingSeconds(VoteSession vote) =>
        (int)Math.Max(0, (vote.ExpiresUtc - DateTime.UtcNow).TotalSeconds);

    private static int MissingVotes(VoteSession vote) =>
        Math.Max(0, vote.RequiredVotes - vote.VoterIds.Count);

    private string Msg(MessageKey key, params object?[] args) => _msg(key, args);

    private void Chat(MessageKey key, params object?[] args) => _chat(key, args);
}
