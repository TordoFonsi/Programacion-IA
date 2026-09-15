using UnityEngine;

public class AttackState : State
{
    private FSMAgent agent;
    private BoidAgent target;

    public AttackState(
        FSMAgent agent,
        StateMachine stateMachine)
        : base(stateMachine)
    {
        this.agent = agent;
    }

    public override void Enter()
    {
        target = FindTarget();

        Debug.Log("Hunter: Attack");
    }

    public override void Exit()
    {
        target = null;

        Debug.Log(
            "Hunter: Exit Attack");
    }

    public override void Update()
    {
        if (target == null ||
            target.IsDead ||
            target.IsCollected)
        {
            StateMachine.ChangeState(
                PoliceStates.Patrol);

            return;
        }

        float distance =
            Vector3.Distance(
                agent.transform.position,
                target.transform.position);

        if (distance >
            agent.PerceptionRadius)
        {
            StateMachine.ChangeState(
                PoliceStates.Patrol);

            return;
        }

        if (distance <=
            agent.MeleeAttackRadius)
        {
            PerformMeleeAttack();
        }
        else if (distance <=
                 agent.RangeAttackRadius)
        {
            PerformRangedAttack();
        }
        else
        {
            Chase();
        }
    }

    private void Chase()
    {
        Vector3 direction =
            target.transform.position -
            agent.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <=
            0.01f)
            return;

        agent.transform.position +=
            direction.normalized *
            agent.Speed *
            Time.deltaTime;
    }

    private void PerformMeleeAttack()
    {
        if (!agent.CanAttack())
            return;

        Debug.Log(
            "Hunter: Melee Attack");

        target.TakeDamage(
            agent.AttackDamage);

        agent.ResetAttackTimer();

        StateMachine.ChangeState(
            PoliceStates.Patrol);
    }

    private void PerformRangedAttack()
    {
        if (!agent.CanAttack())
            return;

        Debug.Log(
            "Hunter: Ranged Attack");

        target.TakeDamage(
            agent.AttackDamage);

        agent.ResetAttackTimer();

        StateMachine.ChangeState(
            PoliceStates.Patrol);
    }

    private BoidAgent FindTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                agent.transform.position,
                agent.PerceptionRadius,
                agent.BoidLayer);

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.IsDead ||
                boid.IsCollected)
                continue;

            return boid;
        }

        return null;
    }
}