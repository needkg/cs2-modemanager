using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private void RegisterBaseCommands()
    {
        if (_baseCommandsRegistered)
            return;

        AddCommand("css_nmm", Msg(MessageKey.CmdDescHelp), CmdHelp);
        AddCommand("css_modes", Msg(MessageKey.CmdDescModes), CmdListModes);

        AddCommand("css_rtv", Msg(MessageKey.CmdDescRtv), CmdOpenRtvMenu);
        AddCommand("css_mode", Msg(MessageKey.CmdDescSetMode), CmdAdminSetMode);
        AddCommand("css_nmm_reload", Msg(MessageKey.CmdDescReload), CmdReloadAll);

        _baseCommandsRegistered = true;
    }

    private void CmdHelp(CCSPlayerController? player, CommandInfo cmd)
    {
        ReplyTone(cmd, MessageKey.HelpTitle);
        ReplyTone(cmd, MessageKey.HelpCommandsLabel);
        ReplyTone(cmd, MessageKey.HelpLineMm);
        ReplyTone(cmd, MessageKey.HelpLineModes);
        ReplyTone(cmd, MessageKey.HelpLineRtv);
        ReplyTone(cmd, MessageKey.HelpLineSetMode);
        ReplyTone(cmd, MessageKey.HelpLineDynamicMode);
        ReplyTone(cmd, MessageKey.HelpLineReload);

        var keys = string.Join(", ", Config.Modes.Keys);
        ReplyTone(cmd, MessageKey.HelpModesList, keys);
    }

    private void CmdListModes(CCSPlayerController? player, CommandInfo cmd)
    {
        var keys = string.Join(", ", Config.Modes.Keys);
        ReplyTone(cmd, MessageKey.ModesListInfo, keys);
        ReplyTone(cmd, MessageKey.ModesVoteHint);
    }

    private void CmdAdminSetMode(CCSPlayerController? player, CommandInfo cmd)
    {
        var key = (cmd.GetArg(1) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            ReplyTone(cmd, MessageKey.ErrorSetModeUsage);
            return;
        }

        if (!CanExecuteReload(player))
        {
            ReplyTone(cmd, MessageKey.ModeNoPermission);
            return;
        }

        if (!Config.Modes.TryGetValue(key, out var mode))
        {
            ReplyTone(cmd, MessageKey.ErrorModeNotFound, key);
            return;
        }

        if (IsModeAlreadyActive(mode))
        {
            ReplyTone(cmd, MessageKey.VoteAlreadyActiveMode, mode.DisplayName);
            return;
        }

        var targetMap = ResolveTargetMapForMode(mode);
        ScheduleModeSwitch(mode, "admin_mode_command", targetMap);
        ReplyTone(cmd, MessageKey.VoteConsoleScheduled, mode.DisplayName, targetMap);
    }

    private void RebuildModeCommands()
    {
        UnregisterModeCommands();
        RegisterModeCommandsFromConfig();
    }

    private void RegisterModeCommandsFromConfig()
    {
        foreach (KeyValuePair<string, ModeDefinition> entry in Config.Modes)
        {
            var key = (entry.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var mode = entry.Value;
            var safeKey = CommandNameSanitizer.ToSafeToken(key);
            var commandName = $"css_{safeKey}";

            CommandInfo.CommandCallback handler = (player, cmd) => HandleVote(player, cmd, mode);

            AddCommand(commandName, Msg(MessageKey.CmdDescDynamicMode, mode.DisplayName), handler);
            _dynamicCommands[commandName] = handler;
        }

        LogInfo(Msg(MessageKey.LogDynamicCommandsRegistered, _dynamicCommands.Count));
    }

    private void UnregisterModeCommands()
    {
        foreach (KeyValuePair<string, CommandInfo.CommandCallback> command in _dynamicCommands)
        {
            try
            {
                RemoveCommand(command.Key, command.Value);
            }
            catch
            {
                // Ignore stale command references during hot reload.
            }
        }

        _dynamicCommands.Clear();
    }
}
