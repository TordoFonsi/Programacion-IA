using UnityEngine;

public class GatherState : State
{
    private FSMAgent agent;
    private BoidAgent target;
    private float timer;

    public GatherState(
        FSMAgent agent,
        StateMachine stateMachine)
        : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        target = FindDeadBoid();
        timer = 0f;

        Debug.Log("Hunter: Gather");
    }

    public override void Exit()
    {
        target = null;

        Debug.Log(
            "Hunter: Exit Gather");
    }

    public override void Update()
    {
        if (target == null ||
            !target.IsDead ||
            target.IsCollected)
        {
            StateMachine.ChangeState(
                PoliceStates.Patrol);

            return;
        }

        Vector3 direction =
            target.transform.position -
            agent.transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        if (distance > 1.5f)
        {
            agent.transform.position +=
                direction.normalized *
                agent.Speed *
                Time.deltaTime;

            return;
        }

        timer += Time.deltaTime;

        if (timer >= agent.GatherTime)
        {
            target.Collected();

            StateMachine.ChangeState(
                PoliceStates.Patrol);
        }
    }

    private BoidAgent FindDeadBoid()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                agent.transform.position,
                agent.PerceptionRadius,
                agent.BoidLayer);

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.IsDead &&
                !boid.IsCollected)
            {
                return boid;
            }
        }

        return null;
    }
}