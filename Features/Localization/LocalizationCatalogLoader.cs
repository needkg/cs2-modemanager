using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ModeManager;

internal static class LocalizationCatalogLoader
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>>> _catalogs =
        new(LoadCatalogs);
    private static readonly Lazy<IReadOnlyDictionary<MessageKey, string>> _enumFallbackCatalog =
        new(BuildEnumFallbackCatalog);

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> Catalogs => _catalogs.Value;

    public static IReadOnlyDictionary<MessageKey, string> EnumFallbackCatalog => _enumFallbackCatalog.Value;

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> LoadCatalogs()
    {
        var catalogs = new Dictionary<string, IReadOnlyDictionary<MessageKey, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in LocalizationLanguage.SupportedLanguages)
            catalogs[language] = LoadCatalog(language);

        return catalogs;
    }

    private static IReadOnlyDictionary<MessageKey, string> LoadCatalog(string languageCode)
    {
        try
        {
            var json = TryReadCatalogText(languageCode);
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<MessageKey, string>();

            var rawCatalog = JsonSerializer.Deserialize<Dictionary<string, string>>(json, new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (rawCatalog == null || rawCatalog.Count == 0)
                return new Dictionary<MessageKey, string>();

            var parsedCatalog = new Dictionary<MessageKey, string>();
            foreach (KeyValuePair<string, string> entry in rawCatalog)
            {
                if (!Enum.TryParse<MessageKey>(entry.Key, false, out var key))
                    continue;
                if (string.IsNullOrWhiteSpace(entry.Value))
                    continue;

                parsedCatalog[key] = entry.Value;
            }

            return parsedCatalog;
        }
        catch
        {
            return new Dictionary<MessageKey, string>();
        }
    }

    private static string? TryReadCatalogText(string languageCode)
    {
        try
        {
            var path = ResolveCatalogPath(languageCode);
            if (path != null && File.Exists(path))
                return File.ReadAllText(path);
        }
        catch
        {
            // Ignore IO failures and try embedded fallback.
        }

        try
        {
            return TryReadEmbeddedCatalogText(languageCode);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadEmbeddedCatalogText(string languageCode)
    {
        var assembly = typeof(MessageLocalizer).Assembly;
        var expectedSuffix = $".lang.{languageCode}.json";
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? ResolveCatalogPath(string languageCode)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "lang", $"{languageCode}.json");
        if (File.Exists(basePath))
            return basePath;

        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), "lang", $"{languageCode}.json");
        if (File.Exists(currentPath))
            return currentPath;

        return basePath;
    }

    private static IReadOnlyDictionary<MessageKey, string> BuildEnumFallbackCatalog()
    {
        var fallback = new Dictionary<MessageKey, string>();
        foreach (MessageKey key in Enum.GetValues<MessageKey>())
            fallback[key] = key.ToString();

        return fallback;
    }
}
