using System;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class ModeManagerFeatureServices
{
    public ConfigFileLoader ConfigLoader { get; } = new("nModeManager", nameof(ModeManagerPlugin));

    public MessageLocalizer Messages { get; set; } = MessageLocalizer.Create("en");

    public ModeSwitcher? Switcher { get; set; }
    public VoteCoordinator? VoteCoordinator { get; set; }
    public MenuApiBridge? MenuApiBridge { get; set; }
    public RtvMenuFlow? RtvMenuFlow { get; set; }
    public ResetCfgProvisioner? ResetCfgProvisioner { get; set; }

    public VoteCoordinator GetOrCreateVoteCoordinator(
        Func<MessageKey, object?[], string> msg,
        Action<MessageKey, object?[]> chat,
        Action<ModeDefinition, string, string?> scheduleModeSwitch)
    {
        VoteCoordinator ??= new VoteCoordinator(msg, chat, scheduleModeSwitch);
        return VoteCoordinator;
    }

    public MenuApiBridge GetOrCreateMenuApiBridge(Action<string> logInfo, Func<MessageKey, string> msg)
    {
        MenuApiBridge ??= new MenuApiBridge(logInfo, msg);
        return MenuApiBridge;
    }

    public RtvMenuFlow GetOrCreateRtvMenuFlow(
        MenuApiBridge menuApiBridge,
        VoteCoordinator voteCoordinator,
        Func<ModeManagerConfig> getConfig,
        Func<MessageKey, object?[], string> msg,
        Action<CCSPlayerController, string> tellPlayer,
        Action<CCSPlayerController, ModeDefinition, string> submitVoteFromMenu,
        Action<float, Action> schedule)
    {
        RtvMenuFlow ??= new RtvMenuFlow(
            menuApiBridge,
            voteCoordinator,
            getConfig,
            msg,
            tellPlayer,
            submitVoteFromMenu,
            schedule);

        return RtvMenuFlow;
    }

    public ResetCfgProvisioner GetOrCreateResetCfgProvisioner(
        string moduleName,
        string typeName,
        Func<MessageKey, object?[], string> msg,
        Action<string> logInfo,
        Action<string> logError)
    {
        ResetCfgProvisioner ??= new ResetCfgProvisioner(
            moduleName,
            typeName,
            msg,
            logInfo,
            logError);

        return ResetCfgProvisioner;
    }
}
