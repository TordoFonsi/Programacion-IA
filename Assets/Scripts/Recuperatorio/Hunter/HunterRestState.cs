using UnityEngine;

public class HunterRestState : HunterState
{
    public HunterRestState(HunterFSM hunter, StateMachine stateMachine) : base(hunter, stateMachine)
    {
    }

    public override void Enter()
    {
        Hunter.Steering.Stop();
    }

    public override void Update()
    {
        Hunter.RechargeEnergy(Hunter.RestRecharge * Time.deltaTime);
    }
}
