using UnityEngine;

public class HunterFeedback : MonoBehaviour
{
    [SerializeField] private HunterFSM hunter;

    private GUIStyle style;

    private void Awake()
    {
        if (hunter == null) hunter = GetComponent<HunterFSM>();
    }

    private void OnGUI()
    {
        if (hunter == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
        }

        string text =
            "CAZADOR: " + hunter.CurrentStateName +
            "\nEnergia: " + Mathf.RoundToInt(hunter.Energy) + " / " + Mathf.RoundToInt(hunter.MaxEnergy);

        GUI.Box(new Rect(10f, 10f, 230f, 50f), GUIContent.none);
        GUI.Label(new Rect(18f, 15f, 220f, 44f), text, style);
    }
}
