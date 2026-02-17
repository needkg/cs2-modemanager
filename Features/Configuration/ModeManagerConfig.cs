using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

public sealed class ModeManagerConfig : BasePluginConfig
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("initial_mode_key")]
    public string? InitialModeKey { get; set; } = "retake";

    [JsonPropertyName("apply_initial_mode_on_startup")]
    public bool ApplyInitialModeOnStartup { get; set; } = true;

    [JsonPropertyName("reset_command")]
    public string ResetCommand { get; set; } = "exec nmodemanager/reset.cfg";

    [JsonPropertyName("vote_ratio")]
    public double VoteRatio { get; set; } = 0.6;

    [JsonPropertyName("vote_min_players")]
    public int VoteMinPlayers { get; set; } = 1;

    [JsonPropertyName("vote_duration_seconds")]
    public int VoteDurationSeconds { get; set; } = 120;

    [JsonPropertyName("switch_cooldown_seconds")]
    public int SwitchCooldownSeconds { get; set; } = 20;

    [JsonPropertyName("switch_delay_seconds")]
    public int SwitchDelaySeconds { get; set; } = 5;

    [JsonPropertyName("apply_game_type_mode")]
    public bool ApplyGameTypeMode { get; set; } = true;

    [JsonPropertyName("debug")]
    public bool DebugEnabled { get; set; }

    [JsonPropertyName("endmatch_map_vote_enabled")]
    public bool EndMatchMapVoteEnabled { get; set; } = true;

    [JsonPropertyName("endmatch_map_vote_file")]
    public string EndMatchMapVoteFile { get; set; } = "gamemodes_server.txt";

    [JsonPropertyName("endmatch_map_vote_mapgroup_prefix")]
    public string EndMatchMapVoteMapgroupPrefix { get; set; } = "mg_nmm_";

    [JsonPropertyName("modes")]
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
