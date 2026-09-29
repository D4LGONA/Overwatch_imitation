using UnityEngine;
using UnityEngine.UI;

// 캐릭터 머리 위에 뜨는 체력바. 맞았을 때만 나타나고 잠시 뒤 사라진다.
public class WorldHealthBar : MonoBehaviour
{
    [Tooltip("비워두면 부모에서 찾는다.")]
    [SerializeField] private Health health;
    [Tooltip("Image Type이 Filled여야 한다.")]
    [SerializeField] private Image fill;
    [Tooltip("보이고 숨길 대상. 비워두면 이 오브젝트를 쓴다.")]
    [SerializeField] private GameObject visual;
    [SerializeField] private float hideDelay = 3f;

    private Camera viewCamera;
    private float hideTime;

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
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Changed -= OnChanged;
        }
    }

    private void Start()
    {
        viewCamera = Camera.main;
        visual.SetActive(false);
    }

    private void OnDamaged()
    {
        visual.SetActive(true);
        hideTime = Time.time + hideDelay;
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

        if (Time.time >= hideTime)
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
