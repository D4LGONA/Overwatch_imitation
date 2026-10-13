using UnityEngine;
using UnityEngine.AI;

// 적 AI의 상태 기계. 지금 상태 하나만 들고 매 프레임 그 상태의 Tick을 부른다.
// 상태들이 같이 쓰는 시야, 조준, 길찾기는 여기에 두고, 무엇을 할지는 각 상태가 정한다.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Sight")]
    [SerializeField] private float sightRange = 20f;
    [Tooltip("시야 원뿔의 전체 각도. 상체가 보는 방향 기준으로 이 안에 들어와야 보인다.")]
    [Range(1f, 360f)]
    [SerializeField] private float viewAngle = 110f;
    [Tooltip("눈 높이. 여기서 상대 가슴 쪽으로 선을 그어 가리는 게 없으면 보인다고 친다.")]
    [SerializeField] private float eyeHeight = 1.6f;
    [Tooltip("시야를 가리는 것. 벽, 바닥, 캐릭터 레이어를 넣는다.")]
    [SerializeField] private LayerMask sightMask = ~0;

    [Header("Aim")]
    [Tooltip("조준(상체)이 목표를 향해 초당 몇 도씩 도는지. 즉시 따라가면 기계 같고, 점멸로 피하는 의미가 없어진다.")]
    [SerializeField] private float aimTurnSpeed = 220f;

    [Header("Guard")]
    [Tooltip("지킬 곳. 거점을 넣는다. 비워두면 처음 서 있던 자리.")]
    [SerializeField] private Transform guardPoint;

    [Header("States")]
    [SerializeField] private EnemyIdleState idle = new EnemyIdleState();
    [SerializeField] private EnemyChaseState chase = new EnemyChaseState();
    [SerializeField] private EnemyAlertState alert = new EnemyAlertState();
    [SerializeField] private EnemyAttackState attack = new EnemyAttackState();

    [Header("Animation")]
    [Tooltip("비워두면 자식에서 찾는다. AC_Player처럼 MoveX, MoveY 파라미터가 있어야 한다.")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDamp = 0.1f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    // 매 프레임 씬을 뒤지면 무거워서 이 간격으로만 상대를 찾는다.
    private const float ScanInterval = 0.2f;
    private const float ChestHeight = 1.2f;

    private Health health;
    private UpperBodyAim aim;
    private ArmAim armAim;
    private EnemyState current;
    private float nextScanTime;

    private float aimPitch;
    private bool lookRequested;
    private float wantYaw;
    private float wantPitch;

    public NavMeshAgent Agent { get; private set; }
    public Vector3 Home { get; private set; }
    public Health Target { get; private set; }
    public bool TargetVisible { get; private set; }
    public float AimYaw { get; private set; }

    public EnemyIdleState Idle => idle;
    public EnemyChaseState Chase => chase;
    public EnemyAlertState Alert => alert;
    public EnemyAttackState Attack => attack;

    public Health Self => health;
    public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    // 지금 실제로 겨누고 있는 방향. 조준이 천천히 돌기 때문에 목표 방향과 다를 수 있다.
    public Vector3 AimDirection => Quaternion.Euler(aimPitch, AimYaw, 0f) * Vector3.forward;

    // 지금 겨누는 방향이 보려는 방향에서 좌우로 몇 도 벗어나 있는지.
    public float AimError => Mathf.Abs(Mathf.DeltaAngle(AimYaw, wantYaw));

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        Home = guardPoint != null ? guardPoint.position : transform.position;

        aim = GetComponent<UpperBodyAim>();
        if (aim == null)
            aim = gameObject.AddComponent<UpperBodyAim>();

        armAim = GetComponent<ArmAim>();
        if (armAim == null)
            armAim = gameObject.AddComponent<ArmAim>();

        // 에이전트가 몸을 이동 방향으로 돌려버리면 상체가 먼저 도는 연출이 안 된다.
        // 몸 회전은 UpperBodyAim에 맡긴다.
        Agent.updateRotation = false;
        AimYaw = transform.eulerAngles.y;

        idle.Init(this);
        chase.Init(this);
        alert.Init(this);
        attack.Init(this);
    }

    private void OnEnable()
    {
        health.Revived += OnRevived;
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.Revived -= OnRevived;
        health.Damaged -= OnDamaged;
    }

    // 맞으면 때린 쪽을 돌아본다. 이미 쫓거나 싸우고 있으면 그대로 둔다.
    private void OnDamaged()
    {
        Health attacker = health.LastAttacker;
        if (attacker == null || !health.IsAlive || current == chase || current == attack)
            return;

        alert.SetAlert(attacker.transform.position);
        if (current != alert)
            ChangeState(alert);
    }

    private void Start()
    {
        ChangeState(idle);
    }

    // 부활하면 쫓던 상대를 잊고 다시 지키는 것부터 시작한다.
    private void OnRevived()
    {
        Target = null;
        TargetVisible = false;
        ChangeState(idle);
    }

    public void ChangeState(EnemyState next)
    {
        current?.Exit();
        current = next;
        current.Enter();
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

        // 상태가 이번 프레임에 어디를 볼지 정하지 않으면 걸어가는 방향을 본다.
        lookRequested = false;
        current.Tick();
        UpdateAim();
        UpdateAnimation();
    }

    // 걷는 속도를 몸 기준으로 바꿔 넘긴다. 상체가 상대를 보며 옆으로 걸으면 옆걸음이 나온다.
    // 블렌드 트리가 -1~1 범위라 최고 속도로 나눠서 맞춘다.
    private void UpdateAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        float maxSpeed = Mathf.Max(Agent.speed, 0.01f);
        Vector3 local = transform.InverseTransformDirection(Agent.velocity) / maxSpeed;

        animator.SetFloat(MoveX, local.x, animationDamp, Time.deltaTime);
        animator.SetFloat(MoveY, local.z, animationDamp, Time.deltaTime);
    }

    // ---------- 조준 ----------

    public void SetArmAiming(bool on)
    {
        armAim.SetAiming(on);
    }

    // 상태가 이번 프레임에 볼 방향을 정한다.
    public void LookYaw(float yaw)
    {
        lookRequested = true;
        wantYaw = yaw;
        wantPitch = 0f;
    }

    public void LookAtTarget()
    {
        if (Target != null)
            LookAt(Target.transform.position);
    }

    // 어떤 캐릭터가 서 있는 자리를 본다. 발밑이 아니라 가슴 높이를 겨눈다.
    public void LookAt(Vector3 characterPosition)
    {
        Vector3 toTarget = characterPosition + Vector3.up * ChestHeight - EyePosition;
        lookRequested = true;
        wantYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        // 아래를 볼수록 양수다. 플레이어 카메라의 pitch와 같은 방향.
        wantPitch = -Mathf.Atan2(toTarget.y, new Vector2(toTarget.x, toTarget.z).magnitude) * Mathf.Rad2Deg;
    }

    // 상체가 먼저 돌고, 하체는 멈춰 있을 땐 비틀림이 커질 때만, 걸을 땐 바로 따라온다.
    private void UpdateAim()
    {
        Vector3 velocity = Agent.velocity;
        velocity.y = 0f;
        bool moving = velocity.sqrMagnitude > 0.01f;

        if (!lookRequested)
        {
            wantYaw = moving ? Quaternion.LookRotation(velocity).eulerAngles.y : AimYaw;
            wantPitch = 0f;
        }

        // 목표 방향으로 한 번에 돌지 않고 초당 aimTurnSpeed 만큼만 돈다.
        float step = aimTurnSpeed * Time.deltaTime;
        AimYaw = Mathf.MoveTowardsAngle(AimYaw, wantYaw, step);
        aimPitch = Mathf.MoveTowards(aimPitch, wantPitch, step);

        aim.SetAim(AimYaw, aimPitch);
        aim.TurnBody(moving);
    }

    // ---------- 시야 ----------

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

        TargetVisible = best != null;
        if (TargetVisible)
            Target = best;
    }

    private bool CanSee(Health candidate, float distance)
    {
        if (distance > sightRange)
            return false;

        Vector3 eye = EyePosition;
        Vector3 chest = candidate.transform.position + Vector3.up * ChestHeight;

        // 상체가 보는 방향에서 좌우로 viewAngle의 절반 안에 있어야 보인다.
        // 높이는 따지지 않아서 위아래로는 원뿔이 아니라 부채꼴이다.
        Vector3 toTarget = chest - eye;
        toTarget.y = 0f;
        Vector3 facing = Quaternion.Euler(0f, AimYaw, 0f) * Vector3.forward;
        if (Vector3.Angle(facing, toTarget) > viewAngle * 0.5f)
            return false;

        // 선이 처음 닿은 게 그 상대 자신이면 가리는 게 없다는 뜻이다. 눈이 자기 몸
        // 안에서 출발하므로 자기 콜라이더에는 닿지 않는다.
        return Physics.Linecast(eye, chest, out RaycastHit hit, sightMask, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponentInParent<Health>() == candidate;
    }

    private void OnDrawGizmosSelected()
    {
        // 시야 부채꼴의 양쪽 끝을 선으로 그린다.
        Gizmos.color = Color.yellow;
        float yaw = Application.isPlaying ? AimYaw : transform.eulerAngles.y;
        Vector3 eyePos = EyePosition;
        Vector3 left = Quaternion.Euler(0f, yaw - viewAngle * 0.5f, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw + viewAngle * 0.5f, 0f) * Vector3.forward;
        Gizmos.DrawLine(eyePos, eyePos + left * sightRange);
        Gizmos.DrawLine(eyePos, eyePos + right * sightRange);
        Gizmos.DrawLine(eyePos + left * sightRange, eyePos + right * sightRange);

        // 돌아다니는 범위.
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? Home : (guardPoint != null ? guardPoint.position : transform.position);
        Gizmos.DrawWireSphere(center, idle.PatrolRadius);

        if (Application.isPlaying && Target != null)
        {
            Gizmos.color = TargetVisible ? Color.red : Color.gray;
            Gizmos.DrawLine(eyePos, Target.transform.position + Vector3.up * ChestHeight);
        }

        // 지금 상태 이름을 머리 위에 띄운다.
#if UNITY_EDITOR
        if (Application.isPlaying && current != null)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.3f, current.GetType().Name);
#endif
    }
}
