using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace ModeManager;

internal sealed class GameModesServerProvisioner
{
    private const string EndMatchMapVoteFileName = "gamemodes_server.txt";
    private const string EndMatchMapVoteMapgroupPrefix = "mg_nmm_";

    private static readonly IReadOnlyDictionary<int, string> _gameTypeNames = new Dictionary<int, string>
    {
        [0] = "classic",
        [1] = "gungame",
        [2] = "training",
        [3] = "custom"
    };

    private static readonly IReadOnlyDictionary<(int GameType, int GameMode), string> _gameModeNames =
        new Dictionary<(int GameType, int GameMode), string>
        {
            [(0, 0)] = "casual",
            [(0, 1)] = "competitive",
            [(0, 2)] = "scrimcomp2v2",
            [(0, 3)] = "deathmatch",
            [(1, 0)] = "gungameprogressive",
            [(1, 1)] = "gungametrbomb",
            [(2, 0)] = "training",
            [(3, 0)] = "custom"
        };

    private readonly Func<MessageKey, object?[], string> _msg;
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logError;

    public GameModesServerProvisioner(
        Func<MessageKey, object?[], string> msg,
        Action<string> logInfo,
        Action<string> logError)
    {
        _msg = msg;
        _logInfo = logInfo;
        _logError = logError;
    }

