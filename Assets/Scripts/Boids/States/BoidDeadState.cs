using UnityEngine;

public class BoidDeadState : BoidState
{
    public BoidDeadState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Enter()
    {
        Agent.Steering.Stop();
        Agent.CurrentInterest = null;
    }

    public override void Update()
    {
    }
}
