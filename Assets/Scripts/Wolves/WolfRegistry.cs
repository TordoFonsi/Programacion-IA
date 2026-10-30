using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global list of wolves so the hunter can detect them with distance + line of sight.
/// </summary>
public static class WolfRegistry
{
    private static readonly List<WolfAgent> wolves = new List<WolfAgent>();

    public static IReadOnlyList<WolfAgent> All => wolves;

    public static void Register(WolfAgent wolf)
    {
        if (wolf != null && !wolves.Contains(wolf)) wolves.Add(wolf);
    }

    public static void Unregister(WolfAgent wolf)
    {
        wolves.Remove(wolf);
    }
}
