using UnityEditor;
using UnityEngine;

public static class BoidsTests
{
    [MenuItem("Tools/Level/Test Boids")]
    public static void Run()
    {
        GameObject host = new GameObject("Boids Test");
        PreyFlock flock = host.AddComponent<PreyFlock>();
        flock.Configure(LevelLayout.HuntingGround, null, 6, 4.5f, 16f);
        flock.Spawn();

        RectInt area = LevelLayout.HuntingGround;
        bool inside = true;

        for (int step = 0; step < 1200; step++)
        {
            flock.Step(1f / 60f);

            for (int i = 0; i < flock.Agents.Count; i++)
            {
                Vector3 position = flock.Agents[i].transform.position;
                if (position.x < area.x || position.x > area.x + area.width) inside = false;
                if (position.z < area.y || position.z > area.y + area.height) inside = false;
            }
        }

        int spawned = flock.Agents.Count;
        bool countOk = spawned == 6;
        bool moving = flock.Agents.Count > 0 && flock.Agents[0].Velocity.sqrMagnitude > 0.01f;

        Debug.Log($"Boids: {(countOk && inside && moving ? "OK" : "FAIL")} | spawned={spawned} inside={inside} moving={moving}");

        Object.DestroyImmediate(host);
    }
}
