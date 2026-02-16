using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace ModeManager;

internal static class AdminAccessPolicy
{
    public static bool CanExecuteRootAction(CCSPlayerController? player)
    {
        if (player == null)
            return true;

        if (!player.IsValid)
            return false;

        try
        {
            return AdminManager.PlayerHasPermissions(player, new[] { "@css/root" });
        }
        catch
        {
            return false;
        }
    }
}
