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

        GameObject hunterObject = CreateCapsule("Hunter Test", new Vector3(50f, 1f, 30f));
        HunterAgent hunter = hunterObject.AddComponent<HunterAgent>();
        hunter.Configure(graph, null, null, Color.white, 6f, 22f);
        hunter.SetGoal(1);
        hunter.StartBrain();

        // Interest objects are only generated inside the hunting ground and
        // never exceed the five active objects allowed by the statement.
        for (int i = 0; i < 8; i++) hunter.TickInterest(7f);
        bool interestSpawned = hunter.ActiveInterestObjects > 0;
        bool interestCapped = hunter.ActiveInterestObjects <= 5;
        hunter.ClearInterestObjects();

        PreyAgent prey = CreatePrey(new Vector3(50f, 0f, 35.5f));

        // Attack can only start once the time between attacks (TBA) has elapsed.
        hunter.Tick(0.9f);
        bool attacks = hunter.StateId == HunterStateId.Attack;

        for (int i = 0; i < 300 && !prey.IsDead; i++) hunter.Tick(0.05f);
        bool killed = prey.IsDead;

        hunter.Tick(0.05f);
        bool gathers = hunter.StateId == HunterStateId.Gather;

        for (int i = 0; i < 300 && hunter.Collected == 0; i++) hunter.Tick(0.05f);
        bool collected = hunter.Collected == 1;

        hunter.Tick(0.05f);
        bool exits = hunter.StateId == HunterStateId.Exit;

        for (int i = 0; i < 1200 && hunter.RunsCompleted == 0; i++) hunter.Tick(0.05f);
        bool escaped = hunter.RunsCompleted == 1;
        bool backAtInitial = (hunter.transform.position - hunter.InitialSpawn).sqrMagnitude < 4f;

        // Flee: a real wolf registered in the scene is detected with distance
        // and line of sight, and the hunter picks the nearest safe zone by A*.
        GameObject packObject = new GameObject("Hunter Test Pack");
        WolfPack pack = packObject.AddComponent<WolfPack>();
        WolfAgent wolf = CreateWolf(graph, hunter.transform.position + new Vector3(0f, 0f, 4f), hunter.transform.position);
        pack.Register(wolf);

        hunter.Tick(0.05f);
        bool flees = hunter.StateId == HunterStateId.Flee;

        hunter.Tick(0.05f);
        bool safeZoneChosen = hunter.HasSafeZoneTarget;

        // Remove the threat and place the hunter inside a safe zone. After the
        // wait time it must resume Search (counter is at zero).
        Object.DestroyImmediate(wolf.gameObject);
        Vector3 safe = new Vector3(
            LevelLayout.SafeZoneA.x + LevelLayout.SafeZoneA.width * 0.5f,
            1f,
            LevelLayout.SafeZoneA.y + LevelLayout.SafeZoneA.height * 0.5f);
        hunter.transform.position = safe;

        for (int i = 0; i < 80; i++) hunter.Tick(0.05f);
        bool returns = hunter.StateId == HunterStateId.Search;

        // Dead state hides the hunter, then it respawns in the initial zone
        // with the gather counter reset.
        hunter.Kill();
        hunter.Tick(0.05f);
        bool dies = hunter.IsDead && hunter.StateId == HunterStateId.Dead;

        for (int i = 0; i < 130; i++) hunter.Tick(0.05f);
        bool respawns = !hunter.IsDead && hunter.Collected == 0 && hunter.StateId == HunterStateId.Search;

        bool ok = interestSpawned && interestCapped && attacks && killed && gathers && collected &&
                  exits && escaped && backAtInitial && flees && safeZoneChosen && returns && dies && respawns;

        Debug.Log($"Hunter: {(ok ? "OK" : "FAIL")} | interest={interestSpawned}/{interestCapped} attack={attacks} killed={killed} gather={gathers} collected={collected} exit={exits} escaped={escaped} back={backAtInitial} flee={flees} safe={safeZoneChosen} returns={returns} dead={dies} respawn={respawns}");

        Object.DestroyImmediate(prey.gameObject);
        if (wolf != null) Object.DestroyImmediate(wolf.gameObject);
        Object.DestroyImmediate(packObject);
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

    private static WolfAgent CreateWolf(Graph graph, Vector3 position, Vector3 lookAt)
    {
        GameObject go = CreateCapsule("Hunter Test Wolf", position);
        go.transform.forward = (lookAt - position).normalized;

        WolfAgent wolf = go.AddComponent<WolfAgent>();
        wolf.Configure(graph, null, Color.gray, 4.2f, 16f, new[] { position });
        WolfRegistry.Register(wolf);
        return wolf;
    }

    private static PreyAgent CreatePrey(Vector3 position)
    {
        GameObject go = CreateCapsule("Hunter Test Prey", position + Vector3.up * 0.5f);

        PreyAgent agent = go.AddComponent<PreyAgent>();
        agent.Configure(null, null, Color.white, 4.5f, 16f);
        PreyRegistry.Register(agent);
        return agent;
    }
}
