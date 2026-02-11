using System;
using System.IO;
using System.Text.Json;

namespace ModeManager;

internal sealed class ConfigFileLoader
{
    private readonly string _moduleName;
    private readonly string _typeName;
    private string? _cachedPath;

    public ConfigFileLoader(string moduleName, string typeName)
    {
        _moduleName = moduleName;
        _typeName = typeName;
    }

    public string? LastResolvedPath => _cachedPath;

    public bool TryLoad(MessageLocalizer messages, out ModeManagerConfig config, out string error)
    {
        config = new ModeManagerConfig();
        error = string.Empty;

        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedPath) && File.Exists(_cachedPath))
                return TryDeserialize(_cachedPath, messages, out config, out error);

            var discoveredPath = ConfigPathDiscovery.Discover(_moduleName, _typeName);
            if (discoveredPath == null)
            {
                error = messages.Format(
                    MessageKey.LoaderConfigNotFound,
                    string.Join(" | ", ConfigPathDiscovery.GetCommonPaths(_moduleName, _typeName)));
                return false;
            }

            _cachedPath = discoveredPath;
            return TryDeserialize(discoveredPath, messages, out config, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDeserialize(string path, MessageLocalizer messages, out ModeManagerConfig config, out string error)
    {
        config = new ModeManagerConfig();
        error = string.Empty;

        try
        {
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var loaded = JsonSerializer.Deserialize<ModeManagerConfig>(json, options);
            if (loaded == null)
            {
                error = messages.Get(MessageKey.LoaderDeserializeNull);
                return false;
            }

            config = loaded;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
