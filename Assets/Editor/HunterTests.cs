using UnityEditor;
using UnityEngine;

public static class HunterTests
{
    [MenuItem("Tools/Level/Test Hunter")]
    public static void Run()
    {
        GameObject graphHost = new GameObject("Hunter Graph Test");
        Graph graph = graphHost.AddComponent<Graph>();
        graph.Build();

        GameObject hunterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hunterObject.name = "Hunter Test";
        hunterObject.transform.position = new Vector3(50f, 1f, 30f);
        hunterObject.transform.forward = Vector3.forward;

        Collider collider = hunterObject.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        HunterAgent hunter = hunterObject.AddComponent<HunterAgent>();
        hunter.Configure(graph, null, null, Color.white, 6f, 22f);
        hunter.SetGoal(1);
        hunter.StartBrain();

        PreyAgent prey = CreatePrey(new Vector3(50f, 0f, 36f));

        bool sawPrey = hunter.Vision.Scan(0.02f);
        hunter.Tick(0.02f);
        bool attacks = hunter.StateId == HunterStateId.Attack;

        for (int i = 0; i < 120 && !prey.IsDead; i++) hunter.Tick(0.05f);
        bool killed = prey.IsDead;

        hunter.Tick(0.05f);
        bool gathers = hunter.StateId == HunterStateId.Gather;

        for (int i = 0; i < 200 && hunter.Collected == 0; i++) hunter.Tick(0.05f);
        bool collected = hunter.Collected == 1;

        hunter.Tick(0.05f);
        bool exits = hunter.StateId == HunterStateId.Exit;

        for (int i = 0; i < 800 && !hunter.IsEscaped; i++) hunter.Tick(0.05f);
        bool escaped = hunter.IsEscaped;

        GameObject threat = new GameObject("Wolf Test");
        threat.transform.position = hunter.transform.position + new Vector3(0f, 0f, 2f);
        ThreatRegistry.Register(threat.transform);
        hunter.Tick(0.05f);
        bool flees = hunter.StateId == HunterStateId.Flee;

        ThreatRegistry.Unregister(threat.transform);
        hunter.Tick(0.05f);
        bool returnsToExit = hunter.StateId == HunterStateId.Exit;

        hunter.ApplyDamage(10, hunter.transform.position);
        hunter.Tick(0.05f);
        bool dies = hunter.StateId == HunterStateId.Dead;

        bool ok = sawPrey && attacks && killed && gathers && collected && exits && escaped && flees && returnsToExit && dies;
        Debug.Log($"Hunter: {(ok ? "OK" : "FAIL")} | saw={sawPrey} attack={attacks} killed={killed} gather={gathers} collected={collected} exit={exits} escaped={escaped} flee={flees} back={returnsToExit} dead={dies}");

        Object.DestroyImmediate(threat);
        Object.DestroyImmediate(prey.gameObject);
        Object.DestroyImmediate(hunterObject);
        Object.DestroyImmediate(graphHost);
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
