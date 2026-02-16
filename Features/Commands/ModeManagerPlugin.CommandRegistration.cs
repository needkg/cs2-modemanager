using System.Collections.Generic;
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
