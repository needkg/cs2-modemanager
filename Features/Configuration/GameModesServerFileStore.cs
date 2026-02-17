using System;
using System.IO;
using System.Text;

namespace ModeManager;

internal sealed class GameModesServerFileStore
{
    private const string EndMatchMapVoteFileName = "gamemodes_server.txt";

    public bool TryResolveFilePath(out string filePath, out string searchedPaths)
    {
        return ConfigPathDiscovery.TryResolveServerFilePathForWrite(
            EndMatchMapVoteFileName,
            out filePath,
            out searchedPaths);
    }

    public bool TryEnsureFileExists(string filePath, string initialContent, out string error)
    {
        error = string.Empty;

        if (File.Exists(filePath))
            return true;

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            WriteAllText(filePath, initialContent);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public string ReadAllText(string filePath)
    {
        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    public void WriteAllText(string filePath, string content)
    {
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
