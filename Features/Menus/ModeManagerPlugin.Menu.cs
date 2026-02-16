using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private MenuApiBridge? _menuApiBridge;
    private const float MenuTransitionDelaySeconds = 0.05f;

    private MenuApiBridge MenuBridge => _menuApiBridge ??= new MenuApiBridge(LogInfo, Msg);

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

        if (Votes.TryReplyActiveVoteStatusForVoter(player, msg => ReplyTone(cmd, msg)))
            return;

        QueueMenuTransition(player, () =>
        {
            Votes.CleanupExpiredVote();
            MenuBridge.TryCloseMenuForPlayer(player);

            if (Votes.TryGetActiveVoteMode(Config, out var activeVoteMode))
            {
                if (!TryOpenMapVoteMenu(player, activeVoteMode))
                    TellTone(player, MessageKey.MenuOpenFailed);

                return;
            }

            if (!TryOpenModeVoteMenu(player))
                TellTone(player, MessageKey.MenuOpenFailed);
        });
        ReplyTone(cmd, MessageKey.RtvMenuOpened);
    }

    private bool TryOpenModeVoteMenu(CCSPlayerController player)
    {
        if (!MenuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleModes), out var menu) || menu == null)
            return false;

        foreach (var entry in Config.Modes)
        {
            var modeKey = (entry.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(modeKey))
                continue;

            var mode = entry.Value;
            if (mode == null)
                continue;

            var capturedModeKey = modeKey;
            var optionText = $"{mode.DisplayName}";
            if (!MenuBridge.TryAddMenuOption(menu, optionText, p => OpenMapVoteMenuFromMode(p, capturedModeKey)))
                return false;
        }

        if (!MenuBridge.TryOpenMenu(menu, player))
        {
            TellTone(player, MessageKey.MenuOpenFailed);
            return false;
        }

        return true;
    }

    private void OpenMapVoteMenuFromMode(CCSPlayerController player, string modeKey)
    {
        if (!player.IsValid)
            return;

        if (!Config.Modes.TryGetValue(modeKey, out var mode) || mode == null)
        {
            TellTone(player, MessageKey.ErrorModeNotFound, modeKey);
            return;
        }

        QueueMenuTransition(player, () =>
        {
            MenuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenMapVoteMenu(player, mode))
                TellTone(player, MessageKey.MenuOpenFailed);
        });
    }

    private bool TryOpenMapVoteMenu(CCSPlayerController player, ModeDefinition mode)
    {
        if (!MenuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleMaps, mode.DisplayName), out var menu) || menu == null)
            return false;

        var selectableMaps = GetSelectableMapsForMode(mode);
        if (selectableMaps.Count == 0)
            return false;

        foreach (var map in selectableMaps)
        {
            var capturedMap = map;
            if (!MenuBridge.TryAddMenuOption(menu, capturedMap, p => OpenVoteConfirmMenuFromMap(p, mode, capturedMap)))
                return false;
        }

        if (!MenuBridge.TryOpenMenu(menu, player))
            return false;

        return true;
    }

    private void OpenVoteConfirmMenuFromMap(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            MenuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenVoteConfirmMenu(player, mode, selectedMap))
                TellTone(player, MessageKey.MenuOpenFailed);
        });
    }

    private bool TryOpenVoteConfirmMenu(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!MenuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleConfirm, mode.DisplayName, selectedMap), out var menu) || menu == null)
            return false;

        if (!MenuBridge.TryAddMenuOption(menu, Msg(MessageKey.MenuOptionConfirmVote), p => ConfirmModeVoteFromMenu(p, mode, selectedMap)))
            return false;

        if (!MenuBridge.TryAddMenuOption(menu, Msg(MessageKey.MenuOptionBackToMaps), p => GoBackToMapSelection(p, mode)))
            return false;

        return MenuBridge.TryOpenMenu(menu, player);
    }

    private void ConfirmModeVoteFromMenu(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            MenuBridge.TryCloseMenuForPlayer(player);
            HandleVoteFromMenu(player, mode, selectedMap);
        });
    }

    private void GoBackToMapSelection(CCSPlayerController player, ModeDefinition mode)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            MenuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenMapVoteMenu(player, mode))
                TellTone(player, MessageKey.MenuOpenFailed);
        });
    }

    private void QueueMenuTransition(CCSPlayerController player, Action action)
    {
        AddTimer(MenuTransitionDelaySeconds, () =>
        {
            if (player == null || !player.IsValid)
                return;

            action();
        });
    }
}
