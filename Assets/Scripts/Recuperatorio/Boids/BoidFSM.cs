using UnityEngine;

public enum BoidStateId
{
    Dead,
    Respawn,
    Evade,
    Consume,
    SeekObject,
    Flocking
}

[DisallowMultipleComponent]
public class BoidFSM : MonoBehaviour
{
    private BoidAgent agent;
    private StateMachine machine;

    public BoidStateId StateId { get; private set; } = BoidStateId.Flocking;
    public string CurrentStateName => StateId.ToString();

    private void Awake()
    {
        agent = GetComponent<BoidAgent>();
        machine = new StateMachine();

        machine.RegisterState(BoidStateId.Dead, new BoidDeadState(agent, machine));
        machine.RegisterState(BoidStateId.Respawn, new BoidRespawnState(agent, machine));
        machine.RegisterState(BoidStateId.Evade, new BoidEvadeState(agent, machine));
        machine.RegisterState(BoidStateId.Consume, new BoidConsumeState(agent, machine));
        machine.RegisterState(BoidStateId.SeekObject, new BoidSeekObjectState(agent, machine));
        machine.RegisterState(BoidStateId.Flocking, new BoidFlockingState(agent, machine));

        machine.ChangeState(BoidStateId.Flocking);
    }

    private void Update()
    {
        BoidStateId next = Evaluate();
        machine.ChangeState(next);
        StateId = next;
        machine.Update();
    }

    private BoidStateId Evaluate()
    {
        if (agent.IsCollected) return BoidStateId.Respawn;
        if (agent.IsDead) return BoidStateId.Dead;
        if (agent.GetThreat() != null) return BoidStateId.Evade;

        InterestObject target = agent.CurrentInterest;
        if (target == null || target.IsDestroyed)
        {
            target = agent.FindInterest();
            agent.CurrentInterest = target;
        }

        if (target == null) return BoidStateId.Flocking;

        float distance = Vector3.Distance(agent.transform.position, target.transform.position);
        return distance <= agent.ConsumeDistance
            ? BoidStateId.Consume
            : BoidStateId.SeekObject;
    }
}
