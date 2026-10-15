using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 내가 맞으면 화면 가운데 둘레에 때린 쪽 방향으로 빨간 표시를 띄우고, 화면 가장자리를 잠깐 붉게 한다.
// 표시는 맞은 순간의 방향에 고정하지 않고 매 프레임 다시 계산해서, 돌아보면 따라 돈다.
public class DamageIndicator : MonoBehaviour
{
    [Tooltip("표시 하나의 원본. 화면 가운데에 두고, 위쪽(+Y)에 빨간 막대를 자식으로 둔다. 처음엔 꺼둔다.")]
    [SerializeField] private RectTransform template;
    [Tooltip("맞았을 때 잠깐 붉어지는 전체 화면 이미지. 비워둬도 된다.")]
    [SerializeField] private Image flash;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float flashAlpha = 0.25f;
    [SerializeField] private float flashDuration = 0.3f;

    private class Marker
    {
        public RectTransform rect;
        public Image image;
        public Health source;
        public Vector3 lastPosition;
        public float endTime;
    }

    private readonly List<Marker> markers = new List<Marker>();
    private Health health;
    private Transform view;
    private float flashEndTime;

    private void Start()
    {
        template.gameObject.SetActive(false);
        if (flash != null)
            flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 0f);

        PlayerController.WhenReady(Bind);
    }

    private void Bind(PlayerController player)
    {
        if (this == null)
            return;

        health = player.Health;
        view = Camera.main != null ? Camera.main.transform : player.transform;
        health.Damaged += OnDamaged;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Damaged -= OnDamaged;
    }

    private void OnDamaged()
    {
        flashEndTime = Time.time + flashDuration;
        SoundManager.Play(SoundId.PlayerHurt);

        Health source = health.LastAttacker;
        if (source == null || source == health)
            return;

        // 같은 상대에게 또 맞으면 새로 만들지 않고 시간만 늘린다.
        Marker marker = markers.Find(m => m.source == source && m.rect.gameObject.activeSelf)
                        ?? markers.Find(m => !m.rect.gameObject.activeSelf)
                        ?? CreateMarker();

        marker.source = source;
        marker.lastPosition = source.transform.position;
        marker.endTime = Time.time + duration;
        marker.rect.gameObject.SetActive(true);
    }

    private Marker CreateMarker()
    {
        RectTransform rect = Instantiate(template, template.parent);
        Marker marker = new Marker { rect = rect, image = rect.GetComponentInChildren<Image>(true) };
        markers.Add(marker);
        return marker;
    }

    private void Update()
    {
        foreach (Marker marker in markers)
        {
            if (!marker.rect.gameObject.activeSelf)
                continue;

            float left = marker.endTime - Time.time;
            if (left <= 0f)
            {
                marker.rect.gameObject.SetActive(false);
                continue;
            }

            // 상대가 죽어 사라져도 마지막 위치를 계속 가리킨다.
            if (marker.source != null && marker.source.IsAlive)
                marker.lastPosition = marker.source.transform.position;

            // 위에서 내려다본 각도. 오른쪽이 양수인데 UI는 반시계가 양수라 뒤집는다.
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            Vector3 toSource = Vector3.ProjectOnPlane(marker.lastPosition - view.position, Vector3.up);
            float angle = Vector3.SignedAngle(forward, toSource, Vector3.up);
            marker.rect.localRotation = Quaternion.Euler(0f, 0f, -angle);

            // 마지막 절반 동안 흐려진다.
            if (marker.image != null)
            {
                Color c = marker.image.color;
                c.a = Mathf.Clamp01(left / (duration * 0.5f));
                marker.image.color = c;
            }
        }

        if (flash != null)
        {
            float t = Mathf.Clamp01((flashEndTime - Time.time) / flashDuration);
            Color c = flash.color;
            c.a = t * flashAlpha;
            flash.color = c;
        }
    }
}
