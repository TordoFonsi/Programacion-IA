using UnityEngine;

public class HunterGatherBoidState : HunterState
{
    private BoidAgent target;
    private float timer;

    public HunterGatherBoidState(HunterFSM hunter, StateMachine stateMachine) : base(hunter, stateMachine)
    {
    }

    public override void Enter()
    {
        timer = 0f;
    }

    public override void Exit()
    {
        target = null;
    }

    public override void Update()
    {
        if (target == null || !target.IsDead || target.IsCollected)
        {
            target = Hunter.FindDeadBoid();
            timer = 0f;
        }

        if (target == null)
        {
            Hunter.Steering.Stop();
            return;
        }

        Vector3 targetPosition = target.transform.position;
        float distance = FlatDistance(Hunter.transform.position, targetPosition);

        if (distance > Hunter.GatherDistance)
        {
            MoveTo(targetPosition);
            return;
        }

        Hunter.Steering.Stop();
        Hunter.Steering.FaceTowards(targetPosition, Time.deltaTime);

        timer += Time.deltaTime;
        if (timer < Hunter.GatherTime) return;

        target.Collect();
        target = null;
        timer = 0f;
    }

    private void MoveTo(Vector3 targetPosition)
    {
        float stopDistance = Hunter.GatherDistance * 0.6f;
        Vector3 force = Hunter.Steering.Arrive(targetPosition, stopDistance, Hunter.ArriveSlowRadius);
        force += Hunter.Steering.Bounds(Hunter.Bounds, Hunter.BoundsMargin);
        Hunter.Move(force);
        timer = 0f;
    }
}
