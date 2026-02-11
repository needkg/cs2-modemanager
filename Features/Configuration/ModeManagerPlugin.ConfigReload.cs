using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void CmdReloadAll(CCSPlayerController? player, CommandInfo cmd)
    {
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
                cmd.ReplyToCommand(Msg(MessageKey.ReloadConfigSuccess));
            }
            else
            {
                cmd.ReplyToCommand(Msg(MessageKey.ReloadConfigNotFound, error));
                LogError(Msg(MessageKey.LogReloadConfigNotFound, error));
            }

            _vote = null;
            CancelPendingSwitch("reload");
            _cooldownUntilUtc = DateTime.MinValue;

            RebuildModeCommands();

            cmd.ReplyToCommand(Msg(MessageKey.ReloadCommandsRebuilt));
            cmd.ReplyToCommand(Msg(MessageKey.ReloadUseHelp));
        }
        catch (Exception ex)
        {
            cmd.ReplyToCommand(Msg(MessageKey.ReloadFailed, ex.Message));
            LogError(Msg(MessageKey.LogReloadException, ex));
        }
    }
}
