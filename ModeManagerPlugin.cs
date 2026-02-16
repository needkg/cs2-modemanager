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

    private readonly ModeManagerCompositionRoot _composition = new();

    private Dictionary<string, CommandInfo.CommandCallback> _dynamicCommands => _composition.State.DynamicCommands;
    private ConfigFileLoader _configLoader => _composition.Services.ConfigLoader;

    private bool _baseCommandsRegistered
    {
        get => _composition.State.BaseCommandsRegistered;
        set => _composition.State.BaseCommandsRegistered = value;
    }

    private ModeSwitcher? _switcher
    {
        get => _composition.Services.Switcher;
        set => _composition.Services.Switcher = value;
    }

    private DateTime _cooldownUntilUtc
    {
        get => _composition.State.CooldownUntilUtc;
        set => _composition.State.CooldownUntilUtc = value;
    }

    private PendingSwitch? _pending
    {
        get => _composition.State.PendingSwitch;
        set => _composition.State.PendingSwitch = value;
    }

    private string? _activeModeKey
    {
        get => _composition.State.ActiveModeKey;
        set => _composition.State.ActiveModeKey = value;
    }

    private bool _initialModeQueued
    {
        get => _composition.State.InitialModeQueued;
        set => _composition.State.InitialModeQueued = value;
    }

    private string? _initialModeKeyQueued
    {
        get => _composition.State.InitialModeKeyQueued;
        set => _composition.State.InitialModeKeyQueued = value;
    }

    private bool _initialModeApplied
    {
        get => _composition.State.InitialModeApplied;
        set => _composition.State.InitialModeApplied = value;
    }

    private MessageLocalizer _messages
    {
        get => _composition.Services.Messages;
        set => _composition.Services.Messages = value;
    }
}
