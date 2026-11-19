using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HunterAttackVisual : MonoBehaviour
{
    [SerializeField] private FSMAgent agent;
    [SerializeField] private int segments = 64;

    [Header("Attack Line")]
    [SerializeField] private float lineDuration = 0.25f;

    private LineRenderer rangeLine;
    private LineRenderer attackLine;

    private float attackTimer;

    private void Awake()
    {
        rangeLine =
            GetComponent<LineRenderer>();

        rangeLine.loop = true;
        rangeLine.useWorldSpace = false;
        rangeLine.positionCount =
            segments;

        DrawPerceptionRange();

        GameObject attackObject =
            new GameObject(
                "AttackLine");

        attackObject.transform.SetParent(
            transform);

        attackLine =
            attackObject.AddComponent<LineRenderer>();

        attackLine.positionCount = 2;
        attackLine.useWorldSpace = true;
        attackLine.enabled = false;
    }

    private void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;

            if (attackTimer <= 0f)
            {
                attackLine.enabled = false;
            }
        }
    }

    private void DrawPerceptionRange()
    {
        if (agent == null)
            return;

        float radius =
            agent.PerceptionRadius;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * Mathf.PI * 2f /
                segments;

            float x =
                Mathf.Cos(angle) *
                radius;

            float z =
                Mathf.Sin(angle) *
                radius;

            rangeLine.SetPosition(
                i,
                new Vector3(
                    x,
                    0.05f,
                    z));
        }
    }

    public void ShowAttackLine(
        Vector3 targetPosition)
    {
        if (attackLine == null)
            return;

        attackLine.SetPosition(
            0,
            transform.position);

        attackLine.SetPosition(
            1,
            targetPosition);

        attackLine.enabled = true;

        attackTimer =
            lineDuration;
    }
}
