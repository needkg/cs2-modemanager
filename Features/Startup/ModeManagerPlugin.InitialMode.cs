using System;
using System.Linq;
using CounterStrikeSharp.API;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void StartInitialModeWatcher()
    {
        AddTimer(1.0f, () =>
        {
            if (_initialModeApplied || !_initialModeQueued || _pending != null)
                return;

            if (!HasAnyValidHumanPlayer())
            {
                StartInitialModeWatcher();
                return;
            }

            var key = (_initialModeKeyQueued ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                _initialModeQueued = false;
                return;
            }

            if (!Config.Modes.TryGetValue(key, out var mode))
            {
                _initialModeQueued = false;
                LogError(Msg(MessageKey.LogInitialModeKeyNotFound, key));
                return;
            }

            var safeDelay = Math.Max(3, Config.SwitchDelaySeconds);

            CancelPendingSwitch("startup_reschedule");
            _pending = new PendingSwitch(mode, "startup_firstplayer");

            ChatAll(Msg(MessageKey.ChatInitialModeScheduled, mode.DisplayName, safeDelay));
            LogInfo(Msg(MessageKey.LogInitialModeScheduled, mode.Key, safeDelay));

            _pending.TimerHandle = AddTimer(safeDelay, () => ExecutePendingSwitch("startup_firstplayer_delay"));
        });
    }

    private static bool HasAnyValidHumanPlayer()
    {
        try
        {
            var players = Utilities.GetPlayers();
            return players.Any(p => p != null && p.IsValid && !p.IsHLTV && !PlayerEligibility.IsIneligible(p));
        }
        catch
        {
            return false;
        }
    }
}
