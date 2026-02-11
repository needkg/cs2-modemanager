using System.Reflection;
using CounterStrikeSharp.API;

namespace ModeManager;

internal static class MapResolver
{
    public static string? TryGetCurrentMapName()
    {
        try
        {
            var serverType = typeof(Server);

            var mapNameProperty = serverType.GetProperty("MapName", BindingFlags.Public | BindingFlags.Static);
            if (mapNameProperty != null)
            {
                var value = mapNameProperty.GetValue(null);
                if (value is string mapName && !string.IsNullOrWhiteSpace(mapName))
                    return mapName.Trim();
            }

            var mapNameMethod = serverType.GetMethod("GetMapName", BindingFlags.Public | BindingFlags.Static);
            if (mapNameMethod != null)
            {
                var value = mapNameMethod.Invoke(null, null);
                if (value is string mapName && !string.IsNullOrWhiteSpace(mapName))
                    return mapName.Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
