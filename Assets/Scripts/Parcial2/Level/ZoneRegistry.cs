using System.Collections.Generic;
using UnityEngine;

public static class ZoneRegistry
{
    private static readonly List<Zone> zones = new List<Zone>();

    public static IReadOnlyList<Zone> Zones => zones;

    public static void Register(Zone zone)
    {
        if (zone != null && !zones.Contains(zone)) zones.Add(zone);
    }

    public static void Unregister(Zone zone)
    {
        zones.Remove(zone);
    }

    public static Zone FindContaining(Vector3 position)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone != null && zone.Contains(position)) return zone;
        }

        return null;
    }
}
