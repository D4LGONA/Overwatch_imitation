using System;
using UnityEngine;
using UnityEngine.AI;

// 지킬 곳 근처의 아무 지점으로 걸어가서, 도착하면 잠시 두리번거리고, 다시 다른 지점으로 간다.
[Serializable]
public class EnemyIdleState : EnemyState
{
    [Tooltip("지킬 곳 둘레에서 이 반경 안을 돌아다닌다.")]
    [SerializeField] private float patrolRadius = 5f;
    [Tooltip("한 자리에 도착해서 두리번거리는 시간(초). 이 사이에서 무작위.")]
    [SerializeField] private Vector2 lookAroundTime = new Vector2(2f, 4f);
    [Tooltip("두리번거릴 때 좌우로 고개를 돌리는 각도.")]
    [SerializeField] private float lookAroundAngle = 60f;
    [Tooltip("두리번거리는 빠르기. 클수록 고개를 빨리 왔다 갔다 한다.")]
    [SerializeField] private float lookAroundSpeed = 1f;

    // 목적지까지 딱 붙어서 가야 하니까 쫓을 때보다 훨씬 가깝게 멈춘다.
    private const float StopDistance = 0.3f;

    public float PatrolRadius => patrolRadius;

    // 두리번거리는 중이면 true. 도착한 순간의 방향을 기준으로 좌우를 오간다.
    private bool lookingAround;
    private float lookAroundStart;
    private float lookAroundUntil;
    private float lookBaseYaw;

    public override void Enter()
    {
        ai.Agent.stoppingDistance = StopDistance;
        lookingAround = false;
        MoveToRandomPoint();
    }

    public override void Tick()
    {
        if (ai.TargetVisible)
        {
            ai.ChangeState(ai.Chase);
            return;
        }

        if (lookingAround)
        {
            // 사인 곡선으로 기준 방향 좌우를 부드럽게 오간다.
            float t = (Time.time - lookAroundStart) * lookAroundSpeed;
            ai.LookYaw(lookBaseYaw + Mathf.Sin(t) * lookAroundAngle);

            if (Time.time >= lookAroundUntil)
            {
                lookingAround = false;
                MoveToRandomPoint();
            }
            return;
        }

        // 길을 아직 계산 중이면 남은 거리가 0으로 나와서 도착으로 착각하므로 기다린다.
        NavMeshAgent agent = ai.Agent;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            lookingAround = true;
            lookAroundStart = Time.time;
            lookAroundUntil = Time.time + UnityEngine.Random.Range(lookAroundTime.x, lookAroundTime.y);
            lookBaseYaw = ai.AimYaw;
        }
    }

    // 지킬 곳 둘레에서 걸어갈 수 있는 아무 지점이나 고른다.
    private void MoveToRandomPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = ai.Home + new Vector3(offset.x, 0f, offset.y);

            // NavMesh 바깥(벽 안, 허공)을 고르면 못 가므로 가장 가까운 걸을 수 있는 점으로 옮긴다.
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                ai.Agent.SetDestination(hit.position);
                return;
            }
        }

        ai.Agent.SetDestination(ai.Home);
    }
}
