using UnityEngine;

public class BoidRespawnState : BoidState
{
    private float timer;

    public BoidRespawnState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Enter()
    {
        timer = 0f;
        Agent.Steering.Stop();
        Agent.SetVisible(false);
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= Agent.RespawnDelay) Agent.RespawnAtRandom();
    }
}
