using UnityEditor;
using UnityEngine;

public static class FeedbackTests
{
    [MenuItem("Tools/Level/Test Feedback")]
    public static void Run()
    {
        GameObject graphHost = new GameObject("Feedback Graph Test");
        Graph graph = graphHost.AddComponent<Graph>();
        graph.Build();

        GameObject hunterObject = CreateCapsule("Feedback Hunter", new Vector3(50f, 1f, 30f));
        HunterAgent hunter = hunterObject.AddComponent<HunterAgent>();
        hunter.Configure(graph, null, null, Color.white, 6f, 22f);
        hunter.StartBrain();

        PreyAgent prey = CreatePrey(new Vector3(50f, 0f, 35f));

        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        icon.name = "Detection Icon Test";
        icon.transform.position = prey.transform.position;
        Renderer iconRenderer = icon.GetComponent<Renderer>();
        iconRenderer.enabled = false;

        DetectionMarker marker = icon.AddComponent<DetectionMarker>();
        marker.Configure(prey, hunter, icon.transform);

        hunter.Tick(0.02f);
        marker.SendMessage("Update");
        bool shows = iconRenderer.enabled;

        hunter.Vision.Forget();
        marker.SendMessage("Update");
        bool hides = !iconRenderer.enabled;

        bool ok = shows && hides;
        Debug.Log($"Feedback: {(ok ? "OK" : "FAIL")} | shows={shows} hides={hides}");

        Object.DestroyImmediate(icon);
        Object.DestroyImmediate(prey.gameObject);
        Object.DestroyImmediate(hunterObject);
        Object.DestroyImmediate(graphHost);
    }

    private static GameObject CreateCapsule(string name, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = position;
        go.transform.forward = Vector3.forward;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        return go;
    }

    private static PreyAgent CreatePrey(Vector3 position)
    {
        GameObject go = CreateCapsule("Feedback Prey", position + Vector3.up * 0.5f);

        PreyAgent agent = go.AddComponent<PreyAgent>();
        agent.Configure(null, null, Color.white, 4.5f, 16f);
        PreyRegistry.Register(agent);
        return agent;
    }
}
