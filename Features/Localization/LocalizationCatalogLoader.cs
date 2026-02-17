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
            ParseFlatCatalog(document.RootElement, parsedCatalog);

            return parsedCatalog;
        }
        catch
        {
            return new Dictionary<MessageKey, string>();
        }
    }

    private static void ParseFlatCatalog(
        JsonElement root,
        IDictionary<MessageKey, string> catalog)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
                continue;

            var value = property.Value.GetString();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!TryParseMessageKey(property.Name, out var key))
                continue;

            catalog[key] = value;
        }
    }

    private static bool TryParseMessageKey(string rawKey, out MessageKey key)
    {
        key = default;
        if (!IsNamespaceSnakeCaseKey(rawKey))
            return false;

        var normalized = (rawKey ?? string.Empty)
            .Trim()
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse<MessageKey>(normalized, ignoreCase: true, out key);
    }

    private static bool IsNamespaceSnakeCaseKey(string key)
    {
        var raw = (key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var firstDot = raw.IndexOf('.');
        if (firstDot <= 0 || firstDot != raw.LastIndexOf('.'))
            return false;

        var namespaceToken = raw[..firstDot];
        var valueToken = raw[(firstDot + 1)..];

        return IsNamespaceToken(namespaceToken) && IsSnakeCaseToken(valueToken);
    }

    private static bool IsNamespaceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token[0] < 'a' || token[0] > 'z')
            return false;

        for (var i = 1; i < token.Length; i++)
        {
            var ch = token[i];
            var isLowerOrDigit = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
            if (!isLowerOrDigit)
                return false;
        }

        return true;
    }

    private static bool IsSnakeCaseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token[0] == '_' || token[^1] == '_')
            return false;

        var previousUnderscore = false;
        for (var i = 0; i < token.Length; i++)
        {
            var ch = token[i];
            if (ch == '_')
            {
                if (previousUnderscore)
                    return false;

                previousUnderscore = true;
                continue;
            }

            var isLowerOrDigit = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
            if (!isLowerOrDigit)
                return false;

            previousUnderscore = false;
        }

        return true;
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
