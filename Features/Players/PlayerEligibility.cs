using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal static class PlayerEligibility
{
    public static int CountEligiblePlayers()
    {
        var players = Utilities.GetPlayers();
        var count = 0;

        foreach (var player in players)
        {
            if (player == null || !player.IsValid)
                continue;

            if (IsIneligible(player))
                continue;

            count++;
        }

        return count;
    }

    public static bool IsIneligible(CCSPlayerController player)
    {
        try
        {
            if (player.IsHLTV)
                return true;

            var type = player.GetType();
            var botProperty = type.GetProperty("IsBot", BindingFlags.Public | BindingFlags.Instance);
            if (botProperty != null && botProperty.GetValue(player) is bool isBot && isBot)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}
