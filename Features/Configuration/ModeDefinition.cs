using System.Collections.Generic;

namespace ModeManager;

public sealed class ModeDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecCommand { get; set; } = string.Empty;

    public string? DefaultMap { get; set; }
    public List<string> MapPool { get; set; } = new();

    public int? GameType { get; set; }
    public int? GameMode { get; set; }

    public List<string> PluginsToUnload { get; set; } = new();
    public List<string> PluginsToLoad { get; set; } = new();
}
