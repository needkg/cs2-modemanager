namespace ModeManager;

internal enum MessageKey
{
    // Plugin lifecycle / logs
    LogPluginLoaded,
    LogPluginUnloaded,
    LogLanguageSet,
    LogInitialModeQueued,
    LogInitialModeKeyNotFound,
    LogInitialModeScheduled,
    LogDynamicCommandsRegistered,
    LogConfigReloaded,
    LogReloadConfigNotFound,
    LogReloadException,
    LogSwitchScheduled,
    LogPendingCanceled,
    LogSwitcherNotInitialized,
    LogApplyingMode,
    LogModeApplied,
    LogModeApplyFailed,

    // Command descriptions
    CmdDescHelp,
    CmdDescModes,
    CmdDescSetMode,
    CmdDescVoteStatus,
    CmdDescReload,
    CmdDescDynamicMode,

    // Help / command replies
    HelpTitle,
    HelpCommandsLabel,
    HelpLineMm,
    HelpLineModes,
    HelpLineSetMode,
    HelpLineDynamicMode,
    HelpLineVoteStatus,
    HelpLineReload,
    HelpModesList,
    ModesListInfo,
    ModesVoteHint,
    VoteStatusPendingSwitch,
    VoteStatusNone,
    VoteStatusActive,
    ErrorSetModeUsage,
    ErrorModeNotFound,

    // Voting
    VoteConsoleScheduled,
    ErrorInvalidPlayer,
    VotePendingAlready,
    VoteCooldown,
    VoteIneligible,
    VoteIdentityMissing,
    VoteAlreadyActiveMode,
    VoteMinPlayers,
    VoteStartedChat,
    VoteRegisteredSelf,
    VoteAlreadyCast,
    VoteRegisteredChat,
    VoteExpiredChat,

    // Switching
    SwitchApprovedNow,
    SwitchApprovedIn,
    ChatSwitcherNotInitialized,
    ChatModeChanged,
    ChatModeApplyFailed,

    // Config reload
    ReloadNoPermission,
    ReloadConfigSuccess,
    ReloadConfigNotFound,
    ReloadCommandsRebuilt,
    ReloadUseHelp,
    ReloadFailed,

    // Loader / validator
    LoaderConfigNotFound,
    LoaderDeserializeNull,
    ValidationLanguageUnsupported,
    ValidationModesRequired,
    ValidationResetCommandRequired,
    ValidationVoteRatioRange,
    ValidationVoteMinPlayersRange,
    ValidationVoteDurationRange,
    ValidationSwitchCooldownRange,
    ValidationSwitchDelayRange,
    ValidationModeNull,
    ValidationExecCommandRequired,
    ValidationDefaultMapInvalid,
    ValidationGameTypeInvalid,
    ValidationGameModeInvalid,

    // Startup chat
    ChatInitialModeScheduled
}
