using UnityEngine;

public class HunterPatrolState : HunterState
{
    public HunterPatrolState(HunterFSM hunter, StateMachine stateMachine) : base(hunter, stateMachine)
    {
    }

    public override void Update()
    {
        Hunter.TrySpawnInterest();

        Transform waypoint = Hunter.CurrentWaypoint;
        if (waypoint == null)
        {
            Hunter.Steering.Stop();
            return;
        }

        if (FlatDistance(Hunter.transform.position, waypoint.position) <= Hunter.WaypointReachedDistance)
            Hunter.AdvanceWaypoint();

        Vector3 force = Hunter.Steering.Arrive(waypoint.position, 0.5f, Hunter.ArriveSlowRadius);
        force += Hunter.Steering.Bounds(Hunter.Bounds, Hunter.BoundsMargin);
        Hunter.Move(force);
    }
}
