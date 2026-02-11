using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal static class SteamIdResolver
{
    public static string? TryGetSteamId64(CCSPlayerController player)
    {
        try
        {
            var type = player.GetType();

            var steamIdProperty = type.GetProperty("SteamID");
            if (steamIdProperty != null)
            {
                var value = steamIdProperty.GetValue(player);
                if (value is ulong steamIdUlong && steamIdUlong != 0)
                    return steamIdUlong.ToString();

                if (value is long steamIdLong && steamIdLong != 0)
                    return steamIdLong.ToString();

                if (value is string steamIdString && !string.IsNullOrWhiteSpace(steamIdString))
                    return steamIdString.Trim();
            }

            var steamIdAltProperty = type.GetProperty("SteamId");
            if (steamIdAltProperty != null)
            {
                var value = steamIdAltProperty.GetValue(player);
                if (value is ulong steamIdUlong && steamIdUlong != 0)
                    return steamIdUlong.ToString();

                if (value is long steamIdLong && steamIdLong != 0)
                    return steamIdLong.ToString();

                if (value is string steamIdString && !string.IsNullOrWhiteSpace(steamIdString))
                    return steamIdString.Trim();
            }

            var authSteamIdProperty = type.GetProperty("AuthorizedSteamID") ?? type.GetProperty("AuthorizedSteamId");
            if (authSteamIdProperty != null)
            {
                var value = authSteamIdProperty.GetValue(player);
                if (value is ulong steamIdUlong && steamIdUlong != 0)
                    return steamIdUlong.ToString();

                if (value is long steamIdLong && steamIdLong != 0)
                    return steamIdLong.ToString();

                if (value is string steamIdString && !string.IsNullOrWhiteSpace(steamIdString))
                    return steamIdString.Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
