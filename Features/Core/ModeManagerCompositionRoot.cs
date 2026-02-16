namespace ModeManager;

internal sealed class ModeManagerCompositionRoot
{
    public ModeManagerRuntimeState State { get; } = new();
    public ModeManagerFeatureServices Services { get; } = new();
}
