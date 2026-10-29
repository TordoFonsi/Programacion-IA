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

        WolfAgent wolf = CreateWolf(graph, new Vector3(50f, 1f, 30f));

        PreyAgent prey = CreatePrey(new Vector3(50f, 0f, 35f));
        wolf.Tick(0.02f);
        bool seesPrey = wolf.PreyTarget == prey;

        for (int i = 0; i < 300 && !prey.IsDead; i++) wolf.Tick(0.05f);
        bool killsPrey = prey.IsDead;

        HunterAgent hunter = CreateHunter(graph, new Vector3(50f, 1f, 31f));
        HunterRegistry.Register(hunter);
        int healthBefore = hunter.Health;

        for (int i = 0; i < 400 && hunter.Health == healthBefore; i++) wolf.Tick(0.05f);
        bool attacksHunter = hunter.Health < healthBefore;

        Object.DestroyImmediate(hunter.gameObject);

        Vector3 safeCenter = new Vector3(
            LevelLayout.SafeZoneA.x + LevelLayout.SafeZoneA.width * 0.5f,
            1f,
            LevelLayout.SafeZoneA.y + LevelLayout.SafeZoneA.height * 0.5f);

        HunterAgent safeHunter = CreateHunter(graph, safeCenter);
        HunterRegistry.Register(safeHunter);

        wolf.transform.position = new Vector3(safeCenter.x - 5f, 1f, safeCenter.z);

        for (int i = 0; i < 300; i++) wolf.Tick(0.05f);
        bool staysOut = !wolf.IsInsideSafeZone();

        bool slower = wolf.MaxSpeed < 6f;
        bool ok = seesPrey && killsPrey && attacksHunter && staysOut && slower;

        Debug.Log($"Wolves: {(ok ? "OK" : "FAIL")} | seesPrey={seesPrey} killed={killsPrey} attacksHunter={attacksHunter} staysOut={staysOut} slower={slower}");

        Object.DestroyImmediate(prey.gameObject);
        Object.DestroyImmediate(safeHunter.gameObject);
        Object.DestroyImmediate(wolf.gameObject);
        Object.DestroyImmediate(graphHost);
    }

    private static WolfAgent CreateWolf(Graph graph, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Wolf Test";
        go.transform.position = position;
        go.transform.forward = Vector3.forward;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        WolfAgent wolf = go.AddComponent<WolfAgent>();
        wolf.Configure(graph, null, Color.gray, 4.2f, 16f);
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
        hunter.StartBrain();
        return hunter;
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
