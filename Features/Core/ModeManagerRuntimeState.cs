using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

internal sealed class ModeManagerRuntimeState
{
    public Dictionary<string, CommandInfo.CommandCallback> DynamicCommands { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool BaseCommandsRegistered { get; set; }

    public DateTime CooldownUntilUtc { get; set; } = DateTime.MinValue;

    public PendingSwitch? PendingSwitch { get; set; }
    public string? ActiveModeKey { get; set; }

    public bool InitialModeQueued { get; set; }
    public string? InitialModeKeyQueued { get; set; }
    public bool InitialModeApplied { get; set; }

    public void SeedActiveModeFromConfigIfUnknown(ModeManagerConfig config)
    {
        if (!string.IsNullOrWhiteSpace(ActiveModeKey))
            return;

        var initialModeKey = (config.InitialModeKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(initialModeKey))
            return;

        if (!config.Modes.ContainsKey(initialModeKey))
            return;

        ActiveModeKey = initialModeKey;
    }

    public bool QueueInitialMode(string? initialModeKey, bool requireNotApplied = true)
    {
        var key = (initialModeKey ?? string.Empty).Trim();
        InitialModeKeyQueued = key;

        InitialModeQueued = (!requireNotApplied || !InitialModeApplied) &&
                            !string.IsNullOrWhiteSpace(key);

        return InitialModeQueued;
    }

    public void ClearInitialModeQueuedFlag() => InitialModeQueued = false;

    public void MarkInitialModeApplied()
    {
        InitialModeApplied = true;
        InitialModeQueued = false;
    }

    public void ResetCooldown() => CooldownUntilUtc = DateTime.MinValue;

    public void StartCooldown(int seconds) => CooldownUntilUtc = DateTime.UtcNow.AddSeconds(seconds);
}
