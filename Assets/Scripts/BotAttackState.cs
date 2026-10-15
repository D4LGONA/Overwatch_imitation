using System;
using UnityEngine;

// 사거리 안의 상대를 쏜다. 거점을 밟는 게 먼저라, 거점 밖이면 거점으로 걸어가면서 쏘고
// 거점 위에 있으면 서서 쏜다. 조준이 거의 다 돌아왔을 때만 쏘므로,
// 점멸로 옆에 붙으면 조준이 따라올 때까지는 맞지 않는다.
[Serializable]
public class BotAttackState : BotState
{
    [Tooltip("이 거리 안에 들어오면 쏘기 시작한다.")]
    [SerializeField] private float range = 12f;
    [Tooltip("이 거리보다 멀어지면 다시 쫓아간다. range보다 크게 둬야 경계에서 멈췄다 걸었다를 반복하지 않는다.")]
    [SerializeField] private float leaveRange = 15f;

    [Header("Weapon")]
    [Tooltip("초당 발사 수.")]
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float damage = 6f;
    [Tooltip("탄퍼짐 반각. 사람처럼 일부러 빗나가게 한다.")]
    [SerializeField] private float spreadAngle = 3f;
    [Tooltip("조준이 목표에서 이 각도 안으로 들어와야 쏜다.")]
    [SerializeField] private float aimTolerance = 10f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Point")]
    [Tooltip("싸우는 동안에도 거점 중심에서 이 반경 밖이면 거점으로 걸어간다.")]
    [SerializeField] private float holdRadius = 2f;
    [Tooltip("길을 다시 찾는 간격(초).")]
    [SerializeField] private float repathInterval = 0.25f;

    private float nextFireTime;
    private float nextRepathTime;

    public float Range => range;

    public override void Enter()
    {
        // 팔을 뻗고 쏜다. 걷는 건 거점 안에 들어갈 때까지만.
        ai.Agent.stoppingDistance = 1f;
        nextRepathTime = 0f;
        ai.SetArmAiming(true);
    }

    public override void Exit()
    {
        ai.SetArmAiming(false);
    }

    public override void Tick()
    {
        Health target = ai.Target;
        if (!ai.TargetVisible || target == null || !target.IsAlive)
        {
            ai.ChangeState(ai.Idle);
            return;
        }

        if (Vector3.Distance(ai.transform.position, target.transform.position) > leaveRange)
        {
            ai.ChangeState(ai.Chase);
            return;
        }

        ai.LookAtTarget();
        HoldPoint();

        if (ai.AimError <= aimTolerance && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Fire();
        }
    }

    // 거점 밖이면 거점 중심으로 걸어가고, 안에 들어오면 멈춘다. 조준은 걷는 동안에도 상대를 본다.
    private void HoldPoint()
    {
        if (Time.time < nextRepathTime)
            return;
        nextRepathTime = Time.time + repathInterval;

        Vector3 offset = ai.transform.position - ai.Home;
        offset.y = 0f;
        if (offset.magnitude <= holdRadius)
            ai.Agent.ResetPath();
        else
            ai.Agent.SetDestination(ai.Home);
    }

    private void Fire()
    {
        Vector3 origin = ai.EyePosition;
        Vector3 direction = Spread(ai.AimDirection, spreadAngle);
        SoundManager.Play(SoundId.BotFire, origin);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, leaveRange * 2f, hitMask, QueryTriggerInteraction.Ignore))
            return;

        Debug.DrawLine(origin, hit.point, Color.magenta, 0.2f);

        if (GameManager.Instance != null && GameManager.Instance.HitEffects != null)
            GameManager.Instance.HitEffects.Get(hit.point, Quaternion.LookRotation(hit.normal));

        // 같은 팀은 맞지 않는다.
        if (hit.collider.TryGetComponent(out Health victim) && ai.Self.IsHostileTo(victim))
            victim.TakeDamage(damage, ai.Self);
    }

    // 트레이서 사격과 같은 원뿔 탄퍼짐. 1미터 앞에서 반지름 tan(각도) 원 안의 한 점을 겨눈다.
    private static Vector3 Spread(Vector3 forward, float angleDeg)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Tan(angleDeg * Mathf.Deg2Rad);
        Quaternion look = Quaternion.LookRotation(forward);
        return (forward + look * Vector3.right * offset.x + look * Vector3.up * offset.y).normalized;
    }
}
