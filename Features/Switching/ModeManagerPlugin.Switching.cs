using System;
using System.Reflection;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void ScheduleModeSwitch(ModeDefinition mode, string reason, string? targetMap = null)
    {
        CancelPendingSwitch("new_schedule");

        var delay = Math.Max(0, Config.SwitchDelaySeconds);
        _pending = new PendingSwitch(mode, reason, targetMap);
        var resolvedTargetMap = ModeSwitcher.ResolveTargetMap(mode, targetMap);

        if (delay <= 0)
        {
            ChatTone(MessageKey.SwitchApprovedNow, mode.DisplayName, resolvedTargetMap);
            ExecutePendingSwitch("immediate");
            return;
        }

        ChatTone(MessageKey.SwitchApprovedIn, mode.DisplayName, resolvedTargetMap, delay);
        LogInfo(Msg(MessageKey.LogSwitchScheduled, mode.Key, delay, reason));

        _pending.TimerHandle = AddTimer(delay, () => ExecutePendingSwitch("delay"));
    }

    private void CancelPendingSwitch(string why)
    {
        if (_pending == null)
            return;

        TryKillTimer(_pending.TimerHandle);
        _pending = null;

        LogInfo(Msg(MessageKey.LogPendingCanceled, why));
    }

    private void ExecutePendingSwitch(string execReason)
    {
        var pending = _pending;
        if (pending == null)
            return;

        var isStartupInitialSwitch = pending.Reason.StartsWith("startup_", StringComparison.OrdinalIgnoreCase);

        _pending = null;
        TryKillTimer(pending.TimerHandle);

        if (_switcher == null)
        {
            LogError(Msg(MessageKey.LogSwitcherNotInitialized));
            ChatTone(MessageKey.ChatSwitcherNotInitialized);
            return;
        }

        LogInfo(Msg(MessageKey.LogApplyingMode, pending.Mode.Key, pending.Reason, execReason));

        var mapGroupName = TryPrepareEndMatchMapVoteForMode(pending.Mode);

        if (_switcher.TrySwitchTo(
                pending.Mode,
                Config,
                pending.TargetMap,
                mapGroupName,
                out var targetMap,
                out var error))
        {
            _composition.State.StartCooldown(Config.SwitchCooldownSeconds);
            _activeModeKey = pending.Mode.Key;

            if (isStartupInitialSwitch)
                _composition.State.MarkInitialModeApplied();

            LogInfo(Msg(MessageKey.LogModeApplied, pending.Mode.DisplayName, targetMap));
            ChatTone(MessageKey.ChatModeChanged, pending.Mode.DisplayName);
        }
        else
        {
            LogError(Msg(MessageKey.LogModeApplyFailed, pending.Mode.DisplayName, error));
            ChatTone(MessageKey.ChatModeApplyFailed, pending.Mode.DisplayName);

            if (isStartupInitialSwitch && !_initialModeApplied && _initialModeQueued)
                StartInitialModeWatcher();
        }
    }

    private static void TryKillTimer(object? timerHandle)
    {
        if (timerHandle == null)
            return;

        try
        {
            var killMethod = timerHandle.GetType().GetMethod("Kill", BindingFlags.Public | BindingFlags.Instance);
            killMethod?.Invoke(timerHandle, null);
        }
        catch
        {
            // Ignore timer cleanup issues.
        }
    }
}
