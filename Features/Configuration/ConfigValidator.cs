using System;
using System.Collections.Generic;

namespace ModeManager;

internal static class ConfigValidator
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
        "css_nmm_setmode",
        "css_nmm_vote",
        "css_nmm_reload"
    };

    public static void ValidateOrThrow(ModeManagerConfig config)
    {
        var messages = MessageLocalizer.Create(config.Language);

        if (!MessageLocalizer.IsSupported(config.Language))
        {
            throw new Exception(messages.Format(
                MessageKey.ValidationLanguageUnsupported,
                config.Language ?? string.Empty,
                string.Join(", ", MessageLocalizer.SupportedLanguages)));
        }

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

            ValidatePluginsToLoadExist(modeKey, mode.PluginsToLoad, messages);

            if (!string.IsNullOrWhiteSpace(mode.DefaultMap))
            {
                var map = mode.DefaultMap.Trim();
                if (map.Contains(' ') || map.Contains('"'))
                    throw new Exception(messages.Format(MessageKey.ValidationDefaultMapInvalid, modeKey, map));
            }

            if (mode.GameType is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameTypeInvalid, modeKey, mode.GameType));

            if (mode.GameMode is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameModeInvalid, modeKey, mode.GameMode));
        }

        ValidateDynamicCommandNames(config, messages);
    }

    private static void ValidatePluginsToLoadExist(
        string modeKey,
        List<string>? pluginsToLoad,
        MessageLocalizer messages)
    {
        if (pluginsToLoad == null || pluginsToLoad.Count == 0)
            return;

        foreach (var pluginName in pluginsToLoad)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
                continue;

            var trimmedName = pluginName.Trim();
            if (ConfigPathDiscovery.TryResolvePluginDll(trimmedName, out _, out var searchedPaths))
                continue;

            throw new Exception(messages.Format(
                MessageKey.ValidationPluginToLoadNotFound,
                modeKey,
                trimmedName,
                searchedPaths));
        }
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
}
