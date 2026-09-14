using UnityEngine;

public enum PoliceStates
{
    Idle,
    Patrol
}

public class FSMAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Patrol")]
    [SerializeField] private PatrolData dataPatrol;

    private StateMachine stateMachine;

    public float Speed => speed;

    private void Awake()
    {
        stateMachine = new StateMachine();

        IdleState idleState =
            new IdleState(stateMachine);

        PatrolState patrolState =
            new PatrolState(
                this,
                dataPatrol,
                stateMachine);

        stateMachine.RegisterState(
            PoliceStates.Idle,
            idleState);

        stateMachine.RegisterState(
            PoliceStates.Patrol,
            patrolState);

        stateMachine.ChangeState(
            PoliceStates.Idle);
    }

    private void Update()
    {
        stateMachine.Update();
    }
}