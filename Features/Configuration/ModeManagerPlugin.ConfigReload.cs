using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void CmdReloadAll(CCSPlayerController? player, CommandInfo cmd)
    {
        if (!CanExecuteReload(player))
        {
            ReplyTone(cmd, MessageKey.ReloadNoPermission);
            return;
        }

        try
        {
            var loadedFromDisk = _configLoader.TryLoad(_messages, out var newConfig, out var error);

            if (loadedFromDisk)
            {
                ConfigValidator.ValidateOrThrow(newConfig);
                Config = newConfig;
                ApplyLanguage(newConfig.Language);

                if (!_initialModeApplied && Config.ApplyInitialModeOnStartup)
                {
                    _initialModeKeyQueued = (Config.InitialModeKey ?? string.Empty).Trim();
                    _initialModeQueued = !string.IsNullOrWhiteSpace(_initialModeKeyQueued);
                    if (_initialModeQueued)
                        StartInitialModeWatcher();
                }

                LogInfo(Msg(MessageKey.LogConfigReloaded, _configLoader.LastResolvedPath));
                ReplyTone(cmd, MessageKey.ReloadConfigSuccess);
            }
            else
            {
                ReplyTone(cmd, MessageKey.ReloadConfigNotFound, error);
                LogError(Msg(MessageKey.LogReloadConfigNotFound, error));
            }

            EnsureResetCfgFileExists();

            ResetVotes();
            CancelPendingSwitch("reload");
            _cooldownUntilUtc = DateTime.MinValue;

            RebuildModeCommands();

            ReplyTone(cmd, MessageKey.ReloadCommandsRebuilt);
            ReplyTone(cmd, MessageKey.ReloadUseHelp);
        }
        catch (Exception ex)
        {
            ReplyTone(cmd, MessageKey.ReloadFailed, ex.Message);
            LogError(Msg(MessageKey.LogReloadException, ex));
        }
    }

    private static bool CanExecuteReload(CCSPlayerController? player)
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
