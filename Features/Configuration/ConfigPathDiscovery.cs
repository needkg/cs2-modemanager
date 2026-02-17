using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public static bool TryResolveServerFilePath(
        string relativeOrAbsolutePath,
        out string resolvedPath,
        out string searchedPaths)
    {
        resolvedPath = string.Empty;

        var candidates = GetCandidateServerFilePaths(relativeOrAbsolutePath);
        searchedPaths = candidates.Count == 0 ? "(none)" : string.Join(" | ", candidates);

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            resolvedPath = candidate;
            return true;
        }

        return false;
    }

    public static bool TryResolveServerFilePathForWrite(
        string relativeOrAbsolutePath,
        out string resolvedPath,
        out string searchedPaths)
    {
        resolvedPath = string.Empty;

        var candidates = GetCandidateServerFilePaths(relativeOrAbsolutePath);
        searchedPaths = candidates.Count == 0 ? "(none)" : string.Join(" | ", candidates);
        if (candidates.Count == 0)
            return false;

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            resolvedPath = candidate;
            return true;
        }

        resolvedPath = candidates[0];
        return true;
    }

    public static bool TryResolvePluginDll(string pluginName, out string resolvedPath, out string searchedPaths)
    {
        resolvedPath = string.Empty;

        var candidates = new List<string>(GetCommonPluginDllPaths(pluginName));
        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            resolvedPath = candidate;
            searchedPaths = string.Join(" | ", candidates);
            return true;
        }

        searchedPaths = candidates.Count == 0 ? "(none)" : string.Join(" | ", candidates);
        return false;
    }

    public static IEnumerable<string> GetCommonPluginDllPaths(string pluginName)
    {
        var normalizedName = NormalizePluginName(pluginName);
        if (string.IsNullOrWhiteSpace(normalizedName))
            yield break;

        foreach (var root in EnumerateCandidateRootsDistinct())
            yield return GetCanonicalPluginDllPathForRoot(root, normalizedName);
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

    private static string GetCanonicalPluginDllPathForRoot(string root, string pluginName)
    {
        return Path.Combine(
            root,
            "addons",
            "counterstrikesharp",
            "plugins",
            pluginName,
            $"{pluginName}.dll");
    }

    private static string NormalizePluginName(string pluginName)
    {
        return (pluginName ?? string.Empty).Trim().Trim('"');
    }

    private static List<string> GetCandidateServerFilePaths(string relativeOrAbsolutePath)
    {
        var normalized = (relativeOrAbsolutePath ?? string.Empty).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized))
            return new List<string>();

        if (Path.IsPathRooted(normalized) || normalized.Contains(':'))
        {
            try
            {
                return new List<string> { Path.GetFullPath(normalized) };
            }
            catch
            {
                return new List<string> { normalized };
            }
        }

        var normalizedRelative = normalized
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var candidates = new List<(string Path, int Score)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateCandidateRootsDistinct())
        {
            try
            {
                var candidate = Path.GetFullPath(Path.Combine(root, normalizedRelative));
                if (!seen.Add(candidate))
                    continue;

                var score = ComputeServerFileCandidateScore(root);
                candidates.Add((candidate, score));
            }
            catch
            {
                // Ignore malformed candidate paths.
            }
        }

        return candidates
            .OrderBy(item => item.Score)
            .ThenBy(item => item.Path.Length)
            .Select(item => item.Path)
            .ToList();
    }

    private static int ComputeServerFileCandidateScore(string root)
    {
        var score = 100;
        var normalizedRoot = root.Replace('\\', '/');

        if (normalizedRoot.EndsWith("/csgo", StringComparison.OrdinalIgnoreCase) ||
            normalizedRoot.Contains("/csgo/", StringComparison.OrdinalIgnoreCase))
            score -= 70;

        if (Directory.Exists(Path.Combine(root, "cfg")))
            score -= 40;

        if (Directory.Exists(Path.Combine(root, "addons", "counterstrikesharp")))
            score -= 30;

        return score;
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
