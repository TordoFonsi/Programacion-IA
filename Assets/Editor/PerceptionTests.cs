using UnityEditor;
using UnityEngine;

public static class PerceptionTests
{
    [MenuItem("Tools/Level/Test Perception")]
    public static void Run()
    {
        GameObject sensorObject = new GameObject("Sensor");
        sensorObject.transform.position = Vector3.zero;
        sensorObject.transform.forward = Vector3.forward;
        VisionSensor sensor = sensorObject.AddComponent<VisionSensor>();
        sensor.Configure(14f, 100f, 3f, ~0);

        PreyAgent front = CreatePrey(new Vector3(0f, 0f, 6f));
        bool seesFront = sensor.Scan();
        bool tracksFront = sensor.Target == front;

        PreyAgent behind = CreatePrey(new Vector3(0f, 0f, -6f));
        Object.DestroyImmediate(front.gameObject);
        bool ignoresBehind = !sensor.Scan();

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0f, 1.5f, 3f);
        wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
        Physics.SyncTransforms();

        PreyAgent hidden = CreatePrey(new Vector3(0f, 0f, 6f));
        bool blocked = !sensor.Scan();

        Object.DestroyImmediate(wall);
        Object.DestroyImmediate(hidden.gameObject);
        Object.DestroyImmediate(behind.gameObject);
        Object.DestroyImmediate(sensorObject);

        bool ok = seesFront && tracksFront && ignoresBehind && blocked;
        Debug.Log($"Perception: {(ok ? "OK" : "FAIL")} | seesFront={seesFront} tracked={tracksFront} ignoresBehind={ignoresBehind} blocked={blocked}");
    }

    private static PreyAgent CreatePrey(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.transform.position = position + Vector3.up * 0.5f;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        PreyAgent agent = go.AddComponent<PreyAgent>();
        agent.Configure(null, null, Color.white, 4.5f, 16f);
        PreyRegistry.Register(agent);
        return agent;
    }
}
