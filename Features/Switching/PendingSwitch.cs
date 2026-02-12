namespace ModeManager;

internal sealed class PendingSwitch
{
    public PendingSwitch(ModeDefinition mode, string reason, string? targetMap = null)
    {
        Mode = mode;
        Reason = reason;
        TargetMap = targetMap;
    }

    public ModeDefinition Mode { get; }
    public string Reason { get; }
    public string? TargetMap { get; }
    public object? TimerHandle { get; set; }
}
