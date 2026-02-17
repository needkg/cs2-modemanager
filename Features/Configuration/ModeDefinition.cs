using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ModeManager;

public sealed class ModeDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("exec_command")]
    public string ExecCommand { get; set; } = string.Empty;

    [JsonPropertyName("default_map")]
    public string? DefaultMap { get; set; }

    [JsonPropertyName("map_pool")]
    public List<string> MapPool { get; set; } = new();

    [JsonPropertyName("game_type")]
    public int? GameType { get; set; }

    [JsonPropertyName("game_mode")]
    public int? GameMode { get; set; }

    [JsonPropertyName("plugins_to_unload")]
    public List<string> PluginsToUnload { get; set; } = new();

    [JsonPropertyName("plugins_to_load")]
    public List<string> PluginsToLoad { get; set; } = new();
}
