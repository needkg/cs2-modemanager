using System;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class RtvMenuFlow
{
    private readonly MenuApiBridge _menuBridge;
    private readonly VoteCoordinator _votes;
    private readonly Func<ModeManagerConfig> _getConfig;
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<CCSPlayerController, string> _tell;
    private readonly Action<CCSPlayerController, ModeDefinition, string> _submitVoteFromMenu;
    private readonly Action<float, Action> _schedule;
    private readonly float _menuTransitionDelaySeconds;

    public RtvMenuFlow(
        MenuApiBridge menuBridge,
        VoteCoordinator votes,
        Func<ModeManagerConfig> getConfig,
        Func<MessageKey, object?[], string> msg,
        Action<CCSPlayerController, string> tell,
        Action<CCSPlayerController, ModeDefinition, string> submitVoteFromMenu,
        Action<float, Action> schedule,
        float menuTransitionDelaySeconds = 0.05f)
    {
        _menuBridge = menuBridge;
        _votes = votes;
        _getConfig = getConfig;
        _msg = msg;
        _tell = tell;
        _submitVoteFromMenu = submitVoteFromMenu;
        _schedule = schedule;
        _menuTransitionDelaySeconds = menuTransitionDelaySeconds;
    }

    public void OpenFromCommand(CCSPlayerController player, Action<string> reply)
    {
        if (_votes.TryReplyActiveVoteStatusForVoter(player, reply))
            return;

        QueueMenuTransition(player, () =>
        {
            _votes.CleanupExpiredVote();
            _menuBridge.TryCloseMenuForPlayer(player);

            if (_votes.TryGetActiveVoteMode(_getConfig(), out var activeVoteMode))
            {
                if (!TryOpenMapVoteMenu(player, activeVoteMode))
                    Tell(player, MessageKey.MenuOpenFailed);

                return;
            }

            if (!TryOpenModeVoteMenu(player))
                Tell(player, MessageKey.MenuOpenFailed);
        });

        reply(Msg(MessageKey.RtvMenuOpened));
    }

    private bool TryOpenModeVoteMenu(CCSPlayerController player)
    {
        if (!_menuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleModes), out var menu) || menu == null)
            return false;

        foreach (var entry in _getConfig().Modes)
        {
            var modeKey = (entry.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(modeKey))
                continue;

            var mode = entry.Value;
            if (mode == null)
                continue;

            var capturedModeKey = modeKey;
            var optionText = $"{mode.DisplayName}";
            if (!_menuBridge.TryAddMenuOption(menu, optionText, p => OpenMapVoteMenuFromMode(p, capturedModeKey)))
                return false;
        }

        if (!_menuBridge.TryOpenMenu(menu, player))
        {
            Tell(player, MessageKey.MenuOpenFailed);
            return false;
        }

        return true;
    }

    private void OpenMapVoteMenuFromMode(CCSPlayerController player, string modeKey)
    {
        if (!player.IsValid)
            return;

        if (!_getConfig().Modes.TryGetValue(modeKey, out var mode) || mode == null)
        {
            Tell(player, MessageKey.ErrorModeNotFound, modeKey);
            return;
        }

        QueueMenuTransition(player, () =>
        {
            _menuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenMapVoteMenu(player, mode))
                Tell(player, MessageKey.MenuOpenFailed);
        });
    }

    private bool TryOpenMapVoteMenu(CCSPlayerController player, ModeDefinition mode)
    {
        if (!_menuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleMaps, mode.DisplayName), out var menu) || menu == null)
            return false;

        var selectableMaps = _votes.GetSelectableMapsForMode(mode);
        if (selectableMaps.Count == 0)
            return false;

        foreach (var map in selectableMaps)
        {
            var capturedMap = map;
            if (!_menuBridge.TryAddMenuOption(menu, capturedMap, p => OpenVoteConfirmMenuFromMap(p, mode, capturedMap)))
                return false;
        }

        if (!_menuBridge.TryOpenMenu(menu, player))
            return false;

        return true;
    }

    private void OpenVoteConfirmMenuFromMap(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            _menuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenVoteConfirmMenu(player, mode, selectedMap))
                Tell(player, MessageKey.MenuOpenFailed);
        });
    }

    private bool TryOpenVoteConfirmMenu(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!_menuBridge.TryCreateMenu(Msg(MessageKey.MenuTitleConfirm, mode.DisplayName, selectedMap), out var menu) || menu == null)
            return false;

        if (!_menuBridge.TryAddMenuOption(menu, Msg(MessageKey.MenuOptionConfirmVote), p => ConfirmModeVoteFromMenu(p, mode, selectedMap)))
            return false;

        if (!_menuBridge.TryAddMenuOption(menu, Msg(MessageKey.MenuOptionBackToMaps), p => GoBackToMapSelection(p, mode)))
            return false;

        return _menuBridge.TryOpenMenu(menu, player);
    }

    private void ConfirmModeVoteFromMenu(CCSPlayerController player, ModeDefinition mode, string selectedMap)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            _menuBridge.TryCloseMenuForPlayer(player);
            _submitVoteFromMenu(player, mode, selectedMap);
        });
    }

    private void GoBackToMapSelection(CCSPlayerController player, ModeDefinition mode)
    {
        if (!player.IsValid)
            return;

        QueueMenuTransition(player, () =>
        {
            _menuBridge.TryCloseMenuForPlayer(player);
            if (!TryOpenMapVoteMenu(player, mode))
                Tell(player, MessageKey.MenuOpenFailed);
        });
    }

    private void QueueMenuTransition(CCSPlayerController player, Action action)
    {
        _schedule(_menuTransitionDelaySeconds, () =>
        {
            if (!player.IsValid)
                return;

            action();
        });
    }

    private string Msg(MessageKey key, params object?[] args) => _msg(key, args);

    private void Tell(CCSPlayerController player, MessageKey key, params object?[] args)
        => _tell(player, Msg(key, args));
}
