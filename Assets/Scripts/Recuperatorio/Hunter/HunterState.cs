using UnityEngine;

public abstract class HunterState : State
{
    protected readonly HunterFSM Hunter;

    protected HunterState(HunterFSM hunter, StateMachine stateMachine) : base(stateMachine)
    {
        Hunter = hunter;
    }

    protected static float FlatDistance(Vector3 from, Vector3 to)
    {
        Vector3 offset = to - from;
        offset.y = 0f;
        return offset.magnitude;
    }
}
