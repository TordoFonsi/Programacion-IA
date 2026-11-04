using UnityEngine;

public class DetectionMarker : MonoBehaviour
{
    [SerializeField] private PreyAgent prey;
    [SerializeField] private HunterAgent hunter;
    [SerializeField] private Transform icon;
    [SerializeField] private Renderer iconRenderer;

    public void Configure(PreyAgent preyReference, HunterAgent hunterReference, Transform iconReference)
    {
        prey = preyReference;
        hunter = hunterReference;
        icon = iconReference;
        iconRenderer = icon != null ? icon.GetComponent<Renderer>() : null;
    }

    private void Update()
    {
        if (prey == null || hunter == null || icon == null || iconRenderer == null) return;

        bool detected = prey.IsActive &&
                        hunter.Vision != null &&
                        hunter.Vision.IsVisible &&
                        hunter.Vision.Target == prey;

        if (iconRenderer.enabled != detected) iconRenderer.enabled = detected;
        if (!detected) return;

        icon.position = prey.transform.position + Vector3.up * 1.1f;
        icon.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
    }
}
