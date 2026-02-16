using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
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
            var keys = string.Join(", ", Config.Modes.Keys);
            ReplyTone(cmd, MessageKey.ModesListInfo, keys);
            ReplyTone(cmd, MessageKey.ErrorSetModeUsage);
            return;
        }

        var requestedMap = (cmd.GetArg(2) ?? string.Empty).Trim();
        var hasExplicitMapSelection = !string.IsNullOrWhiteSpace(requestedMap);

        if (!AdminAccessPolicy.CanExecuteRootAction(player))
        {
            ReplyTone(cmd, MessageKey.ModeNoPermission);
            return;
        }

        if (!Config.Modes.TryGetValue(key, out var mode))
        {
            ReplyTone(cmd, MessageKey.ErrorModeNotFound, key);
            return;
        }

        if (!TryResolveTargetMapForMode(
                mode,
                requestedMap: hasExplicitMapSelection ? requestedMap : null,
                hasExplicitMapSelection: hasExplicitMapSelection,
                out var targetMap))
        {
            ReplyTone(cmd, MessageKey.VoteMapSelectionInvalid);

            if (hasExplicitMapSelection)
            {
                var selectableMaps = GetSelectableMapsForMode(mode);
                if (selectableMaps.Count > 0)
                {
                    ReplyTone(
                        cmd,
                        MessageKey.VoteMapSelectionAvailableMaps,
                        mode.DisplayName,
                        string.Join(", ", selectableMaps));
                }
            }

            return;
        }

        if (IsModeAlreadyActive(mode) &&
            (!hasExplicitMapSelection || IsTargetMapCurrent(targetMap)))
        {
            ReplyTone(cmd, MessageKey.VoteAlreadyActiveMode, mode.DisplayName);
            return;
        }

        ScheduleModeSwitch(mode, "admin_mode_command", targetMap);
        ReplyTone(cmd, MessageKey.VoteConsoleScheduled, mode.DisplayName, targetMap);
    }
}
