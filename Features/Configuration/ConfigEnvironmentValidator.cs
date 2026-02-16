using System;
using System.Collections.Generic;

namespace ModeManager;

internal static class ConfigEnvironmentValidator
{
    public static void ValidateOrThrow(ModeManagerConfig config, MessageLocalizer messages)
    {
        if (config.Modes == null || config.Modes.Count == 0)
            return;

        foreach (KeyValuePair<string, ModeDefinition> entry in config.Modes)
        {
            var mode = entry.Value;
            if (mode == null)
                continue;

            ValidatePluginsToLoadExist(entry.Key, mode.PluginsToLoad, messages);
        }
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
}
