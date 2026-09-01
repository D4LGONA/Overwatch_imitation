using UnityEngine;

// 트레이서의 능력. 슬롯이 눌리면 여기서 해당 능력으로 갈라진다.
public class Tracer : MonoBehaviour
{
    [Tooltip("비워두면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private InputReader input;

    [Header("Pulse Pistols")]
    [Tooltip("레이가 나가는 기준. 비워두면 자식 카메라를 쓴다.")]
    [SerializeField] private Transform aimSource;
    [Tooltip("초당 발사 수.")]
    [SerializeField] private float fireRate = 40f;
    [Tooltip("탄퍼짐 반각. 이 각도의 원뿔 안에서 방향이 정해진다.")]
    [SerializeField] private float spreadAngle = 2.2f;
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Tooltip("한 발당 피해량.")]
    [SerializeField] private float damage = 6f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 40;
    [SerializeField] private float reloadTime = 1f;

    [Header("Blink")]
    [SerializeField] private float blinkDistance = 7f;
    [Tooltip("동시에 들고 있을 수 있는 충전 수.")]
    [SerializeField] private int blinkCharges = 3;
    [Tooltip("충전 하나가 다시 차는 데 걸리는 시간.")]
    [SerializeField] private float blinkRechargeTime = 3f;

    [Tooltip("비워두면 씬에서 찾는다. 이 무기의 탄퍼짐을 조준선에 알려준다.")]
    [SerializeField] private Crosshair crosshair;
    [Tooltip("비워두면 씬에서 찾는다.")]
    [SerializeField] private PlayerHUD hud;

    private Health health;
    private CharacterController controller;

    private float nextFireTime;
    private int ammo;
    private float reloadEndTime;
    private bool isReloading;

    private int blinkStock;
    private float nextChargeTime;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<InputReader>();
        if (aimSource == null)
            aimSource = GetComponentInChildren<Camera>().transform;

        // 체력은 UI를 모르게 두고 캐릭터가 중계한다. 적도 같은 Health를 쓰지만
        // 내 HUD에 뜨는 건 내 것뿐이어야 하기 때문이다.
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
    }

    // 프리팹은 씬 오브젝트를 참조할 수 없어서 인스펙터로 꽂아둘 수 없다.
    private void Start()
    {
        ammo = magazineSize;
        blinkStock = blinkCharges;

        // 프리팹은 씬 오브젝트를 참조할 수 없어서 실행할 때 찾는다.
        if (crosshair == null)
            crosshair = FindObjectOfType<Crosshair>();
        if (crosshair != null)
            crosshair.SetSpread(spreadAngle);

        if (hud == null)
            hud = FindObjectOfType<PlayerHUD>();

        PushAmmo();
        if (health != null)
            OnHealthChanged(health.Current, health.Max);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (hud != null)
            hud.SetHealth(current, max);
    }

    private void PushAmmo()
    {
        if (hud != null)
            hud.SetAmmo(ammo, magazineSize);
    }

    private void OnEnable() // 이벤트 구독
    {
        input.AbilityPressed += HandleAbility;
        if (health != null)
            health.Changed += OnHealthChanged;
    }

    private void OnDisable() // 이벤트 구독 해제
    {
        if (input != null)
            input.AbilityPressed -= HandleAbility;
        if (health != null)
            health.Changed -= OnHealthChanged;
    }

    private void Update()
    {
        RechargeBlink();

        if (isReloading)
        {
            if (Time.time < reloadEndTime)
                return;

            ammo = magazineSize;
            isReloading = false;
            // 재장전이 끝난 순간부터 다시 세야 밀린 발사가 몰리지 않는다.
            nextFireTime = Time.time;
            PushAmmo();
        }

        // 연사는 눌린 순간이 아니라 누르고 있는 동안이라 이벤트로는 처리할 수 없다.
        if (input.IsHeld(AbilitySlot.Primary))
        {
            // while로 도는 이유는 프레임보다 발사 간격이 짧을 수 있어서다. 60fps에서
            // 한 프레임은 16.7ms인데 초당 40발이면 25ms마다 쏴야 하므로, 프레임당
            // 한 발로 묶으면 설정값보다 느려진다. nextFireTime을 더해서 누적하면
            // 프레임이 밀려도 발사율이 유지된다.
            while (Time.time >= nextFireTime)
            {
                // 탄창이 비면 알아서 재장전에 들어간다.
                if (ammo <= 0)
                {
                    StartReload();
                    break;
                }

                FirePulsePistols();
                ammo--;
                PushAmmo();
                nextFireTime += 1f / fireRate;
            }
        }
        else
        {
            // 쉬는 동안 밀린 발사가 쌓였다가 한꺼번에 터지지 않도록 맞춰둔다.
            nextFireTime = Time.time;
        }
    }

