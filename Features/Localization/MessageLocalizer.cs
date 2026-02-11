using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace ModeManager;

internal sealed class MessageLocalizer
{
    private static readonly string[] _supportedLanguages = { "en", "pt-BR" };
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>>> _catalogs =
        new(LoadCatalogs);
    private static readonly Lazy<IReadOnlyDictionary<MessageKey, string>> _enumFallbackCatalog =
        new(BuildEnumFallbackCatalog);

    private readonly IReadOnlyDictionary<MessageKey, string> _messages;
    private readonly IReadOnlyDictionary<MessageKey, string> _fallbackMessages;

    private MessageLocalizer(
        string languageCode,
        IReadOnlyDictionary<MessageKey, string> messages,
        IReadOnlyDictionary<MessageKey, string> fallbackMessages)
    {
        LanguageCode = languageCode;
        _messages = messages;
        _fallbackMessages = fallbackMessages;
    }

    public string LanguageCode { get; }

    public static IReadOnlyList<string> SupportedLanguages => _supportedLanguages;

    public static MessageLocalizer Create(string? languageCode)
    {
        TryNormalize(languageCode, out var normalized);

        var catalogs = _catalogs.Value;
        var fallbackMessages = ResolveEnglishCatalog(catalogs);
        var messages = ResolveLanguageCatalog(catalogs, normalized, fallbackMessages);

        return new MessageLocalizer(normalized, messages, fallbackMessages);
    }

    public static bool IsSupported(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return true;

        return TryNormalize(languageCode, out _);
    }

    public string Get(MessageKey key)
    {
        if (_messages.TryGetValue(key, out var message))
            return message;

        if (_fallbackMessages.TryGetValue(key, out message))
            return message;

        return key.ToString();
    }

    public string Format(MessageKey key, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key), args);

    private static IReadOnlyDictionary<MessageKey, string> ResolveEnglishCatalog(
        IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> catalogs)
    {
        if (catalogs.TryGetValue("en", out var english) && english.Count > 0)
            return english;

        return _enumFallbackCatalog.Value;
    }

    private static IReadOnlyDictionary<MessageKey, string> ResolveLanguageCatalog(
        IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> catalogs,
        string languageCode,
        IReadOnlyDictionary<MessageKey, string> fallbackMessages)
    {
        if (catalogs.TryGetValue(languageCode, out var selected) && selected.Count > 0)
            return selected;

        return fallbackMessages;
    }

    private static bool TryNormalize(string? languageCode, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            normalized = "en";
            return true;
        }

        var trimmed = languageCode.Trim();
        if (trimmed.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "pt-BR";
            return true;
        }

        if (trimmed.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "en";
            return true;
        }

        normalized = "en";
        return false;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> LoadCatalogs()
    {
        var catalogs = new Dictionary<string, IReadOnlyDictionary<MessageKey, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in _supportedLanguages)
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
