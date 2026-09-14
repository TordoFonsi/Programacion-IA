using UnityEngine;

public class IdleState : State
{
    private float timeToPatrol = 3f;
    private float timer;

    public IdleState(StateMachine stateMachine)
        : base(stateMachine)
    {
    }

    public override void Enter()
    {
        timer = 0f;

        Debug.Log("Hunter: Idle");
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeToPatrol)
        {
            StateMachine.ChangeState(
                PoliceStates.Patrol);
        }
    }

    public override void Exit()
    {
        Debug.Log("Hunter: Exit Idle");
    }
}