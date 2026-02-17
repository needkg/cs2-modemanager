using System;
using System.Collections.Generic;

namespace ModeManager;

internal static class ConfigRulesValidator
{
    private static readonly string[] _selfPluginAliases =
    {
        "nModeManager",
        "nModeManager.dll"
    };

    private static readonly string[] _reservedBaseCommands =
    {
        "css_nmm",
        "css_modes",
        "css_rtv",
        "css_mode",
        "css_nmm_reload"
    };

    public static void ValidateOrThrow(ModeManagerConfig config, MessageLocalizer messages)
    {
        if (config.Modes == null || config.Modes.Count == 0)
            throw new Exception(messages.Get(MessageKey.ValidationModesRequired));

        if (string.IsNullOrWhiteSpace(config.ResetCommand))
            throw new Exception(messages.Get(MessageKey.ValidationResetCommandRequired));

        if (config.VoteRatio <= 0 || config.VoteRatio > 1.0)
            throw new Exception(messages.Get(MessageKey.ValidationVoteRatioRange));

        if (config.VoteMinPlayers < 1)
            throw new Exception(messages.Get(MessageKey.ValidationVoteMinPlayersRange));

        if (config.VoteDurationSeconds < 5 || config.VoteDurationSeconds > 300)
            throw new Exception(messages.Get(MessageKey.ValidationVoteDurationRange));

        if (config.SwitchCooldownSeconds < 0 || config.SwitchCooldownSeconds > 600)
            throw new Exception(messages.Get(MessageKey.ValidationSwitchCooldownRange));

        if (config.SwitchDelaySeconds < 0 || config.SwitchDelaySeconds > 600)
            throw new Exception(messages.Get(MessageKey.ValidationSwitchDelayRange));

        ValidateEndMatchMapVoteSettings(config, messages);

        foreach (KeyValuePair<string, ModeDefinition> entry in config.Modes)
        {
            var modeKey = entry.Key;
            var mode = entry.Value;

            if (mode == null)
                throw new Exception(messages.Format(MessageKey.ValidationModeNull, modeKey));

            if (string.IsNullOrWhiteSpace(mode.Key))
                mode.Key = modeKey;

            if (string.IsNullOrWhiteSpace(mode.DisplayName))
                mode.DisplayName = mode.Key;

            if (string.IsNullOrWhiteSpace(mode.ExecCommand))
                throw new Exception(messages.Format(MessageKey.ValidationExecCommandRequired, modeKey));

            if (ContainsSelfPluginInUnload(mode.PluginsToUnload))
            {
                throw new Exception(messages.Format(
                    MessageKey.ValidationSelfUnloadForbidden,
                    modeKey,
                    _selfPluginAliases[0]));
            }

            if (!string.IsNullOrWhiteSpace(mode.DefaultMap))
            {
                var map = mode.DefaultMap.Trim();
                if (IsInvalidMapToken(map))
                    throw new Exception(messages.Format(MessageKey.ValidationDefaultMapInvalid, modeKey, map));
            }

            ValidateAndNormalizeMapPool(modeKey, mode, messages);

            if (mode.GameType is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameTypeInvalid, modeKey, mode.GameType));

            if (mode.GameMode is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameModeInvalid, modeKey, mode.GameMode));
        }

        ValidateDynamicCommandNames(config, messages);
    }

    private static bool ContainsSelfPluginInUnload(List<string>? pluginsToUnload)
    {
        if (pluginsToUnload == null || pluginsToUnload.Count == 0)
            return false;

        foreach (var pluginName in pluginsToUnload)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
                continue;

            var trimmed = pluginName.Trim();
            foreach (var alias in _selfPluginAliases)
            {
                if (string.Equals(trimmed, alias, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static void ValidateDynamicCommandNames(ModeManagerConfig config, MessageLocalizer messages)
    {
        var reserved = new HashSet<string>(_reservedBaseCommands, StringComparer.OrdinalIgnoreCase);
        var generated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, ModeDefinition> entry in config.Modes)
        {
            var modeKey = (entry.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(modeKey))
                continue;

            var safeKey = CommandNameSanitizer.ToSafeToken(modeKey);
            var commandName = $"css_{safeKey}";

            if (reserved.Contains(commandName))
            {
                throw new Exception(messages.Format(
                    MessageKey.ValidationDynamicCommandConflictsBase,
                    modeKey,
                    commandName));
            }

            if (generated.TryGetValue(commandName, out var previousModeKey))
            {
                throw new Exception(messages.Format(
                    MessageKey.ValidationDynamicCommandCollision,
                    modeKey,
                    previousModeKey,
                    commandName));
            }

            generated[commandName] = modeKey;
        }
    }

    private static void ValidateEndMatchMapVoteSettings(ModeManagerConfig config, MessageLocalizer messages)
    {
        if (!config.EndMatchMapVoteEnabled)
            return;

        config.EndMatchMapVoteFile = (config.EndMatchMapVoteFile ?? string.Empty).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(config.EndMatchMapVoteFile))
            throw new Exception(messages.Get(MessageKey.ValidationEndMatchMapVoteFileRequired));

        config.EndMatchMapVoteMapgroupPrefix = (config.EndMatchMapVoteMapgroupPrefix ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(config.EndMatchMapVoteMapgroupPrefix) ||
            IsInvalidMapGroupToken(config.EndMatchMapVoteMapgroupPrefix))
        {
            throw new Exception(messages.Format(
                MessageKey.ValidationEndMatchMapVoteMapgroupPrefixInvalid,
                config.EndMatchMapVoteMapgroupPrefix));
        }
    }

    private static void ValidateAndNormalizeMapPool(string modeKey, ModeDefinition mode, MessageLocalizer messages)
    {
        if (mode.MapPool == null || mode.MapPool.Count == 0)
        {
            mode.MapPool = new List<string>();
            return;
        }

        var normalizedPool = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawMap in mode.MapPool)
        {
            var map = (rawMap ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(map))
                continue;

            if (IsInvalidMapToken(map))
                throw new Exception(messages.Format(MessageKey.ValidationMapPoolMapInvalid, modeKey, map));

            if (seen.Add(map))
                normalizedPool.Add(map);
        }

        mode.MapPool = normalizedPool;
    }

    private static bool IsInvalidMapToken(string map) => map.Contains(' ') || map.Contains('"');

    private static bool IsInvalidMapGroupToken(string mapGroup) => mapGroup.Contains(' ') || mapGroup.Contains('"');
}
