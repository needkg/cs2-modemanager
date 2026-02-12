using System.Linq;
using CounterStrikeSharp.API;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private static void LogInfo(string msg) => Server.PrintToConsole($"[nModeManager] {msg}");
    private static void LogError(string msg) => Server.PrintToConsole($"[nModeManager:ERROR] {msg}");

    private static void ChatAll(string msg)
    {
        try
        {
            var players = Utilities.GetPlayers();
            var anyValid = players.Any(p => p != null && p.IsValid && !p.IsHLTV);
            if (!anyValid)
            {
                Server.PrintToConsole($"[Mode] {msg}");
                return;
            }

            Server.PrintToChatAll($"[Mode] {msg}");
        }
        catch
        {
            Server.PrintToConsole($"[Mode] {msg}");
        }
    }
}
