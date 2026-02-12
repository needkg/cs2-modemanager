using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;

namespace ModeManager;

[MinimumApiVersion(80)]
public sealed partial class ModeManagerPlugin : BasePlugin, IPluginConfig<ModeManagerConfig>
{
    public override string ModuleName => "nModeManager";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "needkg";
    public override string ModuleDescription => "Production-ready CS2 mode manager with vote-based switching, delayed/cooldown execution, per-mode plugin/map/game settings, dynamic commands, localization, and safe live config reload.";

    public ModeManagerConfig Config { get; set; } = new();

    private readonly Dictionary<string, CommandInfo.CommandCallback> _dynamicCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConfigFileLoader _configLoader = new("nModeManager", nameof(ModeManagerPlugin));

    private bool _baseCommandsRegistered;
    private ModeSwitcher? _switcher;

    private VoteSession? _vote;
    private DateTime _cooldownUntilUtc = DateTime.MinValue;
    private PendingSwitch? _pending;
    private string? _activeModeKey;

    private bool _initialModeQueued;
    private string? _initialModeKeyQueued;
    private bool _initialModeApplied;

    public void OnConfigParsed(ModeManagerConfig config)
    {
        ConfigValidator.ValidateOrThrow(config);
        Config = config;
        SeedActiveModeFromConfigIfUnknown(config);
        ApplyLanguage(config.Language);
    }

    public override void Load(bool hotReload)
    {
        _switcher = new ModeSwitcher(new ServerCommandRunner());
        EnsureResetCfgFileExists();
        LogInfo(Msg(MessageKey.LogPluginLoaded, hotReload));
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        if (_switcher == null)
            return;

        RegisterBaseCommands();
        RebuildModeCommands();

        if (!Config.ApplyInitialModeOnStartup)
            return;

        _initialModeKeyQueued = (Config.InitialModeKey ?? string.Empty).Trim();
        _initialModeQueued = !_initialModeApplied && !string.IsNullOrWhiteSpace(_initialModeKeyQueued);

        if (!_initialModeQueued)
            return;

        LogInfo(Msg(MessageKey.LogInitialModeQueued, _initialModeKeyQueued));
        StartInitialModeWatcher();
    }

    public override void Unload(bool hotReload)
    {
        UnregisterModeCommands();
        CancelPendingSwitch("unload");
        _vote = null;
        LogInfo(Msg(MessageKey.LogPluginUnloaded, hotReload));
    }

    private void SeedActiveModeFromConfigIfUnknown(ModeManagerConfig config)
    {
        if (!string.IsNullOrWhiteSpace(_activeModeKey))
            return;

        var initialModeKey = (config.InitialModeKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(initialModeKey))
            return;

        if (!config.Modes.ContainsKey(initialModeKey))
            return;

        _activeModeKey = initialModeKey;
    }
}
