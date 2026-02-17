using System;
using System.Collections.Generic;

namespace ModeManager;

internal sealed class EndMatchMapVoteMapPoolBuilder
{
    private readonly Func<ModeDefinition, string> _fallbackMapResolver;

    public EndMatchMapVoteMapPoolBuilder(Func<ModeDefinition, string> fallbackMapResolver)
    {
        _fallbackMapResolver = fallbackMapResolver;
    }

    public IReadOnlyList<string> BuildConfiguredMaps(ModeDefinition mode)
    {
        return BuildMaps(mode, includeFallbackMap: false);
    }

    public IReadOnlyList<string> BuildForSync(ModeDefinition mode)
    {
        return BuildMaps(mode, includeFallbackMap: true);
    }

    private IReadOnlyList<string> BuildMaps(ModeDefinition mode, bool includeFallbackMap)
    {
        var maps = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in mode.MapPool)
            TryAddMap(map, seen, maps);

        TryAddMap(mode.DefaultMap, seen, maps);

        if (includeFallbackMap && maps.Count == 0)
            TryAddMap(_fallbackMapResolver(mode), seen, maps);

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
}
