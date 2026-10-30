using UnityEngine;

public class Parcial2Bootstrap : MonoBehaviour
{
    [SerializeField] private Graph graph;
    [SerializeField] private Material preyMaterial;
    [SerializeField] private Material hunterMaterial;
    [SerializeField] private Material hunterPathMaterial;
    [SerializeField] private Material wolfMaterial;
    [SerializeField] private int preyCount = 6;
    [SerializeField] private int wolfCount = 4;
    [SerializeField] private float preySpeed = 4.5f;
    [SerializeField] private float preyForce = 16f;
    [SerializeField] private float hunterSpeed = 6f;
    [SerializeField] private float hunterForce = 22f;
    [SerializeField] private float wolfSpeed = 4.2f;
    [SerializeField] private float wolfForce = 16f;

    private static readonly Vector3[][] WolfRoutes =
    {
        // L1: anillo oeste (ida y vuelta)
        new[]
        {
            new Vector3(16.5f, 0.7f, 8f),
            new Vector3(16.5f, 0.7f, 29.5f),
            new Vector3(16.5f, 0.7f, 52f)
        },
        // L2: anillo norte + sala norte
        new[]
        {
            new Vector3(25f, 0.7f, 53.5f),
            new Vector3(37.5f, 0.7f, 53.5f),
            new Vector3(37.5f, 0.7f, 41.5f),
            new Vector3(62.5f, 0.7f, 41.5f),
            new Vector3(62.5f, 0.7f, 53.5f),
            new Vector3(75f, 0.7f, 53.5f)
        },
        // L3: anillo este (ida y vuelta)
        new[]
        {
            new Vector3(83.5f, 0.7f, 8f),
            new Vector3(83.5f, 0.7f, 29.5f),
            new Vector3(83.5f, 0.7f, 52f)
        },
        // L4: anillo sur + sala sur
        new[]
        {
            new Vector3(25f, 0.7f, 6.5f),
            new Vector3(37.5f, 0.7f, 6.5f),
            new Vector3(37.5f, 0.7f, 18.5f),
            new Vector3(62.5f, 0.7f, 18.5f),
            new Vector3(62.5f, 0.7f, 6.5f),
            new Vector3(75f, 0.7f, 6.5f)
        }
    };

    public void Configure(
        Graph graphReference,
        Material preyMaterialReference,
        Material hunterMaterialReference,
        Material hunterPathMaterialReference,
        Material wolfMaterialReference)
    {
        graph = graphReference;
        preyMaterial = preyMaterialReference;
        hunterMaterial = hunterMaterialReference;
        hunterPathMaterial = hunterPathMaterialReference;
        wolfMaterial = wolfMaterialReference;
    }

    private void Awake()
    {
        Build();
    }

    public void Build()
    {
        if (graph != null) graph.EnsureBuilt();

        PreyFlock flock = SpawnFlock();
        HunterAgent hunter = SpawnHunter();
        WolfPack pack = SpawnWolves();
        CreateFeedback(flock, hunter, pack);
    }

    private PreyFlock SpawnFlock()
    {
        GameObject flockObject = new GameObject("PreyFlock");
        flockObject.transform.SetParent(transform, false);

        PreyFlock flock = flockObject.AddComponent<PreyFlock>();
        flock.Configure(LevelLayout.HuntingGround, preyMaterial, preyCount, preySpeed, preyForce);
        flock.Spawn();
        return flock;
    }

    private HunterAgent SpawnHunter()
    {
        GameObject hunterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hunterObject.name = "Hunter";
        hunterObject.transform.SetParent(transform, false);
        hunterObject.transform.position = new Vector3(6.5f, 1f, 30f);

        Collider collider = hunterObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        LineRenderer line = hunterObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = 0.18f;
        line.numCapVertices = 2;
        line.sharedMaterial = hunterPathMaterial;
        line.startColor = new Color(1f, 0.55f, 0.15f);
        line.endColor = new Color(1f, 0.9f, 0.3f);

        HunterAgent hunter = hunterObject.AddComponent<HunterAgent>();
        hunter.Configure(graph, hunterMaterial, line, new Color(0.92f, 0.25f, 0.3f), hunterSpeed, hunterForce, preyMaterial);
        return hunter;
    }

    private WolfPack SpawnWolves()
    {
        GameObject packObject = new GameObject("WolfPack");
        packObject.transform.SetParent(transform, false);
        WolfPack pack = packObject.AddComponent<WolfPack>();

        for (int i = 0; i < wolfCount; i++)
        {
            Vector3[] route = WolfRoutes[i % WolfRoutes.Length];

            GameObject wolfObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wolfObject.name = $"L{i + 1}";
            wolfObject.transform.SetParent(packObject.transform, false);
            wolfObject.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            wolfObject.transform.position = route[0];

            Collider collider = wolfObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            WolfAgent wolf = wolfObject.AddComponent<WolfAgent>();

            LineRenderer line = wolfObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.12f;
            line.numCapVertices = 2;
            line.sharedMaterial = hunterPathMaterial;
            line.startColor = new Color(1f, 0.35f, 0.2f, 0.8f);
            line.endColor = new Color(1f, 0.8f, 0.25f, 0.8f);
            line.positionCount = 0;

            wolf.Configure(graph, wolfMaterial, new Color(0.42f, 0.44f, 0.48f), wolfSpeed, wolfForce, route, line);
            pack.Register(wolf);
        }

        return pack;
    }

    private void CreateFeedback(PreyFlock flock, HunterAgent hunter, WolfPack pack)
    {
        Transform lastKnown = CreateMarker("LastKnownMarker", hunterPathMaterial, 0.55f);

        GameObject feedbackObject = new GameObject("HunterHud");
        feedbackObject.transform.SetParent(transform, false);
        HunterHud feedback = feedbackObject.AddComponent<HunterHud>();
        feedback.Configure(hunter, lastKnown);

        GameObject wolfHudObject = new GameObject("WolfHud");
        wolfHudObject.transform.SetParent(transform, false);
        WolfHud wolfHud = wolfHudObject.AddComponent<WolfHud>();
        wolfHud.Configure(pack, hunter);

        for (int i = 0; i < flock.Agents.Count; i++)
        {
            PreyAgent prey = flock.Agents[i];
            Transform icon = CreateMarker($"Detection_{i}", hunterPathMaterial, 0.22f);

            DetectionMarker marker = icon.gameObject.AddComponent<DetectionMarker>();
            marker.Configure(prey, hunter, icon);

            Renderer iconRenderer = icon.GetComponent<Renderer>();
            if (iconRenderer != null) iconRenderer.enabled = false;
        }
    }

    private Transform CreateMarker(string name, Material material, float size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * size;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        return go.transform;
    }
}
