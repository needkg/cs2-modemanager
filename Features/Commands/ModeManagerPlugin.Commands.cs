using System;
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

        AddCommand("css_nmm_setmode", Msg(MessageKey.CmdDescSetMode), CmdVoteSetMode);
        AddCommand("css_nmm_vote", Msg(MessageKey.CmdDescVoteStatus), CmdVoteStatus);
        AddCommand("css_nmm_reload", Msg(MessageKey.CmdDescReload), CmdReloadAll);

        _baseCommandsRegistered = true;
    }

    private void CmdHelp(CCSPlayerController? player, CommandInfo cmd)
    {
        cmd.ReplyToCommand(Msg(MessageKey.HelpTitle));
        cmd.ReplyToCommand(Msg(MessageKey.HelpCommandsLabel));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineMm));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineModes));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineSetMode));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineDynamicMode));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineVoteStatus));
        cmd.ReplyToCommand(Msg(MessageKey.HelpLineReload));

        var keys = string.Join(", ", Config.Modes.Keys);
        cmd.ReplyToCommand(Msg(MessageKey.HelpModesList, keys));
    }

    private void CmdListModes(CCSPlayerController? player, CommandInfo cmd)
    {
        var keys = string.Join(", ", Config.Modes.Keys);
        cmd.ReplyToCommand(Msg(MessageKey.ModesListInfo, keys));
        cmd.ReplyToCommand(Msg(MessageKey.ModesVoteHint));
    }

    private void CmdVoteStatus(CCSPlayerController? player, CommandInfo cmd)
    {
        if (_pending != null)
            cmd.ReplyToCommand(Msg(MessageKey.VoteStatusPendingSwitch, _pending.Mode.DisplayName, Config.SwitchDelaySeconds));

        var vote = _vote;
        if (vote == null)
        {
            cmd.ReplyToCommand(Msg(MessageKey.VoteStatusNone));
            return;
        }

        var remaining = (int)Math.Max(0, (vote.ExpiresUtc - DateTime.UtcNow).TotalSeconds);
        cmd.ReplyToCommand(Msg(MessageKey.VoteStatusActive, vote.ModeKey, vote.VoterIds.Count, vote.RequiredVotes, remaining));
    }

    private void CmdVoteSetMode(CCSPlayerController? player, CommandInfo cmd)
    {
        var key = (cmd.GetArg(1) ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            cmd.ReplyToCommand(Msg(MessageKey.ErrorSetModeUsage));
            return;
        }

        if (!Config.Modes.TryGetValue(key, out var mode))
        {
            cmd.ReplyToCommand(Msg(MessageKey.ErrorModeNotFound, key));
            return;
        }

        HandleVote(player, cmd, mode);
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
