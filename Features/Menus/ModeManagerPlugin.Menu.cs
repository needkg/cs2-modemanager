using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private MenuApiBridge MenuBridge => _composition.Services.GetOrCreateMenuApiBridge(LogInfo, Msg);
    private RtvMenuFlow MenuFlow => _composition.Services.GetOrCreateRtvMenuFlow(
        menuApiBridge: MenuBridge,
        voteCoordinator: Votes,
        getConfig: () => Config,
        msg: (key, args) => Msg(key, args),
        tellPlayer: TellTone,
        submitVoteFromMenu: HandleVoteFromMenu,
        schedule: (delay, action) => AddTimer(delay, action));

    private void EnsureMenuApiCapability()
    {
        if (MenuBridge.TryResolve())
            return;

        var error = Msg(MessageKey.LogMenuApiMissing);
        LogError(error);
        throw new InvalidOperationException(error);
    }

    private void CmdOpenRtvMenu(CCSPlayerController? player, CommandInfo cmd)
    {
        var subcommand = (cmd.GetArg(1) ?? string.Empty).Trim();
        var isToggleSubcommand =
            subcommand.Equals("enable", StringComparison.OrdinalIgnoreCase) ||
            subcommand.Equals("disable", StringComparison.OrdinalIgnoreCase);

        if (isToggleSubcommand)
        {
            HandleRtvToggleCommand(player, cmd, subcommand);
            return;
        }

        if (!string.IsNullOrWhiteSpace(subcommand) &&
            (player == null || AdminAccessPolicy.CanExecuteRootAction(player)))
        {
            ReplyTone(cmd, MessageKey.RtvToggleUsage);
            return;
        }

        if (!_rtvEnabled)
        {
            ReplyTone(cmd, MessageKey.RtvDisabled);
            return;
        }

        if (player == null || !player.IsValid)
        {
            ReplyTone(cmd, MessageKey.ErrorInvalidPlayer);
            return;
        }

        MenuFlow.OpenFromCommand(player, msg => ReplyTone(cmd, msg));
    }

    private void HandleRtvToggleCommand(CCSPlayerController? player, CommandInfo cmd, string subcommand)
    {
        if (!AdminAccessPolicy.CanExecuteRootAction(player))
        {
            ReplyTone(cmd, MessageKey.RtvToggleNoPermission);
            return;
        }

        if (subcommand.Equals("enable", StringComparison.OrdinalIgnoreCase))
        {
            _rtvEnabled = true;
            ReplyTone(cmd, MessageKey.RtvToggleEnabled);
            return;
        }

        if (subcommand.Equals("disable", StringComparison.OrdinalIgnoreCase))
        {
            _rtvEnabled = false;
            ReplyTone(cmd, MessageKey.RtvToggleDisabled);
            return;
        }

        ReplyTone(cmd, MessageKey.RtvToggleUsage);
    }
}
