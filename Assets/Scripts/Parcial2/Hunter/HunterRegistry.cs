using System.Collections.Generic;
using UnityEngine;

public static class HunterRegistry
{
    private static readonly List<HunterAgent> hunters = new List<HunterAgent>();

    public static IReadOnlyList<HunterAgent> All => hunters;

    public static void Register(HunterAgent hunter)
    {
        if (hunter != null && !hunters.Contains(hunter)) hunters.Add(hunter);
    }

    public static void Unregister(HunterAgent hunter)
    {
        hunters.Remove(hunter);
    }

    public static HunterAgent FindNearest(Vector3 position, float radius)
    {
        HunterAgent nearest = null;
        float best = radius * radius;

        for (int i = hunters.Count - 1; i >= 0; i--)
        {
            HunterAgent hunter = hunters[i];
            if (hunter == null)
            {
                hunters.RemoveAt(i);
                continue;
            }

            if (hunter.IsDead) continue;

            float distance = (hunter.transform.position - position).sqrMagnitude;
            if (distance >= best) continue;

            best = distance;
            nearest = hunter;
        }

        return nearest;
    }
}
