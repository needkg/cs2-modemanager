using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

public sealed class ModeManagerConfig : BasePluginConfig
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("Language")]
    public string? LegacyLanguage { set => Language = value ?? Language; }

    [JsonPropertyName("initial_mode_key")]
    public string? InitialModeKey { get; set; } = "retake";

    [JsonPropertyName("InitialModeKey")]
    public string? LegacyInitialModeKey { set => InitialModeKey = value; }

    [JsonPropertyName("apply_initial_mode_on_startup")]
    public bool ApplyInitialModeOnStartup { get; set; } = true;

    [JsonPropertyName("ApplyInitialModeOnStartup")]
    public bool LegacyApplyInitialModeOnStartup { set => ApplyInitialModeOnStartup = value; }

    [JsonPropertyName("reset_command")]
    public string ResetCommand { get; set; } = "exec nmodemanager/reset.cfg";

    [JsonPropertyName("ResetCommand")]
    public string? LegacyResetCommand { set => ResetCommand = value ?? ResetCommand; }

    [JsonPropertyName("vote_ratio")]
    public double VoteRatio { get; set; } = 0.6;

    [JsonPropertyName("VoteRatio")]
    public double LegacyVoteRatio { set => VoteRatio = value; }

    [JsonPropertyName("vote_min_players")]
    public int VoteMinPlayers { get; set; } = 1;

    [JsonPropertyName("VoteMinPlayers")]
    public int LegacyVoteMinPlayers { set => VoteMinPlayers = value; }

    [JsonPropertyName("vote_duration_seconds")]
    public int VoteDurationSeconds { get; set; } = 120;

    [JsonPropertyName("VoteDurationSeconds")]
    public int LegacyVoteDurationSeconds { set => VoteDurationSeconds = value; }

    [JsonPropertyName("switch_cooldown_seconds")]
    public int SwitchCooldownSeconds { get; set; } = 20;

    [JsonPropertyName("SwitchCooldownSeconds")]
    public int LegacySwitchCooldownSeconds { set => SwitchCooldownSeconds = value; }

    [JsonPropertyName("switch_delay_seconds")]
    public int SwitchDelaySeconds { get; set; } = 5;

    [JsonPropertyName("SwitchDelaySeconds")]
    public int LegacySwitchDelaySeconds { set => SwitchDelaySeconds = value; }

    [JsonPropertyName("apply_game_type_mode")]
    public bool ApplyGameTypeMode { get; set; } = true;

    [JsonPropertyName("ApplyGameTypeMode")]
    public bool LegacyApplyGameTypeMode { set => ApplyGameTypeMode = value; }

    [JsonPropertyName("endmatch_map_vote_enabled")]
    public bool EndMatchMapVoteEnabled { get; set; } = true;

    [JsonPropertyName("EndMatchMapVoteEnabled")]
    public bool LegacyEndMatchMapVoteEnabled { set => EndMatchMapVoteEnabled = value; }

    [JsonPropertyName("endmatch_map_vote_file")]
    public string EndMatchMapVoteFile { get; set; } = "gamemodes_server.txt";

    [JsonPropertyName("EndMatchMapVoteFile")]
    public string? LegacyEndMatchMapVoteFile { set => EndMatchMapVoteFile = value ?? EndMatchMapVoteFile; }

    [JsonPropertyName("endmatch_map_vote_mapgroup_prefix")]
    public string EndMatchMapVoteMapgroupPrefix { get; set; } = "mg_nmm_";

    [JsonPropertyName("EndMatchMapVoteMapgroupPrefix")]
    public string? LegacyEndMatchMapVoteMapgroupPrefix { set => EndMatchMapVoteMapgroupPrefix = value ?? EndMatchMapVoteMapgroupPrefix; }

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

    [JsonPropertyName("Modes")]
    public Dictionary<string, ModeDefinition>? LegacyModes { set => Modes = value ?? Modes; }
}
