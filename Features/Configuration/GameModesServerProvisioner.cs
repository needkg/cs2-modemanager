using System;

namespace ModeManager;

internal sealed class GameModesServerProvisioner
{
    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logError;
    private readonly GameModesServerFileStore _fileStore;
    private readonly GameModesServerVdfEditor _vdfEditor;
    private readonly EndMatchMapVoteMapPoolBuilder _mapPoolBuilder;

    public GameModesServerProvisioner(
        Func<MessageKey, object?[], string> msg,
        Action<string> logInfo,
        Action<string> logError)
    {
        _msg = msg;
        _logInfo = logInfo;
        _logError = logError;
        _fileStore = new GameModesServerFileStore();
        _vdfEditor = new GameModesServerVdfEditor();
        _mapPoolBuilder = new EndMatchMapVoteMapPoolBuilder(mode => ModeSwitcher.ResolveTargetMap(mode));
    }

    public bool TrySyncEndMatchMapVote(ModeManagerConfig config, ModeDefinition mode, out string mapGroupName)
    {
        mapGroupName = string.Empty;

        if (!config.EndMatchMapVoteEnabled)
            return true;

        try
        {
            if (!_fileStore.TryResolveFilePath(out var filePath, out var searchedPaths))
            {
                _logError(_msg(
                    MessageKey.LogEndMatchMapVoteSyncFailed,
                    new object?[]
                    {
                        mode.Key,
                        _msg(MessageKey.ValidationEndMatchMapVoteFileNotFound, new object?[] { searchedPaths })
                    }));

                return false;
            }

            var initialContent = _vdfEditor.BuildInitialContent(config, _mapPoolBuilder);
            if (!_fileStore.TryEnsureFileExists(filePath, initialContent, out var ensureError))
            {
                _logError(_msg(
                    MessageKey.LogEndMatchMapVoteSyncFailed,
                    new object?[] { mode.Key, ensureError }));
                return false;
            }

            var maps = _mapPoolBuilder.BuildForSync(mode);
            if (maps.Count == 0)
            {
                _logInfo(_msg(
                    MessageKey.LogEndMatchMapVoteSyncSkipped,
                    new object?[] { mode.Key, "no valid maps for this mode" }));
                return false;
            }

            var fileContent = _fileStore.ReadAllText(filePath);
            var serialized = _vdfEditor.ApplyMode(fileContent, mode, maps, out mapGroupName);
            if (!string.Equals(
                    NormalizeLineEndings(fileContent),
                    NormalizeLineEndings(serialized),
                    StringComparison.Ordinal))
            {
                _fileStore.WriteAllText(filePath, serialized);
                _logInfo(_msg(
                    MessageKey.LogEndMatchMapVoteSyncSuccess,
                    new object?[] { filePath, mapGroupName, maps.Count }));
            }
            else
            {
                _logInfo(_msg(
                    MessageKey.LogEndMatchMapVoteSyncSkipped,
                    new object?[] { mode.Key, "already up to date" }));
            }

            return true;
        }
        catch (Exception ex)
        {
            mapGroupName = string.Empty;
            _logError(_msg(
                MessageKey.LogEndMatchMapVoteSyncFailed,
                new object?[] { mode.Key, ex.Message }));
            return false;
        }
    }

    private static string NormalizeLineEndings(string content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
}
