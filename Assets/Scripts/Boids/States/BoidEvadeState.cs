using UnityEngine;

public class BoidEvadeState : BoidState
{
    public BoidEvadeState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Update()
    {
        SteeringBody threat = Agent.GetThreat();
        if (threat == null) return;

        Vector3 force = Agent.Steering.Evade(threat.transform.position, threat.Velocity);
        force += Agent.Flocking.GetSeparation(Agent.Steering, Agent.NeighbourColliders);
        force += Agent.Steering.Bounds(Agent.Bounds, Agent.BoundsMargin);

        Agent.Steering.Move(force, Time.deltaTime);
        Agent.Steering.ClampTo(Agent.Bounds);
    }
}
