using TMPro;
using UnityEngine;

public class HunterFeedback : MonoBehaviour
{
    [SerializeField] private FSMAgent agent;
    [SerializeField] private TMP_Text stateText;

    private void Update()
    {
        if (agent == null ||
            stateText == null)
        {
            return;
        }

        stateText.text =
            "STATE: " +
            agent.CurrentStateName;
    }
}