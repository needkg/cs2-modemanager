using System;

namespace ModeManager;

internal sealed class ModeSwitcher
{
    private readonly IServerCommandRunner _runner;
    private readonly object _lock = new();

    public ModeSwitcher(IServerCommandRunner runner)
    {
        _runner = runner;
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
                _runner.Run(config.ResetCommand);

                foreach (var pluginName in mode.PluginsToUnload)
                {
                    if (!string.IsNullOrWhiteSpace(pluginName))
                        _runner.Run($"css_plugins unload \"{pluginName}\"");
                }

                foreach (var pluginName in mode.PluginsToLoad)
                {
                    if (!string.IsNullOrWhiteSpace(pluginName))
                        _runner.Run($"css_plugins load \"{pluginName}\"");
                }

                _runner.Run(mode.ExecCommand);

                if (config.ApplyGameTypeMode)
                {
                    if (mode.GameType.HasValue)
                        _runner.Run($"game_type {mode.GameType.Value}");

                    if (mode.GameMode.HasValue)
                        _runner.Run($"game_mode {mode.GameMode.Value}");
                }

                if (config.EndMatchMapVoteEnabled)
                {
                    if (!string.IsNullOrWhiteSpace(mapGroupOverride))
                        _runner.Run($"mapgroup \"{mapGroupOverride.Trim()}\"");

                    _runner.Run("mp_endmatch_votenextmap 1");
                    _runner.Run("mp_match_end_changelevel 1");
                    _runner.Run("mp_match_end_restart 0");
                }

                targetMap = ResolveTargetMap(mode, targetMapOverride);
                _runner.Run($"changelevel {targetMap}");

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
