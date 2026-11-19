using UnityEngine;

public class BoidFlockingState : BoidState
{
    public BoidFlockingState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Update()
    {
        Vector3 force = Agent.Flocking.GetFlocking(Agent.Steering, Agent.NeighbourColliders);
        force += Agent.Steering.Bounds(Agent.Bounds, Agent.BoundsMargin);
        Agent.Steering.Move(force, Time.deltaTime);
        Agent.Steering.ClampTo(Agent.Bounds);
    }
}
