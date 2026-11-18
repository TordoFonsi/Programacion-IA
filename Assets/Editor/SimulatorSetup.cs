using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SimulatorSetup
{
    private const string ScenePath = "Assets/Scenes/Simulator.unity";
    private const string FloorMaterialPath = "Assets/Materials/Level/Floor.mat";
    private const string WallMaterialPath = "Assets/Materials/Level/Wall.mat";
    private const string BoidMaterialPath = "Assets/Materials/BoidMaterial.mat";
    private const string MarkerMaterialPath = "Assets/Materials/ObjectMaterial.mat";

    private static readonly Vector3 BoundsCenter = new Vector3(10f, 0f, 6f);
    private static readonly Vector3 BoundsSize = new Vector3(44f, 1f, 36f);

    [MenuItem("Tools/Setup Simulator")]
    public static void BuildSimulator()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        WorldBounds bounds = CreateBounds();
        CreateGround(bounds);
        CreateWalls(bounds);
        CreateCamera(bounds);
        CreateLight();
        CreateWaypoints();
        CreateBoids(bounds);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[SimulatorSetup] Escena Simulator creada en " + ScenePath);
    }

    private static WorldBounds CreateBounds()
    {
        GameObject root = new GameObject("Bounds");
        root.transform.position = BoundsCenter;

        WorldBounds bounds = root.AddComponent<WorldBounds>();
        SetProperty(bounds, "size", BoundsSize);
        return bounds;
    }

    private static void CreateGround(WorldBounds bounds)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = bounds.Center;
        ground.transform.localScale = new Vector3(bounds.Size.x / 10f, 1f, bounds.Size.z / 10f);
        ground.layer = Layer("Obstacle");
        SetMaterial(ground, FloorMaterialPath);
    }

    private static void CreateWalls(WorldBounds bounds)
    {
        GameObject root = new GameObject("Walls");
        Vector3 center = bounds.Center;
        float halfX = bounds.Size.x * 0.5f;
        float halfZ = bounds.Size.z * 0.5f;

        CreateWall(root.transform, "Wall_North", center + new Vector3(0f, 0.3f, halfZ),
            new Vector3(bounds.Size.x + 0.3f, 0.6f, 0.3f));
        CreateWall(root.transform, "Wall_South", center + new Vector3(0f, 0.3f, -halfZ),
            new Vector3(bounds.Size.x + 0.3f, 0.6f, 0.3f));
        CreateWall(root.transform, "Wall_East", center + new Vector3(halfX, 0.3f, 0f),
            new Vector3(0.3f, 0.6f, bounds.Size.z + 0.3f));
        CreateWall(root.transform, "Wall_West", center + new Vector3(-halfX, 0.3f, 0f),
            new Vector3(0.3f, 0.6f, bounds.Size.z + 0.3f));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = Layer("Obstacle");
        SetMaterial(wall, WallMaterialPath);
    }

    private static void CreateCamera(WorldBounds bounds)
    {
        GameObject root = new GameObject("Main Camera");
        root.tag = "MainCamera";
        root.transform.position = bounds.Center + Vector3.up * 30f;
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera camera = root.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.Size.z * 0.5f + 2f, bounds.Size.x * 0.5f / 1.7f + 2f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        root.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        GameObject root = new GameObject("Directional Light");
        root.transform.position = new Vector3(0f, 10f, 0f);
        root.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = root.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateWaypoints()
    {
        GameObject root = new GameObject("Waypoints");
        Vector2[] offsets =
        {
            new Vector2(-18f, -14f),
            new Vector2(18f, -14f),
            new Vector2(18f, 14f),
            new Vector2(-18f, 14f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waypoint.name = "Waypoint_0" + (i + 1);
            waypoint.transform.SetParent(root.transform);
            waypoint.transform.position = BoundsCenter + new Vector3(offsets[i].x, 0.05f, offsets[i].y);
            waypoint.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
            waypoint.layer = Layer("Obstacle");
            Object.DestroyImmediate(waypoint.GetComponent<Collider>());
            SetMaterial(waypoint, MarkerMaterialPath);
        }
    }

    private static void CreateBoids(WorldBounds bounds)
    {
        GameObject root = new GameObject("Boids");
        int boidLayer = Layer("Boid");
        int hunterLayer = Layer("Hunter");
        int interestLayer = Layer("InterestObject");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BoidMaterialPath);

        Vector2[] offsets =
        {
            new Vector2(-4f, -4f),
            new Vector2(-1.5f, -5f),
            new Vector2(1.5f, -4f),
            new Vector2(4f, -2f),
            new Vector2(4f, 2f),
            new Vector2(1.5f, 4f),
            new Vector2(-1.5f, 4f),
            new Vector2(-4f, 2f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject boid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boid.name = "Boid_0" + (i + 1);
            boid.transform.SetParent(root.transform);
            boid.transform.position = bounds.Center + new Vector3(offsets[i].x, 0.5f, offsets[i].y);
            boid.transform.localScale = new Vector3(0.9f, 0.6f, 1.4f);
            boid.transform.rotation = Quaternion.Euler(0f, i * 45f, 0f);
            boid.layer = boidLayer;

            Renderer renderer = boid.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            SteeringBody steering = boid.AddComponent<SteeringBody>();
            SetProperty(steering, "maxSpeed", 4.5f);
            SetProperty(steering, "maxForce", 14f);
            SetProperty(steering, "turnSpeed", 360f);

            SteeringDemo demo = boid.AddComponent<SteeringDemo>();
            SetProperty(demo, "bounds", bounds);

            CreateSensor(boid.transform, "NeighbourSensor", SensorRole.BoidNeighbours, 6f, boidLayer);
            CreateSensor(boid.transform, "ThreatSensor", SensorRole.BoidThreat, 7f, hunterLayer);
            CreateSensor(boid.transform, "InterestSensor", SensorRole.BoidInterest, 5f, interestLayer);
        }
    }

    private static TriggerSensor CreateSensor(Transform parent, string name, SensorRole role, float radius, int layer)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;

        SphereCollider sphere = root.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;

        TriggerSensor sensor = root.AddComponent<TriggerSensor>();
        sensor.Configure(role, radius, 1 << layer);
        return sensor;
    }

    private static void SetMaterial(GameObject target, string path)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (renderer != null && material != null) renderer.sharedMaterial = material;
    }

    private static int Layer(string name)
    {
        int layer = LayerMask.NameToLayer(name);
        if (layer < 0) Debug.LogError("[SimulatorSetup] Falta la capa " + name);
        return Mathf.Max(0, layer);
    }

    private static void SetProperty(Object target, string name, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            Debug.LogError("[SimulatorSetup] No existe la propiedad " + name + " en " + target);
            return;
        }

        switch (value)
        {
            case float number:
                property.floatValue = number;
                break;
            case int number:
                property.intValue = number;
                break;
            case bool flag:
                property.boolValue = flag;
                break;
            case Vector3 vector:
                property.vector3Value = vector;
                break;
            case Color color:
                property.colorValue = color;
                break;
            case Object reference:
                property.objectReferenceValue = reference;
                break;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
