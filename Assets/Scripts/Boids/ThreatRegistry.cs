using System.Collections.Generic;
using UnityEngine;

public static class ThreatRegistry
{
    private static readonly List<Transform> threats = new List<Transform>();

    public static IReadOnlyList<Transform> All => threats;

    public static void Register(Transform threat)
    {
        if (threat != null && !threats.Contains(threat)) threats.Add(threat);
    }

    public static void Unregister(Transform threat)
    {
        threats.Remove(threat);
    }

    public static Transform FindNearest(Vector3 position, float radius)
    {
        Transform nearest = null;
        float best = radius * radius;

        for (int i = threats.Count - 1; i >= 0; i--)
        {
            Transform threat = threats[i];
            if (threat == null)
            {
                threats.RemoveAt(i);
                continue;
            }

            Vector3 offset = threat.position - position;
            offset.y = 0f;
            float distance = offset.sqrMagnitude;
            if (distance >= best) continue;

            best = distance;
            nearest = threat;
        }

        return nearest;
    }
}
