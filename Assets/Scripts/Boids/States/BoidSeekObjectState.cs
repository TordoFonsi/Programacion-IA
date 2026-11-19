using UnityEngine;

public class BoidSeekObjectState : BoidState
{
    public BoidSeekObjectState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Update()
    {
        InterestObject target = Agent.CurrentInterest;
        if (target == null || target.IsDestroyed) return;

        Vector3 force = Agent.Steering.Arrive(
            target.transform.position,
            Agent.ConsumeDistance,
            Agent.ArriveSlowRadius);

        force += Agent.Flocking.GetSeparation(Agent.Steering, Agent.NeighbourColliders);
        force += Agent.Steering.Bounds(Agent.Bounds, Agent.BoundsMargin);

        Agent.Steering.Move(force, Time.deltaTime);
        Agent.Steering.ClampTo(Agent.Bounds);
    }
}
