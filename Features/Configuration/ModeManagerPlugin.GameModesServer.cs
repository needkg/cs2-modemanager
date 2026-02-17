namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private GameModesServerProvisioner GameModesServerProvisioner => _composition.Services.GetOrCreateGameModesServerProvisioner(
        (key, args) => Msg(key, args),
        LogInfo,
        LogError);

    private string? TryPrepareEndMatchMapVoteForMode(ModeDefinition mode)
    {
        if (!Config.EndMatchMapVoteEnabled)
            return null;

        if (!GameModesServerProvisioner.TrySyncEndMatchMapVote(Config, mode, out var mapGroupName))
            return null;

        return string.IsNullOrWhiteSpace(mapGroupName) ? null : mapGroupName;
    }
}