    private void HandleAbility(AbilitySlot slot)
    {
        switch (slot)
        {
            // 점멸은 Shift와 우클릭 어느 쪽으로도 나간다.
            case AbilitySlot.Secondary:
            case AbilitySlot.Ability1:
                Blink();
                break;
            case AbilitySlot.Ability2:  Debug.Log("[Tracer] 스킬2"); break;
            case AbilitySlot.Ultimate:  Debug.Log("[Tracer] 궁극기"); break;
            case AbilitySlot.Punch:     Debug.Log("[Tracer] 근접공격"); break;
            case AbilitySlot.Reload:    StartReload(); break;
        }
    }

    // 충전은 하나씩 따로 차지 않고 순서대로 하나씩 채워진다. 가득 차 있으면
    // 타이머를 계속 미뤄서, 한 발 쓴 직후부터 3초를 세게 만든다.
    private void RechargeBlink()
    {
        if (blinkStock >= blinkCharges)
        {
            nextChargeTime = Time.time + blinkRechargeTime;
        }
        else if (Time.time >= nextChargeTime)
        {
            blinkStock++;
            nextChargeTime = Time.time + blinkRechargeTime;
        }

        PushBlink();
    }

    private void PushBlink()
    {
        if (hud == null)
            return;

        // 남은 시간을 진행도로 바꾼다. 가득 차 있으면 UI가 알아서 덮개를 걷는다.
        float remaining = Mathf.Max(0f, nextChargeTime - Time.time);
        float progress = 1f - remaining / blinkRechargeTime;
        hud.SetAbility(AbilitySlot.Ability1, blinkStock, blinkCharges, progress);
    }

    private void Blink()
    {
        if (blinkStock <= 0)
            return;

        // 오버워치 점멸은 보는 방향이 아니라 이동 입력 방향으로 나간다.
        // 아무 키도 안 누르고 있으면 정면이다.
        Vector2 move = input.Move;
        Vector3 local = move.sqrMagnitude > 0.01f
            ? new Vector3(move.x, 0f, move.y).normalized
            : Vector3.forward;

        // 이동과 같은 기준을 써야 시야 방향과 어긋나지 않는다.
        Vector3 direction = Quaternion.Euler(0f, aimSource.eulerAngles.y, 0f) * local;

        // Move를 쓰면 벽을 통과하지 않고 CharacterController가 알아서 막아준다.
        controller.Move(direction * blinkDistance);

        blinkStock--;
        PushBlink();
    }

    private void StartReload()
    {
        // 이미 가득 찼거나 재장전 중이면 다시 시작하지 않는다. 재장전 중에 R을
        // 연타해도 시간이 늘어나면 안 된다.
        if (isReloading || ammo == magazineSize)
            return;

        isReloading = true;
        reloadEndTime = Time.time + reloadTime;
        Debug.Log("[Tracer] 재장전 시작");
    }

    private void FirePulsePistols()
    {
        Vector3 origin = aimSource.position;
        Vector3 direction = SpreadDirection(aimSource.forward, spreadAngle); // 탄퍼짐

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
        {
            Debug.DrawLine(origin, hit.point, Color.red, 0.3f);
            MarkHit(hit.point);

            // 팀이 다를 때만 맞는다. 아군 관통은 대상이 생긴 뒤에 RaycastAll로 넣는다.
            if (hit.collider.TryGetComponent(out Health target)
                && (health == null || target.Team != health.Team))
            {
                target.TakeDamage(damage);
            }
        }
    }

    // 각도를 1미터 앞에서의 반지름으로 바꾼다. 방향을 흩뜨리므로 퍼짐이
    // 거리에 비례해 커지는 원뿔이 된다.
    private Vector3 SpreadDirection(Vector3 forward, float angleDeg)
    {
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(angleDeg * Mathf.Deg2Rad);
        return (forward + aimSource.right * offset.x + aimSource.up * offset.y).normalized;
    }

    // 체력이 생기기 전까지 맞은 자리를 눈으로 확인하는 임시 표시.
    private void MarkHit(Vector3 point)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = point;
        marker.transform.localScale = Vector3.one * 0.08f;
        // 콜라이더를 남기면 다음 탄이 이 표시에 맞는다.
        Destroy(marker.GetComponent<Collider>());
        Destroy(marker, 1f);
    }
}
