using System;
using UnityEngine;

// 사거리 안의 상대에게 멈춰 서서 쏜다. 조준이 거의 다 돌아왔을 때만 쏘므로,
// 점멸로 옆에 붙으면 조준이 따라올 때까지는 맞지 않는다.
[Serializable]
public class EnemyAttackState : EnemyState
{
    [Tooltip("이 거리 안에 들어오면 멈춰서 쏜다.")]
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

    private float nextFireTime;

    public float Range => range;

    public override void Enter()
    {
        // 서서, 팔을 뻗고 쏜다.
        ai.Agent.ResetPath();
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

        if (ai.AimError <= aimTolerance && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 origin = ai.EyePosition;
        Vector3 direction = Spread(ai.AimDirection, spreadAngle);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, leaveRange * 2f, hitMask, QueryTriggerInteraction.Ignore))
            return;

        Debug.DrawLine(origin, hit.point, Color.magenta, 0.2f);

        if (GameManager.Instance != null && GameManager.Instance.HitEffects != null)
            GameManager.Instance.HitEffects.Get(hit.point, Quaternion.LookRotation(hit.normal));

        // 같은 팀은 맞지 않는다.
        if (hit.collider.TryGetComponent(out Health victim) && victim.Team != ai.Self.Team)
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
