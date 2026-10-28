using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LevelBuilder
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";
    private const string MaterialFolder = "Assets/Materials/Level";
    private const string MeshFolder = "Assets/Meshes/Level";
    private const float WallHeight = 3f;

    [MenuItem("Tools/Level/Build Parcial 2")]
    public static void Build()
    {
        CellType[,] cells = LevelLayout.Build();

        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        ClearMeshes();

        Material floor = CreateMaterial("Floor", new Color(0.85f, 0.82f, 0.74f));
        Material wall = CreateMaterial("Wall", new Color(0.18f, 0.19f, 0.22f));
        Material hunting = CreateMaterial("HuntingGround", new Color(0.95f, 0.72f, 0.35f));
        Material initial = CreateMaterial("InitialZone", new Color(0.45f, 0.75f, 0.55f));
        Material safe = CreateMaterial("SafeZone", new Color(0.65f, 0.45f, 0.80f));
        Material exit = CreateMaterial("ExitZone", new Color(0.40f, 0.65f, 0.90f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level").transform;

        BuildFloor(cells, root, CellType.Floor, floor);
        BuildFloor(cells, root, CellType.HuntingGround, hunting);
        BuildFloor(cells, root, CellType.Initial, initial);
        BuildFloor(cells, root, CellType.SafeZone, safe);
        BuildFloor(cells, root, CellType.Exit, exit);
        BuildWalls(cells, root, wall);
        BuildZones(root);

        CreateCamera();
        CreateLight();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }

    private static void BuildFloor(CellType[,] cells, Transform parent, CellType type, Material material)
    {
        MeshData data = new MeshData();

        for (int x = 0; x < LevelLayout.Width; x++)
        for (int z = 0; z < LevelLayout.Height; z++)
        {
            if (cells[x, z] == type) data.AddTopQuad(x, z);
        }

        if (data.Vertices.Count == 0) return;
        CreateMeshObject(parent, type.ToString(), data, material, false);
    }

    private static void BuildWalls(CellType[,] cells, Transform parent, Material material)
    {
        MeshData data = new MeshData();

        for (int x = 0; x < LevelLayout.Width; x++)
        for (int z = 0; z < LevelLayout.Height; z++)
        {
            if (cells[x, z] != CellType.Solid || !HasFloorNeighbour(cells, x, z)) continue;
            data.AddBox(x, z, WallHeight);
        }

        if (data.Vertices.Count == 0) return;
        CreateMeshObject(parent, "Walls", data, material, true);
    }

    private static bool HasFloorNeighbour(CellType[,] cells, int x, int z)
    {
        return IsFloor(cells, x - 1, z) || IsFloor(cells, x + 1, z) || IsFloor(cells, x, z - 1) || IsFloor(cells, x, z + 1);
    }

    private static bool IsFloor(CellType[,] cells, int x, int z)
    {
        if (x < 0 || x >= LevelLayout.Width || z < 0 || z >= LevelLayout.Height) return false;
        return cells[x, z] != CellType.Solid;
    }

    private static void BuildZones(Transform parent)
    {
        CreateZone(parent, "Initial", ZoneType.Initial, LevelLayout.InitialZone, new Color(0.45f, 0.75f, 0.55f));
        CreateZone(parent, "HuntingGround", ZoneType.HuntingGround, LevelLayout.HuntingGround, new Color(0.95f, 0.72f, 0.35f));
        CreateZone(parent, "SafeZoneA", ZoneType.SafeZone, LevelLayout.SafeZoneA, new Color(0.65f, 0.45f, 0.80f));
        CreateZone(parent, "SafeZoneB", ZoneType.SafeZone, LevelLayout.SafeZoneB, new Color(0.65f, 0.45f, 0.80f));
        CreateZone(parent, "Exit", ZoneType.Exit, LevelLayout.ExitZone, new Color(0.40f, 0.65f, 0.90f));
    }

    private static void CreateZone(Transform parent, string name, ZoneType type, RectInt rect, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(rect.x + rect.width * 0.5f, 0f, rect.y + rect.height * 0.5f);

        Zone zone = go.AddComponent<Zone>();
        zone.Configure(type, new Vector3(rect.width, 1f, rect.height), color);
    }

    private static void CreateMeshObject(Transform parent, string name, MeshData data, Material material, bool collider)
    {
        Mesh mesh = data.ToMesh(name);
        string path = $"{MeshFolder}/{name}.asset";
        AssetDatabase.CreateAsset(mesh, path);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(50f, 90f, -40f);
        go.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

        Camera camera = go.AddComponent<Camera>();
        camera.fieldOfView = 50f;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ClearMeshes()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private sealed class MeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        public void AddTopQuad(int x, int z)
        {
            float x0 = x, x1 = x + 1f, z0 = z, z1 = z + 1f;
            AddQuad(new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0), Vector3.up);
        }

        public void AddBox(int x, int z, float height)
        {
            float x0 = x, x1 = x + 1f, z0 = z, z1 = z + 1f, y0 = 0f, y1 = height;

            AddQuad(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.up);
            AddQuad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), Vector3.down);
            AddQuad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward);
            AddQuad(new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), Vector3.back);
            AddQuad(new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), Vector3.right);
            AddQuad(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left);
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int start = Vertices.Count;

            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
            Normals.Add(normal); Normals.Add(normal); Normals.Add(normal); Normals.Add(normal);
            Uvs.Add(new Vector2(0f, 0f)); Uvs.Add(new Vector2(0f, 1f)); Uvs.Add(new Vector2(1f, 1f)); Uvs.Add(new Vector2(1f, 0f));
            Triangles.Add(start); Triangles.Add(start + 1); Triangles.Add(start + 2);
            Triangles.Add(start); Triangles.Add(start + 2); Triangles.Add(start + 3);
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
