using UnityEngine;

public class Parcial2Bootstrap : MonoBehaviour
{
    [SerializeField] private Graph graph;
    [SerializeField] private Material preyMaterial;
    [SerializeField] private int preyCount = 6;
    [SerializeField] private float preySpeed = 4.5f;
    [SerializeField] private float preyForce = 16f;

    public void Configure(Graph graphReference, Material preyMaterialReference)
    {
        graph = graphReference;
        preyMaterial = preyMaterialReference;
    }

    private void Awake()
    {
        Build();
    }

    public void Build()
    {
        if (graph != null) graph.EnsureBuilt();

        GameObject flockObject = new GameObject("PreyFlock");
        flockObject.transform.SetParent(transform, false);

        PreyFlock flock = flockObject.AddComponent<PreyFlock>();
        flock.Configure(LevelLayout.HuntingGround, preyMaterial, preyCount, preySpeed, preyForce);
        flock.Spawn();
    }
}
