using System;
using System.Collections.Generic;
using System.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace ModeManager;

internal sealed class GameModesServerVdfEditor
{
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

    public string BuildInitialContent(ModeManagerConfig config, EndMatchMapVoteMapPoolBuilder mapPoolBuilder)
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
            var maps = mapPoolBuilder.BuildConfiguredMaps(mode);
            if (maps.Count == 0)
                continue;

            var mapGroupName = BuildModeMapGroupName(modeKey);
            EnsureMapGroup(root, mapGroupName, mode.DisplayName, maps);
        }

        var document = new VProperty("GameModes_Server.txt", root);
        return VdfConvert.Serialize(document);
    }

    public string ApplyMode(
        string fileContent,
        ModeDefinition mode,
        IReadOnlyList<string> maps,
        out string mapGroupName)
    {
        mapGroupName = BuildModeMapGroupName(mode.Key);

        var parsedRoot = VdfConvert.Deserialize(fileContent);
        if (parsedRoot.Value is not VObject rootObject)
            throw new InvalidOperationException("VDF root does not contain an object.");

        EnsureMapGroup(rootObject, mapGroupName, mode.DisplayName, maps);
        EnsureMapGroupBindingForGameMode(
            rootObject,
            mapGroupName,
            mode.GameType ?? 0,
            mode.GameMode ?? 0);

        return VdfConvert.Serialize(parsedRoot);
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
}
