using UnityEngine;

public enum PoliceStates
{
    Idle,
    Patrol,
    Attack
}

public class FSMAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Patrol")]
    [SerializeField] private PatrolData dataPatrol;

    [Header("Attack")]
    [SerializeField] private float tba = 3f;
    [SerializeField] private float rangeAttackRadius = 10f;
    [SerializeField] private float meleeAttackRadius = 2f;
    [SerializeField] private int attackDamage = 25;

    private StateMachine stateMachine;
    private float attackTimer;

    public float Speed => speed;
    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public int AttackDamage => attackDamage;

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

        AttackState attackState =
            new AttackState(
                this,
                stateMachine);

        stateMachine.RegisterState(
            PoliceStates.Idle,
            idleState);

        stateMachine.RegisterState(
            PoliceStates.Patrol,
            patrolState);

        stateMachine.RegisterState(
            PoliceStates.Attack,
            attackState);

        stateMachine.ChangeState(
            PoliceStates.Idle);
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        stateMachine.Update();
    }

    public bool CanAttack()
    {
        return attackTimer >= tba;
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0f;
    }
}