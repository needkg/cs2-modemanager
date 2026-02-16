using System;
using System.IO;

namespace ModeManager;

internal sealed class ResetCfgProvisioner
{
    private readonly string _moduleName;
    private readonly string _typeName;
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logError;

    public ResetCfgProvisioner(
        string moduleName,
        string typeName,
        Func<MessageKey, object?[], string> msg,
        Action<string> logInfo,
        Action<string> logError)
    {
        _moduleName = moduleName;
        _typeName = typeName;
        _msg = msg;
        _logInfo = logInfo;
        _logError = logError;
    }

    public void EnsureResetCfgFileExists(string resetCommand)
    {
        try
        {
            if (!TryExtractExecCfgRelativePath(resetCommand, out var relativeCfgPath))
                return;

            var path = ResolveResetCfgPhysicalPath(relativeCfgPath);
            if (string.IsNullOrWhiteSpace(path) || File.Exists(path))
                return;

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
            File.WriteAllText(path, BuildDefaultResetCfgText());

            _logInfo(_msg(MessageKey.LogResetCfgCreated, new object?[] { path }));
        }
        catch (Exception ex)
        {
            _logError(_msg(MessageKey.LogResetCfgCreateFailed, new object?[] { resetCommand, ex.Message }));
        }
    }

    private string? ResolveResetCfgPhysicalPath(string relativeCfgPath)
    {
        var discoveredConfigPath = ConfigPathDiscovery.Discover(_moduleName, _typeName);

        if (TryGetServerRootFromConfigPath(discoveredConfigPath, out var serverRoot))
            return CombineCfgPath(serverRoot, relativeCfgPath);

        var fallbackRoot = TryFindLikelyServerRoot();
        if (fallbackRoot == null)
            return null;

        return CombineCfgPath(fallbackRoot, relativeCfgPath);
    }

    private static bool TryGetServerRootFromConfigPath(string? configPath, out string root)
    {
        root = string.Empty;

        if (string.IsNullOrWhiteSpace(configPath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(configPath);
            var marker = Path.Combine("addons", "counterstrikesharp", "configs", "plugins");
            var markerIndex = fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex <= 0)
                return false;

            var candidateRoot = fullPath[..markerIndex]
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(candidateRoot))
                return false;

            root = candidateRoot;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryFindLikelyServerRoot()
    {
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        }
        catch
        {
            return null;
        }

        for (var depth = 0; directory != null && depth < 12; depth++, directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "cfg")))
                return directory.FullName;

            var csgoRoot = Path.Combine(directory.FullName, "csgo");
            if (Directory.Exists(Path.Combine(csgoRoot, "cfg")))
                return csgoRoot;
        }

        return null;
    }

    private static bool TryExtractExecCfgRelativePath(string resetCommand, out string relativeCfgPath)
    {
        relativeCfgPath = string.Empty;

        if (string.IsNullOrWhiteSpace(resetCommand))
            return false;

        var command = resetCommand.Trim();
        if (!command.StartsWith("exec", StringComparison.OrdinalIgnoreCase))
            return false;

        var remainder = command.Length > 4 ? command[4..].Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(remainder))
            return false;

        string token;
        if (remainder[0] == '"')
        {
            var endQuote = remainder.IndexOf('"', 1);
            if (endQuote <= 1)
                return false;

            token = remainder[1..endQuote];
        }
        else
        {
            var firstWhitespace = remainder.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            token = firstWhitespace < 0 ? remainder : remainder[..firstWhitespace];
        }

        token = token.Trim().TrimEnd(';').TrimStart('/', '\\').Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!token.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
            token += ".cfg";

        if (Path.IsPathRooted(token) || token.Contains(':'))
            return false;

        var segments = token.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return false;

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
                return false;
        }

        relativeCfgPath = string.Join('/', segments);
        return true;
    }

    private static string CombineCfgPath(string serverRoot, string relativeCfgPath)
    {
        var normalizedRelative = relativeCfgPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return Path.GetFullPath(Path.Combine(serverRoot, "cfg", normalizedRelative));
    }

    private static string BuildDefaultResetCfgText()
    {
        return """
echo =========================================
echo [nModeManager] Applying FULL RESET CONFIG
echo =========================================

// ==================================================
// CORE MODE RESET
// ==================================================
game_type 0
game_mode 0
sv_skirmish_id 0

// ==================================================
// DEFAULT WEAPON RESET
// ==================================================
mp_ct_default_primary ""
mp_ct_default_secondary weapon_hkp2000
mp_ct_default_melee weapon_knife
mp_ct_default_grenades ""

mp_t_default_primary ""
mp_t_default_secondary weapon_glock
mp_t_default_melee weapon_knife
mp_t_default_grenades ""

mp_weapons_allow_map_placed 1
mp_weapons_allow_typecount -1

// ==================================================
// Bots
// ==================================================
bot_kick
bot_quota 0

// ==================================================
// Economy
// ==================================================
mp_maxmoney 16000
mp_startmoney 800
mp_afterroundmoney 0
mp_playercashawards 1
mp_teamcashawards 1

// ==================================================
// Buy settings
// ==================================================
mp_buy_anywhere 0
mp_buytime 20

// ==================================================
// Respawn
// ==================================================
mp_respawn_on_death_ct 0
mp_respawn_on_death_t 0

// ==================================================
// Round settings
// ==================================================
mp_roundtime 1.92
mp_roundtime_defuse 1.92
mp_round_restart_delay 5
mp_freezetime 15
mp_warmuptime 60
mp_warmup_pausetimer 0
mp_ignore_round_win_conditions 0
mp_match_can_clinch 1
mp_match_end_restart 0
mp_match_restart_delay 5
mp_maxrounds 24
mp_timelimit 0

// ==================================================
// Bomb
// ==================================================
mp_c4timer 40
mp_give_player_c4 1

// ==================================================
// Teams
// ==================================================
mp_autoteambalance 1
mp_autokick 1
mp_solid_teammates 1
mp_forcecamera 1

// ==================================================
// Damage / Friendly Fire
// ==================================================
mp_friendlyfire 0

// ==================================================
// Drops
// ==================================================
mp_death_drop_gun 1
mp_death_drop_defuser 1
mp_death_drop_grenade 1

// ==================================================
// Communication
// ==================================================
sv_talk_enemy_dead 0
sv_talk_enemy_living 0
sv_deadtalk 0

// ==================================================
// Spectator / Replay
// ==================================================
spec_replay_enable 0

mp_restartgame 1

echo =========================================
echo [nModeManager] FULL RESET COMPLETE
echo =========================================
""";
    }
}
