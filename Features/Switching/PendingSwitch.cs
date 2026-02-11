namespace ModeManager;

internal sealed class PendingSwitch
{
    public PendingSwitch(ModeDefinition mode, string reason)
    {
        Mode = mode;
        Reason = reason;
    }

    public ModeDefinition Mode { get; }
    public string Reason { get; }
    public object? TimerHandle { get; set; }
}
