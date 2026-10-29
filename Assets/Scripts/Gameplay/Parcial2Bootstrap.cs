using UnityEngine;

public class Parcial2Bootstrap : MonoBehaviour
{
    [SerializeField] private Graph graph;
    [SerializeField] private Material preyMaterial;
    [SerializeField] private Material hunterMaterial;
    [SerializeField] private Material hunterPathMaterial;
    [SerializeField] private int preyCount = 6;
    [SerializeField] private float preySpeed = 4.5f;
    [SerializeField] private float preyForce = 16f;
    [SerializeField] private float hunterSpeed = 6f;
    [SerializeField] private float hunterForce = 22f;

    public void Configure(
        Graph graphReference,
        Material preyMaterialReference,
        Material hunterMaterialReference,
        Material hunterPathMaterialReference)
    {
        graph = graphReference;
        preyMaterial = preyMaterialReference;
        hunterMaterial = hunterMaterialReference;
        hunterPathMaterial = hunterPathMaterialReference;
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
}
