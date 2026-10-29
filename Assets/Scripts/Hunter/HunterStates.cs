using UnityEngine;

public class HunterSearchState : State
{
    private readonly HunterAgent agent;

    public HunterSearchState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        agent.SearchWait = 0.5f;
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Search)) return;

        if (agent.Vision.HasTarget)
        {
            agent.SetDestination(agent.Vision.LastKnownPosition);
            agent.FollowPath(agent.DeltaTime);
            return;
        }

        if (agent.HasArrived)
        {
            agent.SearchWait -= agent.DeltaTime;
            if (agent.SearchWait > 0f) return;

            agent.SetDestination(agent.RandomSearchPoint());
            agent.SearchWait = 1.5f;
        }

        agent.FollowPath(agent.DeltaTime);
    }
}

public class HunterAttackState : State
{
    private readonly HunterAgent agent;

    public HunterAttackState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Attack)) return;

        PreyAgent target = agent.Vision.Target;
        if (target == null || !target.IsActive || !agent.Vision.IsVisible)
        {
            agent.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 offset = target.transform.position - agent.transform.position;
        offset.y = 0f;

        if (offset.magnitude <= agent.AttackRange)
        {
            agent.Halt();
            agent.Face(target.transform.position);
            agent.TryAttack(target);
            return;
        }

        agent.SetDestination(target.transform.position);
        agent.FollowPath(agent.DeltaTime);
    }
}

public class HunterGatherState : State
{
    private readonly HunterAgent agent;

    public HunterGatherState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Gather)) return;

        PreyAgent target = agent.FindDeadPrey();
        if (target == null)
        {
            agent.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 offset = target.transform.position - agent.transform.position;
        offset.y = 0f;

        if (offset.magnitude > agent.GatherRange)
        {
            agent.SetDestination(target.transform.position);
            agent.FollowPath(agent.DeltaTime);
            return;
        }

        agent.Halt();
        agent.Face(target.transform.position);

        if (!agent.GatherTick(target)) agent.ChangeState(HunterStateId.Search);
    }
}

public class HunterExitState : State
{
    private readonly HunterAgent agent;

    public HunterExitState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Exit)) return;

        if (agent.IsInsideExitZone())
        {
            agent.MarkEscaped();
            return;
        }

        agent.SetDestination(agent.ExitPosition);
        agent.FollowPath(agent.DeltaTime);
    }
}

public class HunterFleeState : State
{
    private readonly HunterAgent agent;
    private float repathTimer;

    public HunterFleeState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        repathTimer = 0f;
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Flee)) return;

        if (agent.IsInsideSafeZone())
        {
            agent.Halt();
            return;
        }

        repathTimer -= agent.DeltaTime;
        if (repathTimer <= 0f)
        {
            agent.SetDestination(agent.NearestSafeZone());
            repathTimer = 0.5f;
        }

        agent.FollowPath(agent.DeltaTime);
    }
}

public class HunterDeadState : State
{
    private readonly HunterAgent agent;

    public HunterDeadState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        agent.Halt();
        agent.SetColor(new Color(0.25f, 0.22f, 0.2f));
    }

    public override void Update()
    {
        agent.Halt();
    }
}
