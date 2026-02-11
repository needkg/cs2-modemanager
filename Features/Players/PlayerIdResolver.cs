using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal static class PlayerIdResolver
{
    public static string? GetStableId(CCSPlayerController player)
    {
        var steamId = SteamIdResolver.TryGetSteamId64(player);
        if (!string.IsNullOrWhiteSpace(steamId))
            return $"steam:{steamId}";

        try
        {
            return $"uid:{player.UserId}";
        }
        catch
        {
            return null;
        }
    }
}
