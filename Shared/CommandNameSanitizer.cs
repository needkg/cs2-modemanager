namespace ModeManager;

internal static class CommandNameSanitizer
{
    public static string ToSafeToken(string input)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            var isAllowed =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_';

            chars[i] = isAllowed ? c : '_';
        }

        var token = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(token) ? "mode" : token;
    }
}
