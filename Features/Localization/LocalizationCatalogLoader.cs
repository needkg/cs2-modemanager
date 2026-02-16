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

            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<MessageKey, string>();

            var parsedCatalog = new Dictionary<MessageKey, string>();
            FlattenCatalog(document.RootElement, pathPrefix: null, parsedCatalog);

            return parsedCatalog;
        }
        catch
        {
            return new Dictionary<MessageKey, string>();
        }
    }

    private static void FlattenCatalog(
        JsonElement element,
        string? pathPrefix,
        IDictionary<MessageKey, string> catalog)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            var path = string.IsNullOrWhiteSpace(pathPrefix)
                ? property.Name
                : $"{pathPrefix}_{property.Name}";

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    FlattenCatalog(property.Value, path, catalog);
                    break;
                case JsonValueKind.String:
                    var value = property.Value.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                        break;

                    if (!TryParseMessageKey(path, out var key))
                        break;

                    catalog[key] = value;
                    break;
                default:
                    break;
            }
        }
    }

    private static bool TryParseMessageKey(string rawKey, out MessageKey key)
    {
        if (Enum.TryParse<MessageKey>(rawKey, ignoreCase: true, out key))
            return true;

        var normalized = (rawKey ?? string.Empty)
            .Trim()
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse<MessageKey>(normalized, ignoreCase: true, out key);
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
