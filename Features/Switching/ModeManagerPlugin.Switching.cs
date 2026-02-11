using System;
using System.Reflection;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void ScheduleModeSwitch(ModeDefinition mode, string reason)
    {
        CancelPendingSwitch("new_schedule");

        var delay = Math.Max(0, Config.SwitchDelaySeconds);
        _pending = new PendingSwitch(mode, reason);

        if (delay <= 0)
        {
            ChatAll(Msg(MessageKey.SwitchApprovedNow, mode.DisplayName));
            ExecutePendingSwitch("immediate");
            return;
        }

        ChatAll(Msg(MessageKey.SwitchApprovedIn, mode.DisplayName, delay));
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

        _pending = null;
        TryKillTimer(pending.TimerHandle);

        _cooldownUntilUtc = DateTime.UtcNow.AddSeconds(Config.SwitchCooldownSeconds);

        if (_switcher == null)
        {
            LogError(Msg(MessageKey.LogSwitcherNotInitialized));
            ChatAll(Msg(MessageKey.ChatSwitcherNotInitialized));
            return;
        }

        LogInfo(Msg(MessageKey.LogApplyingMode, pending.Mode.Key, pending.Reason, execReason));

        if (_switcher.TrySwitchTo(pending.Mode, Config, out var targetMap, out var error))
        {
            LogInfo(Msg(MessageKey.LogModeApplied, pending.Mode.DisplayName, targetMap));
            ChatAll(Msg(MessageKey.ChatModeChanged, pending.Mode.DisplayName));
        }
        else
        {
            LogError(Msg(MessageKey.LogModeApplyFailed, pending.Mode.DisplayName, error));
            ChatAll(Msg(MessageKey.ChatModeApplyFailed, pending.Mode.DisplayName));
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
