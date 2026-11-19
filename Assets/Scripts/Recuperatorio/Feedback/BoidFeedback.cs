using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoidAgent))]
public class BoidFeedback : MonoBehaviour
{
    [SerializeField] private float verticalOffset = 1.55f;
    [SerializeField] private float width = 96f;
    [SerializeField] private float height = 16f;

    private BoidAgent agent;
    private BoidFSM fsm;
    private GUIStyle style;

    private void Awake()
    {
        agent = GetComponent<BoidAgent>();
        fsm = GetComponent<BoidFSM>();
    }

    private void OnGUI()
    {
        if (agent == null || agent.IsCollected) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * verticalOffset);
        if (screen.z <= 0f) return;

        if (style == null) style = CreateStyle();
        style.normal.textColor = StateColor(fsm != null ? fsm.StateId : BoidStateId.Flocking);

        Rect rect = new Rect(
            screen.x - width * 0.5f,
            Screen.height - screen.y - height,
            width,
            height);

        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, agent.CurrentBehaviour, style);
    }

    private static GUIStyle CreateStyle()
    {
        GUIStyle created = new GUIStyle(GUI.skin.label);
        created.fontSize = 11;
        created.alignment = TextAnchor.MiddleCenter;
        return created;
    }

    private static Color StateColor(BoidStateId state)
    {
        switch (state)
        {
            case BoidStateId.Evade:
                return new Color(1f, 0.85f, 0.2f);
            case BoidStateId.Consume:
                return new Color(1f, 0.6f, 0.2f);
            case BoidStateId.SeekObject:
                return new Color(0.4f, 0.9f, 1f);
            case BoidStateId.Dead:
                return new Color(1f, 0.35f, 0.35f);
            case BoidStateId.Respawn:
                return new Color(0.7f, 0.7f, 0.7f);
            default:
                return Color.white;
        }
    }
}
