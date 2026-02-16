using System;
using System.Collections.Generic;
using System.Linq;

namespace ModeManager;

internal sealed class VoteTargetMapResolver
{
    public IReadOnlyList<string> GetSelectableMapsForMode(ModeDefinition mode)
    {
        var maps = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawMap in mode.MapPool)
        {
            var map = NormalizeMapName(rawMap);
            if (map == null)
                continue;

            if (seen.Add(map))
                maps.Add(map);
        }

        if (maps.Count > 0)
            return maps;

        var fallback = NormalizeMapName(ModeSwitcher.ResolveTargetMap(mode));
        if (fallback != null)
            maps.Add(fallback);

        return maps;
    }

    public bool TryResolveTargetMap(
        ModeDefinition mode,
        string? requestedMap,
        bool hasExplicitMapSelection,
        out string targetMap)
    {
        var selectableMaps = GetSelectableMapsForMode(mode);
        if (selectableMaps.Count == 0)
        {
            targetMap = string.Empty;
            return false;
        }

        var normalizedRequestedMap = NormalizeMapName(requestedMap);
        if (hasExplicitMapSelection)
        {
            if (normalizedRequestedMap == null)
            {
                targetMap = string.Empty;
                return false;
            }

            var selected = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedRequestedMap, StringComparison.OrdinalIgnoreCase));

            if (selected == null)
            {
                targetMap = string.Empty;
                return false;
            }

            targetMap = selected;
            return true;
        }

        if (normalizedRequestedMap != null)
        {
            var requestedInPool = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedRequestedMap, StringComparison.OrdinalIgnoreCase));
            if (requestedInPool != null)
            {
                targetMap = requestedInPool;
                return true;
            }
        }

        targetMap = GetPreferredMapForMode(mode, selectableMaps);
        return true;
    }

    public bool IsCurrentMapForTarget(string map)
    {
        var currentMap = NormalizeMapName(MapResolver.TryGetCurrentMapName());
        var normalizedTargetMap = NormalizeMapName(map);

        return currentMap != null &&
               normalizedTargetMap != null &&
               currentMap.Equals(normalizedTargetMap, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPreferredMapForMode(ModeDefinition mode, IReadOnlyList<string> selectableMaps)
    {
        var normalizedDefaultMap = NormalizeMapName(mode.DefaultMap);
        if (normalizedDefaultMap != null)
        {
            var defaultMapInPool = selectableMaps.FirstOrDefault(
                map => map.Equals(normalizedDefaultMap, StringComparison.OrdinalIgnoreCase));
            if (defaultMapInPool != null)
                return defaultMapInPool;
        }

        return selectableMaps[0];
    }

    private static string? NormalizeMapName(string? map)
    {
        var trimmed = (map ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (trimmed.Contains(' ') || trimmed.Contains('"'))
            return null;

        return trimmed;
    }
}
