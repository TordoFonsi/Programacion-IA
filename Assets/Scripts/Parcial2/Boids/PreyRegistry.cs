using System.Collections.Generic;
using UnityEngine;

public static class PreyRegistry
{
    private static readonly List<PreyAgent> prey = new List<PreyAgent>();

    public static IReadOnlyList<PreyAgent> All => prey;

    public static void Register(PreyAgent agent)
    {
        if (agent != null && !prey.Contains(agent)) prey.Add(agent);
    }

    public static void Unregister(PreyAgent agent)
    {
        prey.Remove(agent);
    }

    public static PreyAgent FindNearest(Vector3 position, float radius)
    {
        PreyAgent nearest = null;
        float best = radius * radius;

        for (int i = prey.Count - 1; i >= 0; i--)
        {
            PreyAgent agent = prey[i];
            if (agent == null)
            {
                prey.RemoveAt(i);
                continue;
            }

            if (!agent.IsActive) continue;

            float distance = (agent.transform.position - position).sqrMagnitude;
            if (distance >= best) continue;

            best = distance;
            nearest = agent;
        }

        return nearest;
    }
}
