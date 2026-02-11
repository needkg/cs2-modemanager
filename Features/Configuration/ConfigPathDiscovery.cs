using System;
using System.Collections.Generic;
using System.IO;

namespace ModeManager;

internal static class ConfigPathDiscovery
{
    public static string? Discover(string moduleName, string typeName)
    {
        _ = typeName;

        foreach (var root in EnumerateCandidateRootsDistinct())
        {
            var path = GetCanonicalPathForRoot(root, moduleName);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    public static IEnumerable<string> GetCommonPaths(string moduleName, string typeName)
    {
        _ = typeName;

        foreach (var root in EnumerateCandidateRootsDistinct())
            yield return GetCanonicalPathForRoot(root, moduleName);
    }

    private static string GetCanonicalPathForRoot(string root, string moduleName)
    {
        return Path.Combine(
            root,
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            moduleName,
            $"{moduleName}.json");
    }

    private static IEnumerable<string> EnumerateCandidateRootsDistinct()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateCandidateRoots())
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(root);
            }
            catch
            {
                continue;
            }

            if (seen.Add(fullPath))
                yield return fullPath;
        }
    }

    private static IEnumerable<string> EnumerateCandidateRoots()
    {
        var cwd = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(cwd);
        var depth = 0;

        while (directory != null && depth++ < 10)
        {
            yield return directory.FullName;

            var csgoPath = Path.Combine(directory.FullName, "csgo");
            if (Directory.Exists(csgoPath))
                yield return csgoPath;

            directory = directory.Parent;
        }

        const string marker = "/game/";
        var markerIndex = cwd.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            yield break;

        var gameBase = cwd.Substring(0, markerIndex) + "/game";
        if (Directory.Exists(gameBase))
            yield return gameBase;

        var csgoGamePath = Path.Combine(gameBase, "csgo");
        if (Directory.Exists(csgoGamePath))
            yield return csgoGamePath;
    }
}
