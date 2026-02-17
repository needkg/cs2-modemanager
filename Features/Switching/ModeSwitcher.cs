using System;

namespace ModeManager;

internal sealed class ModeSwitcher
{
    private readonly IServerCommandRunner _runner;
    private readonly Action<string>? _debugLog;
    private readonly object _lock = new();

    public ModeSwitcher(IServerCommandRunner runner, Action<string>? debugLog = null)
    {
        _runner = runner;
        _debugLog = debugLog;
    }

    public bool TrySwitchTo(
        ModeDefinition mode,
        ModeManagerConfig config,
        string? targetMapOverride,
        string? mapGroupOverride,
        out string targetMap,
        out string error)
    {
        lock (_lock)
        {
            try
            {
                RunCommand(config.ResetCommand);

                foreach (var pluginName in mode.PluginsToUnload)
                {
                    if (!string.IsNullOrWhiteSpace(pluginName))
                        RunCommand($"css_plugins unload \"{pluginName}\"");
                }

                foreach (var pluginName in mode.PluginsToLoad)
                {
                    if (!string.IsNullOrWhiteSpace(pluginName))
                        RunCommand($"css_plugins load \"{pluginName}\"");
                }

                RunCommand(mode.ExecCommand);

                if (config.ApplyGameTypeMode)
                {
                    if (mode.GameType.HasValue)
                        RunCommand($"game_type {mode.GameType.Value}");

                    if (mode.GameMode.HasValue)
                        RunCommand($"game_mode {mode.GameMode.Value}");
                }

                if (config.EndMatchMapVoteEnabled)
                {
                    if (!string.IsNullOrWhiteSpace(mapGroupOverride))
                        RunCommand($"mapgroup \"{mapGroupOverride.Trim()}\"");

                    RunCommand("mp_endmatch_votenextmap 1");
                    RunCommand("mp_match_end_changelevel 1");
                    RunCommand("mp_match_end_restart 0");
                }

                targetMap = ResolveTargetMap(mode, targetMapOverride);
                RunCommand($"changelevel {targetMap}");

                error = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                targetMap = string.Empty;
                error = ex.Message;
                return false;
            }
        }
    }

    private void RunCommand(string command)
    {
        _debugLog?.Invoke(command);
        _runner.Run(command);
    }

    public static string ResolveTargetMap(ModeDefinition mode, string? targetMapOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(targetMapOverride))
            return targetMapOverride.Trim();

        if (!string.IsNullOrWhiteSpace(mode.DefaultMap))
            return mode.DefaultMap.Trim();

        var currentMap = MapResolver.TryGetCurrentMapName();
        if (!string.IsNullOrWhiteSpace(currentMap))
            return currentMap.Trim();

        return "de_dust2";
    }
}
