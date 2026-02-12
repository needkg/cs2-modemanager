using System;
using System.Linq;
using CounterStrikeSharp.API;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private static void LogInfo(string msg) => Server.PrintToConsole($"[nModeManager] {msg}");
    private static void LogError(string msg) => Server.PrintToConsole($"[nModeManager:ERROR] {msg}");

    private void ChatAll(string msg)
    {
        try
        {
            var players = Utilities.GetPlayers();
            var anyValid = players.Any(p => p != null && p.IsValid && !p.IsHLTV);
            if (!anyValid)
            {
                Server.PrintToConsole(PrefixMessageForConsole(msg));
                return;
            }

            Server.PrintToChatAll(PrefixMessageForChat(msg));
        }
        catch
        {
            Server.PrintToConsole(PrefixMessageForConsole(msg));
        }
    }

    private static string StripChatControlCodes(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return string.Empty;

        return new string(msg.Where(ch => !char.IsControl(ch) || ch is '\r' or '\n' or '\t').ToArray());
    }
}
