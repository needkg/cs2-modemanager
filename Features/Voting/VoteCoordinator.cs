using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class VoteCoordinator
{
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<MessageKey, object?[]> _chat;
    private readonly Action<ModeDefinition, string, string?> _scheduleModeSwitch;
    private readonly VoteTargetMapResolver _targetMapResolver = new();
    private readonly VoteEligibilityPolicy _eligibilityPolicy = new();
    private readonly VoteActiveModePolicy _activeModePolicy = new();
    private readonly VoteQuorumPolicy _quorumPolicy = new();
    private readonly VoteSessionRegistrar _sessionRegistrar = new();
    private readonly VoteSessionStore _sessionStore;

    public VoteCoordinator(
        Func<MessageKey, object?[], string> msg,
        Action<MessageKey, object?[]> chat,
        Action<ModeDefinition, string, string?> scheduleModeSwitch)
    {
        _msg = msg;
        _chat = chat;
        _scheduleModeSwitch = scheduleModeSwitch;
        _sessionStore = new VoteSessionStore(msg, chat);
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
                _targetMapResolver.TryResolveTargetMap(
                    mode,
                    requestedMap,
                    hasExplicitMapSelection: true,
                    out var selectedMap) &&
                !_targetMapResolver.IsCurrentMapForTarget(selectedMap))
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
            HandleConsoleVoteRequest(mode, requestedMap, hasExplicitMapSelection, reply);
            return;
        }

        if (!_eligibilityPolicy.TryResolveVoterIdentity(player, pendingSwitch, cooldownUntilUtc, Msg, reply, out var voterId))
            return;

        _sessionStore.CleanupExpiredVoteIfNeeded();

        var vote = _sessionStore.Current;

        if (!_activeModePolicy.TryAllowVoteForMode(vote, mode, Msg, reply))
            return;

        if (!_targetMapResolver.TryResolveTargetMap(mode, requestedMap, hasExplicitMapSelection, out var resolvedMap))
        {
            reply(Msg(MessageKey.VoteMapSelectionInvalid));
            return;
        }

        if (!_quorumPolicy.TryResolveRequiredVotes(config, Msg, reply, out var requiredVotes))
            return;

        var registration = _sessionRegistrar.RegisterVote(
            currentVote: vote,
            mode: mode,
            voterId: voterId,
            targetMap: resolvedMap,
            hasExplicitMapSelection: hasExplicitMapSelection,
            requiredVotes: requiredVotes,
            voteDurationSeconds: config.VoteDurationSeconds);

        if (registration.Status == VoteRegistrationStatus.Started)
        {
            _sessionStore.Set(registration.Vote);

            var dynamicAlias = CommandNameSanitizer.ToSafeToken(mode.Key);
            Chat(
                MessageKey.VoteStartedChat,
                mode.DisplayName,
                registration.Vote.TargetMap,
                registration.Vote.VoterIds.Count,
                registration.Vote.RequiredVotes,
                registration.MissingVotes,
                registration.RemainingSeconds,
                dynamicAlias);

            reply(
                Msg(
                    MessageKey.VoteRegisteredSelf,
                    mode.DisplayName,
                    registration.Vote.TargetMap,
                    registration.MissingVotes,
                    registration.RemainingSeconds));

            TryFinalizeVoteIfReached(mode, registration.Vote);
            return;
        }

        if (registration.Status == VoteRegistrationStatus.AlreadyVoted)
        {
            reply(Msg(MessageKey.VoteAlreadyCast));
            return;
        }

        Chat(
            MessageKey.VoteRegisteredChat,
            registration.Vote.ModeDisplayName,
            registration.Vote.TargetMap,
            registration.Vote.VoterIds.Count,
            registration.Vote.RequiredVotes,
            registration.MissingVotes,
            registration.RemainingSeconds);

        TryFinalizeVoteIfReached(mode, registration.Vote);
    }

    public IReadOnlyList<string> GetSelectableMapsForMode(ModeDefinition mode)
        => _targetMapResolver.GetSelectableMapsForMode(mode);

    public bool TryResolveTargetMapForMode(
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        out string targetMap) =>
        _targetMapResolver.TryResolveTargetMap(mode, requestedMap, hasExplicitMapSelection, out targetMap);

    public void Reset() => _sessionStore.Reset();

    public void CleanupExpiredVote() => _sessionStore.CleanupExpiredVoteIfNeeded();

    public bool TryReplyActiveVoteStatusForVoter(CCSPlayerController player, Action<string> reply)
        => _sessionStore.TryReplyActiveVoteStatusForVoter(player, reply);

    public bool TryGetActiveVoteMode(ModeManagerConfig config, out ModeDefinition mode)
        => _sessionStore.TryGetActiveVoteMode(config, out mode);

    public bool IsCurrentMapForTarget(string map) => _targetMapResolver.IsCurrentMapForTarget(map);

    private static bool IsModeAlreadyActive(string? activeModeKey, ModeDefinition mode) =>
        !string.IsNullOrWhiteSpace(activeModeKey) &&
        activeModeKey.Equals(mode.Key, StringComparison.OrdinalIgnoreCase);

    private void HandleConsoleVoteRequest(
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        Action<string> reply)
    {
        if (!_targetMapResolver.TryResolveTargetMap(mode, requestedMap, hasExplicitMapSelection, out var mapForSwitch))
        {
            reply(Msg(MessageKey.VoteMapSelectionInvalid));
            return;
        }

        _scheduleModeSwitch(mode, "console", mapForSwitch);
        reply(Msg(MessageKey.VoteConsoleScheduled, mode.DisplayName, mapForSwitch));
    }

    private void TryFinalizeVoteIfReached(ModeDefinition mode, VoteSession vote)
    {
        if (vote.VoterIds.Count < vote.RequiredVotes)
            return;

        _sessionStore.Clear();
        _scheduleModeSwitch(mode, "vote", vote.TargetMap);
    }

    private string Msg(MessageKey key, params object?[] args) => _msg(key, args);

    private void Chat(MessageKey key, params object?[] args) => _chat(key, args);
}
