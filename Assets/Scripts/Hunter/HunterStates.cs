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

        agent.TickInterest(agent.DeltaTime);

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
        float distance = offset.magnitude;

        if (distance <= agent.MeleeAttackRange)
        {
            agent.SetAttackMode($"Ataque melee ({distance:0.0} u)");
            agent.Halt();
            agent.Face(target.transform.position);

            if (agent.TryAttack(target))
            {
                agent.FlashAttack(target.transform.position, true);
                agent.ChangeState(HunterStateId.Search);
            }

            return;
        }

        if (distance <= agent.RangeAttackRange)
        {
            agent.SetAttackMode($"Ataque a distancia ({distance:0.0} u)");
            agent.Halt();
            agent.Face(target.transform.position);

            if (agent.TryAttack(target))
            {
                agent.FlashAttack(target.transform.position, false);
                agent.ChangeState(HunterStateId.Search);
            }

            return;
        }

        agent.SetAttackMode($"Persiguiendo boid ({distance:0.0} u)");
        agent.PursueTarget(target);
    }
}

public class HunterGatherState : State
{
    private readonly HunterAgent agent;

    public HunterGatherState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        agent.ResetGatherTimer();
    }

    public override void Update()
    {
        if (agent.TryHigherPriority(HunterStateId.Gather)) return;

        PreyAgent target = agent.FindDeadPrey();
        if (target == null)
        {
            agent.ResetGatherTimer();
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

        if (agent.GatherTick(target)) return;

        agent.ResetGatherTimer();
        agent.ChangeState(agent.Collected >= agent.Goal ? HunterStateId.Exit : HunterStateId.Search);
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
            agent.CompleteRun();
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
    private float waitTimer;

    public HunterFleeState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        repathTimer = 0f;
        waitTimer = 0f;
        agent.Halt();
    }

    public override void Update()
    {
        if (agent.IsDead) return;

        if (agent.IsInsideSafeZone())
        {
            agent.Halt();
            waitTimer += agent.DeltaTime;
            agent.FleeWait = waitTimer;

            if (waitTimer < agent.SafeZoneWaitTime) return;

            waitTimer = 0f;
            agent.FleeWait = 0f;
            agent.ChangeState(agent.PostFleeState());
            return;
        }

        waitTimer = 0f;
        agent.FleeWait = 0f;
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
        agent.SetColor(HunterAgent.DeadColor);
    }

    public override void Update()
    {
        agent.Halt();
    }
}
