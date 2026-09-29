using UnityEngine;
using UnityEngine.AI;

// 적 AI. 상태 하나에 할 일 하나만 두고, 조건이 맞으면 다른 상태로 넘어간다.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        Guard,  // 지키는 자리에 서 있는다
        Chase,  // 상대를 쫓아간다
    }

    [Header("Sight")]
    [SerializeField] private float sightRange = 20f;
    [Tooltip("눈 높이. 여기서 상대 가슴 쪽으로 선을 그어 가리는 게 없으면 보인다고 친다.")]
    [SerializeField] private float eyeHeight = 1.6f;
    [Tooltip("시야를 가리는 것. 벽, 바닥, 캐릭터 레이어를 넣는다.")]
    [SerializeField] private LayerMask sightMask = ~0;
    [Tooltip("상대를 놓친 뒤에도 마지막으로 본 자리까지 쫓아가는 시간.")]
    [SerializeField] private float loseSightTime = 3f;

    [Header("Guard")]
    [Tooltip("지킬 자리. 비워두면 처음 서 있던 자리.")]
    [SerializeField] private Transform guardPoint;

    // 매 프레임 씬을 뒤지면 무거워서 이 간격으로만 상대를 찾는다.
    private const float ScanInterval = 0.2f;

    private NavMeshAgent agent;
    private Health health;
    private State state;
    private Vector3 home;

    private Health target;
    private bool targetVisible;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float nextScanTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        home = guardPoint != null ? guardPoint.position : transform.position;
    }

    private void OnEnable() => health.Revived += OnRevived;

    private void OnDisable()
    {
        if (health != null)
            health.Revived -= OnRevived;
    }

    // 부활하면 쫓던 상대를 잊고 다시 지키는 것부터 시작한다.
    private void OnRevived()
    {
        target = null;
        targetVisible = false;
        state = State.Guard;
    }

    private void Update()
    {
        if (!health.IsAlive)
            return;

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + ScanInterval;
            Scan();
        }

        switch (state)
        {
            case State.Guard: UpdateGuard(); break;
            case State.Chase: UpdateChase(); break;
        }
    }

    private void UpdateGuard()
    {
        agent.SetDestination(home);

        if (targetVisible)
            state = State.Chase;
    }

    private void UpdateChase()
    {
        if (target == null || !target.IsAlive)
        {
            state = State.Guard;
            return;
        }

        // 보이는 동안은 상대에게, 안 보이면 마지막으로 본 자리로 간다.
        agent.SetDestination(targetVisible ? target.transform.position : lastSeenPosition);

        if (!targetVisible && Time.time - lastSeenTime > loseSightTime)
            state = State.Guard;
    }

    // 살아 있는 다른 팀 중 보이는 가장 가까운 상대를 고른다.
    private void Scan()
    {
        Health best = null;
        float bestDistance = float.MaxValue;

        foreach (Health candidate in FindObjectsOfType<Health>())
        {
            if (candidate == health || !candidate.IsAlive || candidate.Team == health.Team)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < bestDistance && CanSee(candidate, distance))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        targetVisible = best != null;
        if (targetVisible)
        {
            target = best;
            lastSeenPosition = best.transform.position;
            lastSeenTime = Time.time;
        }
    }

    private bool CanSee(Health candidate, float distance)
    {
        if (distance > sightRange)
            return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 chest = candidate.transform.position + Vector3.up * 1.2f;

        // 선이 처음 닿은 게 그 상대 자신이면 가리는 게 없다는 뜻이다. 눈이 자기 몸
        // 안에서 출발하므로 자기 콜라이더에는 닿지 않는다.
        return Physics.Linecast(eye, chest, out RaycastHit hit, sightMask, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponentInParent<Health>() == candidate;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (Application.isPlaying && target != null)
        {
            Gizmos.color = targetVisible ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, target.transform.position + Vector3.up * 1.2f);
        }
    }
}
