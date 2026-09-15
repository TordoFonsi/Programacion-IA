using System.Collections.Generic;
using UnityEngine;

public class PatrolState : State
{
    private FSMAgent agent;
    private PatrolData data;
    private int currentNode;

    public PatrolState(
        FSMAgent agent,
        PatrolData data,
        StateMachine stateMachine)
        : base(stateMachine)
    {
        this.agent = agent;
        this.data = data;

        currentNode = 0;
    }

    public override void Enter()
    {
        Debug.Log("Hunter: Patrol");
    }

    public override void Update()
    {
        if (agent.HasDeadBoidInRange())
        {
            StateMachine.ChangeState(
                PoliceStates.Gather);

            return;
        }

        if (agent.CanAttack() &&
            agent.HasBoidInRange())
        {
            StateMachine.ChangeState(
                PoliceStates.Attack);

            return;
        }

        if (agent.CanSpawnInterestObject())
        {
            GameObject.Instantiate(
                agent.InterestObjectPrefab,
                agent.GetRandomSpawnPosition(),
                Quaternion.identity);
        }

        Patrol();
    }

    private void Patrol()
    {
        if (data == null ||
            data.wayPoints == null ||
            data.wayPoints.Count == 0)
        {
            return;
        }

        Transform waypoint =
            data.wayPoints[currentNode];

        Vector3 direction =
            waypoint.position -
            data.transform.position;

        direction.y = 0f;

        if (direction.magnitude <=
            data.waypointCheckDistance)
        {
            currentNode++;

            if (currentNode >= data.wayPoints.Count)
                currentNode = 0;

            return;
        }

        data.transform.position +=
            direction.normalized *
            agent.Speed *
            Time.deltaTime;
    }
}

[System.Serializable]
public class PatrolData
{
    public List<Transform> wayPoints;
    public Transform transform;
    public float waypointCheckDistance;
}