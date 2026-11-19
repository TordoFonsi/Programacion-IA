using UnityEngine;

public class HunterHuntState : HunterState
{
    public HunterHuntState(HunterFSM hunter, StateMachine stateMachine) : base(hunter, stateMachine)
    {
    }

    public override void Enter()
    {
        Hunter.AcquireTarget();
    }

    public override void Exit()
    {
        Hunter.ClearTarget();
    }

    public override void Update()
    {
        Hunter.DrainEnergy(Hunter.HuntDrain * Time.deltaTime);

        BoidAgent target = Hunter.Target;
        if (target == null) return;

        Vector3 targetPosition = target.transform.position;
        float distance = FlatDistance(Hunter.transform.position, targetPosition);

        if (distance > Hunter.RangeRadius)
        {
            Chase(target, targetPosition);
            return;
        }

        Hunter.Steering.Stop();
        Hunter.Steering.FaceTowards(targetPosition, Time.deltaTime);

        TryAttack(target, targetPosition, distance);
    }

    private void Chase(BoidAgent target, Vector3 targetPosition)
    {
        Vector3 force = Hunter.Steering.Pursuit(targetPosition, target.Steering.Velocity);
        force += Hunter.Steering.Bounds(Hunter.Bounds, Hunter.BoundsMargin);
        Hunter.Move(force);
    }

    private void TryAttack(BoidAgent target, Vector3 targetPosition, float distance)
    {
        if (!Hunter.CanAttack()) return;

        int damage = distance <= Hunter.MeleeRadius ? Hunter.MeleeDamage : Hunter.RangedDamage;
        target.TakeDamage(damage);
        Hunter.ResetAttackTimer();
        Hunter.NotifyAttack(targetPosition);
    }
}