    public bool TrySyncEndMatchMapVote(ModeManagerConfig config, ModeDefinition mode, out string mapGroupName)
    {
        mapGroupName = string.Empty;

        if (!config.EndMatchMapVoteEnabled)
            return true;

        try
        {
            if (!ConfigPathDiscovery.TryResolveServerFilePathForWrite(
                    EndMatchMapVoteFileName,
                    out var filePath,
                    out var searchedPaths))
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

            if (!TryEnsureGameModesServerFileExists(filePath, config, out var ensureError))
            {
                _logError(_msg(
                    MessageKey.LogEndMatchMapVoteSyncFailed,
                    new object?[] { mode.Key, ensureError }));
                return false;
            }

            var maps = BuildMapsForMode(mode);
            if (maps.Count == 0)
            {
                _logInfo(_msg(
                    MessageKey.LogEndMatchMapVoteSyncSkipped,
                    new object?[] { mode.Key, "no valid maps for this mode" }));
                return false;
            }

            mapGroupName = BuildModeMapGroupName(mode.Key);

            var fileContent = File.ReadAllText(filePath, Encoding.UTF8);
            var parsedRoot = VdfConvert.Deserialize(fileContent);
            if (parsedRoot.Value is not VObject rootObject)
                throw new InvalidOperationException("VDF root does not contain an object.");

            EnsureMapGroup(rootObject, mapGroupName, mode.DisplayName, maps);
            EnsureMapGroupBindingForGameMode(
                rootObject,
                mapGroupName,
                mode.GameType ?? 0,
                mode.GameMode ?? 0);

            var serialized = VdfConvert.Serialize(parsedRoot);
            if (!string.Equals(
                    NormalizeLineEndings(fileContent),
                    NormalizeLineEndings(serialized),
                    StringComparison.Ordinal))
            {
                File.WriteAllText(filePath, serialized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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

    private static bool TryEnsureGameModesServerFileExists(
        string filePath,
        ModeManagerConfig config,
        out string error)
    {
        error = string.Empty;

        if (File.Exists(filePath))
            return true;

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var initialContent = BuildInitialGameModesServerContent(config);
            File.WriteAllText(filePath, initialContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string BuildInitialGameModesServerContent(ModeManagerConfig config)
    {
        var root = new VObject();
        root.Add("gameTypes", new VObject());
        root.Add("mapgroups", new VObject());

        foreach (var modeEntry in config.Modes)
        {
            var mode = modeEntry.Value;
            if (mode == null)
                continue;

            var modeKey = string.IsNullOrWhiteSpace(mode.Key) ? modeEntry.Key : mode.Key;
            var maps = BuildConfiguredMapsForMode(mode);
            if (maps.Count == 0)
                continue;

            var mapGroupName = BuildModeMapGroupName(modeKey);
            EnsureMapGroup(root, mapGroupName, mode.DisplayName, maps);
        }

        var document = new VProperty("GameModes_Server.txt", root);
        return VdfConvert.Serialize(document);
    }

    private static IReadOnlyList<string> BuildConfiguredMapsForMode(ModeDefinition mode)
    {
        var maps = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in mode.MapPool)
            TryAddMap(map, seen, maps);

        TryAddMap(mode.DefaultMap, seen, maps);
        return maps;
    }

    private static IReadOnlyList<string> BuildMapsForMode(ModeDefinition mode)
    {
        var maps = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in mode.MapPool)
            TryAddMap(map, seen, maps);

        TryAddMap(mode.DefaultMap, seen, maps);

        if (maps.Count == 0)
            TryAddMap(ModeSwitcher.ResolveTargetMap(mode), seen, maps);

        return maps;
    }

    private static void TryAddMap(string? rawMap, ISet<string> seen, ICollection<string> maps)
    {
        var map = NormalizeMap(rawMap);
        if (map == null)
            return;

        if (seen.Add(map))
            maps.Add(map);
    }

    private static string? NormalizeMap(string? map)
    {
        var normalized = (map ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string BuildModeMapGroupName(string modeKey)
    {
        return $"{EndMatchMapVoteMapgroupPrefix}{CommandNameSanitizer.ToSafeToken(modeKey)}";
    }

    private static void EnsureMapGroup(VObject root, string mapGroupName, string displayName, IReadOnlyList<string> maps)
    {
        var mapGroups = GetOrCreateObject(root, "mapgroups");
        var mapGroup = GetOrCreateObject(mapGroups, mapGroupName);

        SetString(mapGroup, "name", mapGroupName);
        SetString(
            mapGroup,
            "displayname",
            string.IsNullOrWhiteSpace(displayName) ? mapGroupName : displayName.Trim());

        var mapsObject = GetOrCreateObject(mapGroup, "maps");
        mapsObject.Clear();

        foreach (var map in maps)
            mapsObject.Add(map, new VValue(string.Empty));
    }

    private static void EnsureMapGroupBindingForGameMode(
        VObject root,
        string mapGroupName,
        int gameType,
        int gameMode)
    {
        var gameTypes = GetOrCreateObject(root, "gameTypes");
        var gameTypeNode = GetOrCreateGameTypeNode(gameTypes, gameType);
        var gameModes = GetOrCreateObject(gameTypeNode, "gameModes");
        var gameModeNode = GetOrCreateGameModeNode(gameModes, gameType, gameMode);
        var mapGroupsMp = GetOrCreateObject(gameModeNode, "mapgroupsMP");

        RemovePreviousModeMapGroups(mapGroupsMp, EndMatchMapVoteMapgroupPrefix, mapGroupName);

        var existingMapGroup = FindProperty(mapGroupsMp, mapGroupName);
        if (existingMapGroup != null)
            return;

        mapGroupsMp.Add(mapGroupName, new VValue(GetNextMapGroupWeight(mapGroupsMp).ToString()));
    }

    private static void RemovePreviousModeMapGroups(
        VObject mapGroupsMp,
        string mapGroupPrefix,
        string activeMapGroupName)
    {
        if (string.IsNullOrWhiteSpace(mapGroupPrefix))
            return;

        var keysToRemove = mapGroupsMp.Properties()
            .Select(property => property.Key)
            .Where(key =>
                key.StartsWith(mapGroupPrefix, StringComparison.OrdinalIgnoreCase) &&
                !key.Equals(activeMapGroupName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var key in keysToRemove)
            mapGroupsMp.Remove(key);
    }

    private static int GetNextMapGroupWeight(VObject mapGroupsMp)
    {
        var maxWeight = 49;

        foreach (var property in mapGroupsMp.Properties())
        {
            if (!int.TryParse(property.Value.ToString(), out var currentWeight))
                continue;

            if (currentWeight > maxWeight)
                maxWeight = currentWeight;
        }

        return maxWeight + 1;
    }

    private static VObject GetOrCreateGameTypeNode(VObject gameTypes, int gameType)
    {
        var normalizedName = _gameTypeNames.TryGetValue(gameType, out var name)
            ? name
            : gameType.ToString();

        var candidates = new[] { normalizedName, gameType.ToString() };
        return GetOrCreateNamedObject(gameTypes, candidates, normalizedName);
    }

    private static VObject GetOrCreateGameModeNode(VObject gameModes, int gameType, int gameMode)
    {
        var normalizedName = _gameModeNames.TryGetValue((gameType, gameMode), out var name)
            ? name
            : gameMode.ToString();

        var candidates = new[] { normalizedName, gameMode.ToString() };
        return GetOrCreateNamedObject(gameModes, candidates, normalizedName);
    }

    private static VObject GetOrCreateNamedObject(
        VObject parent,
        IEnumerable<string> candidateNames,
        string preferredCreateName)
    {
        foreach (var candidate in candidateNames.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var existing = FindProperty(parent, candidate);
            if (existing == null)
                continue;

            if (existing.Value is VObject existingObject)
                return existingObject;

            var replacement = new VObject();
            parent[existing.Key] = replacement;
            return replacement;
        }

        var created = new VObject();
        parent.Add(preferredCreateName, created);
        return created;
    }

    private static VObject GetOrCreateObject(VObject parent, string key)
    {
        var existing = FindProperty(parent, key);
        if (existing != null)
        {
            if (existing.Value is VObject existingObject)
                return existingObject;

            var replacement = new VObject();
            parent[existing.Key] = replacement;
            return replacement;
        }

        var created = new VObject();
        parent.Add(key, created);
        return created;
    }

    private static void SetString(VObject parent, string key, string value)
    {
        var existing = FindProperty(parent, key);
        if (existing != null)
        {
            parent[existing.Key] = new VValue(value);
            return;
        }

        parent.Add(key, new VValue(value));
    }

    private static VProperty? FindProperty(VObject parent, string key)
    {
        foreach (var property in parent.Properties())
        {
            if (property.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return property;
        }

        return null;
    }

    private static string NormalizeLineEndings(string content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
}
