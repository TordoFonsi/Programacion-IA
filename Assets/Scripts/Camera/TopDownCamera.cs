using UnityEngine;

/// <summary>
/// Cenital (top-down) camera. Orthographic, it frames the whole level and
/// reserves a strip on the right for the colour legend, so the HUD never
/// covers the map. The size adapts to the current Game view aspect.
/// </summary>
[RequireComponent(typeof(Camera))]
public class TopDownCamera : MonoBehaviour
{
    [SerializeField] private float padding = 3f;
    [SerializeField] private float height = 90f;
    [SerializeField, Range(0f, 0.45f)] private float rightInset = 0.20f;

    private Camera cam;
    private float lastAspect = -1f;
    private int lastWidth = -1;

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (Mathf.Abs(cam.aspect - lastAspect) > 0.001f || Screen.width != lastWidth) Apply();
    }

    public void Apply()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        float inset = Mathf.Clamp(rightInset, 0f, 0.45f);
        float usableAspect = Mathf.Max(0.0001f, cam.aspect * (1f - inset));

        float halfHeight = LevelLayout.Height * 0.5f + padding;
        float halfWidth = (LevelLayout.Width * 0.5f + padding) / usableAspect;

        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = height * 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = LevelPalette.Background;

        float visibleWidth = 2f * cam.orthographicSize * cam.aspect;
        float centerX = LevelLayout.Width * 0.5f + visibleWidth * inset * 0.5f;

        transform.position = new Vector3(centerX, height, LevelLayout.Height * 0.5f);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        lastAspect = cam.aspect;
        lastWidth = Screen.width;
    }
}
