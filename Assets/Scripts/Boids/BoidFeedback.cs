using System.Text;
using TMPro;
using UnityEngine;

public class BoidFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text boidText;

    private BoidAgent[] boids;

    private void Start()
    {
        boids =
            FindObjectsByType<BoidAgent>(
                FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (boidText == null)
            return;

        StringBuilder text =
            new StringBuilder();

        int active = 0;
        int dead = 0;
        int collected = 0;

        text.AppendLine("BOIDS");
        text.AppendLine();

        foreach (BoidAgent boid in boids)
        {
            if (boid == null)
                continue;

            text.AppendLine(
                boid.name +
                ": " +
                boid.CurrentBehaviour);

            if (boid.IsCollected)
            {
                collected++;
            }
            else if (boid.IsDead)
            {
                dead++;
            }
            else
            {
                active++;
            }
        }

        text.AppendLine();
        text.AppendLine(
            "Active: " +
            active);

        text.AppendLine(
            "Dead: " +
            dead);

        text.AppendLine(
            "Collected: " +
            collected);

        boidText.text =
            text.ToString();
    }
}