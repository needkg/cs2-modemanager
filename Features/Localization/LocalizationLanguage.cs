using System;
using System.Collections.Generic;

namespace ModeManager;

internal static class LocalizationLanguage
{
    private static readonly string[] _supportedLanguages = { "en", "pt-BR", "es" };

    public static IReadOnlyList<string> SupportedLanguages => _supportedLanguages;

    public static bool TryNormalize(string? languageCode, out string normalized)
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

        if (trimmed.StartsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "es";
            return true;
        }

        normalized = "en";
        return false;
    }
}
