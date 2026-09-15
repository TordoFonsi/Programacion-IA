using UnityEngine;

public class AttackState : State
{
    private FSMAgent agent;

    public AttackState(
        FSMAgent agent,
        StateMachine stateMachine)
        : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        Debug.Log("Hunter: Attack");
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        Debug.Log("Hunter: Exit Attack");
    }
}