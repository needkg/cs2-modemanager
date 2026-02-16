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
        if (player == null || !player.IsValid)
        {
            ReplyTone(cmd, MessageKey.ErrorInvalidPlayer);
            return;
        }

        MenuFlow.OpenFromCommand(player, msg => ReplyTone(cmd, msg));
    }
}
