using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ModeManager;

internal static class LocalizationMessageFormatter
{
    private static readonly Regex _namedTokenPattern = new(@"\{([A-Za-z_][A-Za-z0-9_-]*)\}", RegexOptions.Compiled);

    public static string Format(string message, params object?[] args)
    {
        var escaped = EscapeNamedTokens(message);
        return string.Format(CultureInfo.InvariantCulture, escaped, args ?? Array.Empty<object?>());
    }

    private static string EscapeNamedTokens(string message) =>
        _namedTokenPattern.Replace(message, "{{$1}}");
}
