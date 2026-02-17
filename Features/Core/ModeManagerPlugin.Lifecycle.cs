using CounterStrikeSharp.API.Core;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    public void OnConfigParsed(ModeManagerConfig config)
    {
        ConfigValidator.ValidateOrThrow(config);
        Config = config;
        _composition.State.SeedActiveModeFromConfigIfUnknown(config);
        ApplyLanguage(config.Language);
        LogDebug(MessageKey.LogDebugSettings, Config.DebugEnabled);
    }

    public override void Load(bool hotReload)
    {
        _switcher = new ModeSwitcher(
            new ServerCommandRunner(),
            command => LogDebug(MessageKey.LogDebugServerCommand, command));
        EnsureResetCfgFileExists();
        LogInfo(Msg(MessageKey.LogPluginLoaded, hotReload));
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        if (_switcher == null)
            return;

        EnsureMenuApiCapability();
        RegisterBaseCommands();
        RebuildModeCommands();

        if (!Config.ApplyInitialModeOnStartup)
            return;

        if (!_composition.State.QueueInitialMode(Config.InitialModeKey, requireNotApplied: true))
            return;

        LogInfo(Msg(MessageKey.LogInitialModeQueued, _initialModeKeyQueued));
        StartInitialModeWatcher();
    }

    public override void Unload(bool hotReload)
    {
        UnregisterModeCommands();
        CancelPendingSwitch("unload");
        ResetVotes();
        LogInfo(Msg(MessageKey.LogPluginUnloaded, hotReload));
    }
}
