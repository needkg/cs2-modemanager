using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ModeManager;

public sealed class ModeDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("Key")]
    public string? LegacyKey { set => Key = value ?? Key; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("DisplayName")]
    public string? LegacyDisplayName { set => DisplayName = value ?? DisplayName; }

    [JsonPropertyName("exec_command")]
    public string ExecCommand { get; set; } = string.Empty;

    [JsonPropertyName("ExecCommand")]
    public string? LegacyExecCommand { set => ExecCommand = value ?? ExecCommand; }

    [JsonPropertyName("default_map")]
    public string? DefaultMap { get; set; }

    [JsonPropertyName("DefaultMap")]
    public string? LegacyDefaultMap { set => DefaultMap = value; }

    [JsonPropertyName("map_pool")]
    public List<string> MapPool { get; set; } = new();

    [JsonPropertyName("MapPool")]
    public List<string>? LegacyMapPool { set => MapPool = value ?? MapPool; }

    [JsonPropertyName("game_type")]
    public int? GameType { get; set; }

    [JsonPropertyName("GameType")]
    public int? LegacyGameType { set => GameType = value; }

    [JsonPropertyName("game_mode")]
    public int? GameMode { get; set; }

    [JsonPropertyName("GameMode")]
    public int? LegacyGameMode { set => GameMode = value; }

    [JsonPropertyName("plugins_to_unload")]
    public List<string> PluginsToUnload { get; set; } = new();

    [JsonPropertyName("PluginsToUnload")]
    public List<string>? LegacyPluginsToUnload { set => PluginsToUnload = value ?? PluginsToUnload; }

    [JsonPropertyName("plugins_to_load")]
    public List<string> PluginsToLoad { get; set; } = new();

    [JsonPropertyName("PluginsToLoad")]
    public List<string>? LegacyPluginsToLoad { set => PluginsToLoad = value ?? PluginsToLoad; }
}
