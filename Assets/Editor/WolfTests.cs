using UnityEditor;
using UnityEngine;

public static class WolfTests
{
    [MenuItem("Tools/Level/Test Wolves")]
    public static void Run()
    {
        GameObject graphHost = new GameObject("Wolf Graph Test");
        Graph graph = graphHost.AddComponent<Graph>();
        graph.Build();

        GameObject packObject = new GameObject("Wolf Test Pack");
        WolfPack pack = packObject.AddComponent<WolfPack>();

        WolfAgent wolf = CreateWolf(
            graph,
            new Vector3(50f, 0.7f, 30f),
            Vector3.forward,
            new[]
            {
                new Vector3(50f, 0.7f, 30f),
                new Vector3(50f, 0.7f, 34f)
            });
        pack.Register(wolf);

        WolfAgent listener = CreateWolf(
            graph,
            new Vector3(16.5f, 0.7f, 30f),
            Vector3.forward,
            new[]
            {
                new Vector3(16.5f, 0.7f, 30f),
                new Vector3(16.5f, 0.7f, 34f)
            });
        pack.Register(listener);

        HunterAgent hunter = CreateHunter(graph, new Vector3(50f, 1f, 34f));
        hunter.StartBrain();

        // A physical wall between the listener and the hunter forces the wolf to
        // fall back to A* instead of steering in a straight line.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wolf Test Wall";
        wall.transform.position = new Vector3(30f, 1.5f, 32f);
        wall.transform.localScale = new Vector3(2f, 3f, 8f);
        Physics.SyncTransforms();

        wolf.Tick(0.02f);
        bool seesHunter = wolf.Target == hunter && wolf.HasAlert;

        bool alertPropagates = listener.HasAlert &&
                               (listener.LastKnownPosition - hunter.transform.position).sqrMagnitude < 1f;

        // The listener has no line of sight to the hunter, so it must use A*.
        listener.Tick(0.05f);
        bool usesAStar = listener.CurrentPath.Count > 1;

        // Chase and capture: reaching the hunter puts it in the Dead state.
        for (int i = 0; i < 400 && !hunter.IsDead; i++) wolf.Tick(0.05f);
        bool captures = hunter.IsDead;

        // A hunter inside a safe zone cannot be detected and the wolf cannot enter.
        Vector3 safeCenter = new Vector3(
            LevelLayout.SafeZoneA.x + LevelLayout.SafeZoneA.width * 0.5f,
            1f,
            LevelLayout.SafeZoneA.y + LevelLayout.SafeZoneA.height * 0.5f);

        HunterAgent safeHunter = CreateHunter(graph, safeCenter);
        safeHunter.StartBrain();

        WolfAgent guard = CreateWolf(
            graph,
            new Vector3(83.5f, 0.7f, 44f),
            Vector3.left,
            new[]
            {
                new Vector3(83.5f, 0.7f, 44f),
                new Vector3(83.5f, 0.7f, 48f)
            });
        pack.Register(guard);

        for (int i = 0; i < 300; i++) guard.Tick(0.05f);
        bool staysOut = !guard.IsInsideSafeZone();
        bool ignoresSafe = guard.Target == null;

        bool slower = wolf.MaxSpeed < hunter.MaxSpeed;

        bool ok = seesHunter && alertPropagates && usesAStar && captures && staysOut && ignoresSafe && slower;

        Debug.Log($"Wolves: {(ok ? "OK" : "FAIL")} | sees={seesHunter} alert={alertPropagates} astar={usesAStar} capture={captures} staysOut={staysOut} ignoresSafe={ignoresSafe} slower={slower}");

        Object.DestroyImmediate(listener.gameObject);
        Object.DestroyImmediate(guard.gameObject);
        Object.DestroyImmediate(wolf.gameObject);
        Object.DestroyImmediate(hunter.gameObject);
        Object.DestroyImmediate(safeHunter.gameObject);
        Object.DestroyImmediate(wall);
        Object.DestroyImmediate(packObject);
        Object.DestroyImmediate(graphHost);
    }

    private static WolfAgent CreateWolf(Graph graph, Vector3 position, Vector3 forward, Vector3[] route)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Wolf Test";
        go.transform.position = position;
        go.transform.forward = forward;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        WolfAgent wolf = go.AddComponent<WolfAgent>();
        wolf.Configure(graph, null, Color.gray, 4.2f, 16f, route);
        WolfRegistry.Register(wolf);
        return wolf;
    }

    private static HunterAgent CreateHunter(Graph graph, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Hunter Wolf Test";
        go.transform.position = position;
        go.transform.forward = Vector3.forward;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        HunterAgent hunter = go.AddComponent<HunterAgent>();
        hunter.Configure(graph, null, null, Color.white, 6f, 22f);
        HunterRegistry.Register(hunter);
        return hunter;
    }
}
