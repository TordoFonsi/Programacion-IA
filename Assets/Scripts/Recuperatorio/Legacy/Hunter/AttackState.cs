using UnityEngine;

public class AttackState : State
{
    private FSMAgent agent;
    private BoidAgent target;
    private HunterAttackVisual attackVisual;

    public AttackState(
        FSMAgent agent,
        StateMachine stateMachine)
        : base(stateMachine)
    {
        this.agent = agent;

        attackVisual =
            agent.GetComponentInChildren<HunterAttackVisual>();
    }

    public override void Enter()
    {
        target = FindTarget();

        if (target != null)
        {
            Debug.Log(
                "Hunter: Attack -> " +
                target.name);
        }
        else
        {
            StateMachine.ChangeState(
                PoliceStates.Patrol);
        }
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
            agent.AttackExitRadius)
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

        agent.transform.forward =
            direction.normalized;
    }

    private void PerformMeleeAttack()
    {
        FaceTarget();

        if (!agent.CanAttack())
            return;

        Debug.Log(
            "Hunter: Melee Attack");

        target.TakeDamage(
            agent.AttackDamage);

        if (attackVisual != null)
        {
            attackVisual.ShowAttackLine(
                target.transform.position);
        }

        agent.ResetAttackTimer();

        StateMachine.ChangeState(
            PoliceStates.Patrol);
    }

    private void PerformRangedAttack()
    {
        FaceTarget();

        if (!agent.CanAttack())
            return;

        Debug.Log(
            "Hunter: Ranged Attack");

        target.TakeDamage(
            agent.AttackDamage);

        if (attackVisual != null)
        {
            attackVisual.ShowAttackLine(
                target.transform.position);
        }

        agent.ResetAttackTimer();

        StateMachine.ChangeState(
            PoliceStates.Patrol);
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 direction =
            target.transform.position -
            agent.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <=
            0.01f)
            return;

        agent.transform.forward =
            direction.normalized;
    }

    private BoidAgent FindTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                agent.transform.position,
                agent.PerceptionRadius,
                agent.BoidLayer);

        BoidAgent closest =
            null;

        float closestDistance =
            Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.IsDead ||
                boid.IsCollected)
                continue;

            float distance =
                Vector3.Distance(
                    agent.transform.position,
                    boid.transform.position);

            if (distance < closestDistance)
            {
                closest =
                    boid;

                closestDistance =
                    distance;
            }
        }

        return closest;
    }
}