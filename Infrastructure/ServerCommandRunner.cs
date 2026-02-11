using CounterStrikeSharp.API;

namespace ModeManager;

internal sealed class ServerCommandRunner : IServerCommandRunner
{
    public void Run(string command)
    {
        if (!string.IsNullOrWhiteSpace(command))
            Server.ExecuteCommand(command);
    }
}
