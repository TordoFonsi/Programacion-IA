using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class HealthFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Health Bar")]
    [SerializeField] private float barWidth = 46f;
    [SerializeField] private float barHeight = 6f;
    [SerializeField] private float barOffset = 1.2f;
    [SerializeField] private Color fullColor = new Color(0.25f, 0.95f, 0.35f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.25f, 0.2f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Health health;
    private BoidAgent agent;
    private Renderer targetRenderer;
    private MaterialPropertyBlock block;
    private Color baseColor;
    private float flashTimer;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<BoidAgent>();
        targetRenderer = GetComponentInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
        baseColor = ReadBaseColor();
    }

    private void OnEnable()
    {
        health.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= HandleDamaged;
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f)
        {
            RestoreColor();
            return;
        }

        float progress = 1f - flashTimer / flashDuration;
        ApplyColor(Color.Lerp(flashColor, baseColor, progress));
    }

    private void OnGUI()
    {
        if (!IsVisible()) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * barOffset);
        if (screen.z <= 0f) return;

        float fraction = health.Max > 0 ? (float)health.Current / health.Max : 0f;
        float x = screen.x - barWidth * 0.5f;
        float y = Screen.height - screen.y - barHeight * 0.5f;

        Rect background = new Rect(x - 1f, y - 1f, barWidth + 2f, barHeight + 2f);
        Rect fill = new Rect(x, y, barWidth * Mathf.Clamp01(fraction), barHeight);

        Color previous = GUI.color;

        GUI.color = backgroundColor;
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = Color.Lerp(lowColor, fullColor, fraction);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = previous;
    }

    private bool IsVisible()
    {
        if (health == null || health.IsDead) return false;
        if (agent != null && agent.IsCollected) return false;
        return true;
    }

    private void HandleDamaged()
    {
        flashTimer = flashDuration;
        ApplyColor(flashColor);
    }

    private Color ReadBaseColor()
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null) return Color.white;

        Material material = targetRenderer.sharedMaterial;
        return material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : material.color;
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null) return;

        block.SetColor(BaseColorId, color);
        targetRenderer.SetPropertyBlock(block);
    }

    private void RestoreColor()
    {
        if (targetRenderer == null) return;
        targetRenderer.SetPropertyBlock(null);
    }
}
