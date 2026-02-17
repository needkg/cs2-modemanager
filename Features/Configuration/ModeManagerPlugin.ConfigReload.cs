using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void CmdReloadAll(CCSPlayerController? player, CommandInfo cmd)
    {
        if (!AdminAccessPolicy.CanExecuteRootAction(player))
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
                LogDebug(MessageKey.LogDebugSettings, Config.DebugEnabled);

                if (!_initialModeApplied &&
                    Config.ApplyInitialModeOnStartup &&
                    _composition.State.QueueInitialMode(Config.InitialModeKey, requireNotApplied: false))
                    StartInitialModeWatcher();

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
            _composition.State.ResetCooldown();

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
}
