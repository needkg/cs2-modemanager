using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

public sealed class ModeManagerConfig : BasePluginConfig
{
    [JsonPropertyName("Language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("InitialModeKey")]
    public string? InitialModeKey { get; set; } = "retake";

    [JsonPropertyName("ApplyInitialModeOnStartup")]
    public bool ApplyInitialModeOnStartup { get; set; } = true;

    [JsonPropertyName("ResetCommand")]
    public string ResetCommand { get; set; } = "exec nmodemanager/reset.cfg";

    [JsonPropertyName("VoteRatio")]
    public double VoteRatio { get; set; } = 0.6;

    [JsonPropertyName("VoteMinPlayers")]
    public int VoteMinPlayers { get; set; } = 1;

    [JsonPropertyName("VoteDurationSeconds")]
    public int VoteDurationSeconds { get; set; } = 120;

    [JsonPropertyName("SwitchCooldownSeconds")]
    public int SwitchCooldownSeconds { get; set; } = 20;

    [JsonPropertyName("SwitchDelaySeconds")]
    public int SwitchDelaySeconds { get; set; } = 5;

    [JsonPropertyName("ApplyGameTypeMode")]
    public bool ApplyGameTypeMode { get; set; } = true;

    [JsonPropertyName("Modes")]
    public Dictionary<string, ModeDefinition> Modes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["retake"] = new ModeDefinition
            {
                Key = "retake",
                DisplayName = "Retake",
                ExecCommand = "exec nmodemanager/retake.cfg"
            }
        };
}
