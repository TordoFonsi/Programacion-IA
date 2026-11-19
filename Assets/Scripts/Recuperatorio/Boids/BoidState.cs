using UnityEngine;

public abstract class BoidState : State
{
    protected readonly BoidAgent Agent;

    protected BoidState(BoidAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        Agent = agent;
    }

    protected static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
