using System;
using System.Collections.Generic;
namespace ModeManager;

internal sealed class MessageLocalizer
{
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

    public static IReadOnlyList<string> SupportedLanguages => LocalizationLanguage.SupportedLanguages;

    public static MessageLocalizer Create(string? languageCode)
    {
        LocalizationLanguage.TryNormalize(languageCode, out var normalized);

        var catalogs = LocalizationCatalogLoader.Catalogs;
        var fallbackMessages = ResolveEnglishCatalog(catalogs);
        var messages = ResolveLanguageCatalog(catalogs, normalized, fallbackMessages);

        return new MessageLocalizer(normalized, messages, fallbackMessages);
    }

    public static bool IsSupported(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return true;

        return LocalizationLanguage.TryNormalize(languageCode, out _);
    }

    public string Get(MessageKey key)
    {
        if (_messages.TryGetValue(key, out var message))
            return message;

        if (_fallbackMessages.TryGetValue(key, out message))
            return message;

        return key.ToString();
    }

    public string Format(MessageKey key, params object?[] args)
    {
        var raw = Get(key);
        return LocalizationMessageFormatter.Format(raw, args);
    }

    private static IReadOnlyDictionary<MessageKey, string> ResolveEnglishCatalog(
        IReadOnlyDictionary<string, IReadOnlyDictionary<MessageKey, string>> catalogs)
    {
        if (catalogs.TryGetValue("en", out var english) && english.Count > 0)
            return english;

        return LocalizationCatalogLoader.EnumFallbackCatalog;
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
}
