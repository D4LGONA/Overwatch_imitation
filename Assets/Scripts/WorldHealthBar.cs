using UnityEngine;
using UnityEngine.UI;

// 캐릭터 머리 위에 뜨는 체력바. 적은 맞았을 때만 잠깐 빨갛게 뜨고,
// 아군은 원작처럼 늘 파랗게 떠 있다.
public class WorldHealthBar : MonoBehaviour
{
    [Tooltip("비워두면 부모에서 찾는다.")]
    [SerializeField] private Health health;
    [Tooltip("Image Type이 Filled여야 한다.")]
    [SerializeField] private Image fill;
    [Tooltip("보이고 숨길 대상. 비워두면 이 오브젝트를 쓴다.")]
    [SerializeField] private GameObject visual;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private Color allyColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f);

    private Camera viewCamera;
    private float hideTime;
    private bool isAlly;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();
        if (visual == null)
            visual = gameObject;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Changed += OnChanged;
            health.Died += OnDied;
            health.Revived += OnRevived;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Changed -= OnChanged;
            health.Died -= OnDied;
            health.Revived -= OnRevived;
        }
    }

    // 팀은 생성한 쪽이 Awake 뒤에 정해주므로 Start에서 본다.
    private void Start()
    {
        viewCamera = Camera.main;
        visual.SetActive(false);
        PlayerController.WhenReady(SetSide);
    }

    // 플레이어 기준으로 아군인지 정한다. 내 체력바는 HUD에 있으니 머리 위엔 띄우지 않는다.
    private void SetSide(PlayerController player)
    {
        if (this == null)
            return;

        isAlly = player.Health != health && !player.Health.IsHostileTo(health);

        if (fill != null)
            fill.color = isAlly ? allyColor : enemyColor;

        visual.SetActive(isAlly && health.IsAlive);
    }

    private void OnRevived()
    {
        if (isAlly)
            visual.SetActive(true);
    }

    private void OnDamaged()
    {
        // 죽는 한 방도 피격이라 여기로 온다. 죽었으면 띄우지 않는다.
        if (!health.IsAlive)
            return;

        visual.SetActive(true);
        hideTime = Time.time + hideDelay;
    }

    // Respawner는 메시만 숨기고 UI는 건드리지 않아서 여기서 직접 끈다.
    private void OnDied()
    {
        visual.SetActive(false);
    }

    private void OnChanged(float current, float max)
    {
        if (fill != null)
            fill.fillAmount = max > 0f ? current / max : 0f;
    }

    // 카메라가 움직인 뒤에 돌려야 한 프레임 늦게 따라오지 않는다.
    private void LateUpdate()
    {
        if (!visual.activeSelf)
            return;

        if (!isAlly && Time.time >= hideTime)
        {
            visual.SetActive(false);
            return;
        }

        // 돌리는 건 체력바(visual)이지 이 스크립트가 붙은 오브젝트가 아니다. 캐릭터 본체에
        // 붙여도 캐릭터까지 같이 돌아가면 안 된다.
        if (viewCamera != null)
            visual.transform.rotation = viewCamera.transform.rotation;
    }
}
