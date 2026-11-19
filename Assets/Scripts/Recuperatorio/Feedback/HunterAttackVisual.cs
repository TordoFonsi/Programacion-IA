using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HunterAttackVisual : MonoBehaviour
{
    [SerializeField] private HunterFSM hunter;
    [SerializeField] private int segments = 64;
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private float lineDuration = 0.25f;

    private LineRenderer rangeLine;
    private LineRenderer attackLine;
    private float attackTimer;

    private void Awake()
    {
        if (hunter == null) hunter = GetComponentInParent<HunterFSM>();

        rangeLine = GetComponent<LineRenderer>();
        rangeLine.loop = true;
        rangeLine.useWorldSpace = false;
        rangeLine.positionCount = segments;
        rangeLine.startWidth = lineWidth;
        rangeLine.endWidth = lineWidth;

        attackLine = CreateAttackLine();
        DrawRange();
    }

    private void OnEnable()
    {
        if (hunter != null) hunter.Attacked += ShowAttackLine;
    }

    private void OnDisable()
    {
        if (hunter != null) hunter.Attacked -= ShowAttackLine;
    }

    private void Update()
    {
        if (attackTimer <= 0f) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f) attackLine.enabled = false;
    }

    private LineRenderer CreateAttackLine()
    {
        GameObject attackObject = new GameObject("AttackLine");
        attackObject.transform.SetParent(transform);

        LineRenderer line = attackObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.sharedMaterial = rangeLine.sharedMaterial;
        line.enabled = false;
        return line;
    }

    private void DrawRange()
    {
        if (hunter == null) return;

        float radius = hunter.VisionRadius;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            rangeLine.SetPosition(i, new Vector3(x, 0.05f, z));
        }
    }

    public void ShowAttackLine(Vector3 targetPosition)
    {
        if (attackLine == null) return;

        attackLine.SetPosition(0, transform.position);
        attackLine.SetPosition(1, targetPosition);
        attackLine.enabled = true;
        attackTimer = lineDuration;
    }
}
