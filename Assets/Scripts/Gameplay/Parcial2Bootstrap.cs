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

    private static readonly Vector3[] WolfSpawns =
    {
        new Vector3(34f, 0.7f, 26f),
        new Vector3(66f, 0.7f, 34f),
        new Vector3(50f, 0.7f, 25f),
        new Vector3(50f, 0.7f, 35f)
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

        SpawnFlock();
        SpawnHunter();
        SpawnWolves();
    }

    private void SpawnFlock()
    {
        GameObject flockObject = new GameObject("PreyFlock");
        flockObject.transform.SetParent(transform, false);

        PreyFlock flock = flockObject.AddComponent<PreyFlock>();
        flock.Configure(LevelLayout.HuntingGround, preyMaterial, preyCount, preySpeed, preyForce);
        flock.Spawn();
    }

    private void SpawnHunter()
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
        hunter.Configure(graph, hunterMaterial, line, new Color(0.92f, 0.25f, 0.3f), hunterSpeed, hunterForce);
    }

    private void SpawnWolves()
    {
        GameObject packObject = new GameObject("WolfPack");
        packObject.transform.SetParent(transform, false);

        for (int i = 0; i < wolfCount; i++)
        {
            GameObject wolfObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wolfObject.name = $"Wolf_{i}";
            wolfObject.transform.SetParent(packObject.transform, false);
            wolfObject.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            wolfObject.transform.position = WolfSpawns[i % WolfSpawns.Length];

            Collider collider = wolfObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            WolfAgent wolf = wolfObject.AddComponent<WolfAgent>();
            wolf.Configure(graph, wolfMaterial, new Color(0.42f, 0.44f, 0.48f), wolfSpeed, wolfForce);
        }
    }
}
