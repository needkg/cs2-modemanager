namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private ResetCfgProvisioner ResetCfgProvisioner => _composition.Services.GetOrCreateResetCfgProvisioner(
        ModuleName,
        nameof(ModeManagerPlugin),
        (key, args) => Msg(key, args),
        LogInfo,
        LogError);

    private void EnsureResetCfgFileExists() => ResetCfgProvisioner.EnsureResetCfgFileExists(Config.ResetCommand);
}
