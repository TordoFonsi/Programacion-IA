using System.Collections.Generic;
using UnityEngine;

public class WolfPack : MonoBehaviour
{
    private readonly List<WolfAgent> wolves = new List<WolfAgent>();

    public IReadOnlyList<WolfAgent> Wolves => wolves;
    public int AlertCount { get; private set; }
    public Vector3 LastAlertPosition { get; private set; }

    public void Register(WolfAgent wolf)
    {
        if (wolf == null || wolves.Contains(wolf)) return;

        wolves.Add(wolf);
        wolf.SetPack(this);
    }

    public void Unregister(WolfAgent wolf)
    {
        wolves.Remove(wolf);
    }

    public void Alert(Vector3 position, WolfAgent source)
    {
        LastAlertPosition = position;
        AlertCount++;

        for (int i = 0; i < wolves.Count; i++)
        {
            WolfAgent wolf = wolves[i];
            if (wolf == null || wolf == source) continue;

            wolf.ReceiveAlert(position);
        }
    }
}
