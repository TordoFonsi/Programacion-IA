using UnityEngine;

public class BoidConsumeState : BoidState
{
    private float timer;

    public BoidConsumeState(BoidAgent agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
    }

    public override void Enter()
    {
        timer = 0f;
        Agent.Steering.Stop();
    }

    public override void Update()
    {
        InterestObject target = Agent.CurrentInterest;
        if (target == null || target.IsDestroyed) return;

        Agent.Steering.Stop();
        Agent.Steering.FaceTowards(target.transform.position, Time.deltaTime);

        timer += Time.deltaTime;
        if (timer < Agent.ConsumeInterval) return;

        timer = 0f;
        target.TakeDamage(Agent.ConsumeDamage);
    }
}
