using System.Collections.Generic;
using UnityEngine;

// 던져서 처음 닿은 것에 붙고, 잠시 뒤 주변에 피해를 준다.
[RequireComponent(typeof(Rigidbody))]
public class PulseBomb : MonoBehaviour
{
    [SerializeField] private float fuseTime = 1f;
    [SerializeField] private float radius = 3f;
    [Tooltip("중심에서의 피해량. 가장자리로 갈수록 줄어든다.")]
    [SerializeField] private float maxDamage = 300f;
    [Tooltip("가장자리에서도 남는 피해 비율.")]
    [Range(0f, 1f)]
    [SerializeField] private float minDamageRatio = 0.2f;
    [SerializeField] private LayerMask damageMask = ~0;

    private Rigidbody body;
    private Health owner;
    private bool attached;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    // 궁극기 게이지는 넘겨받지 않는다. 궁극기 피해로 다시 궁극기가 차면 안 된다.
    public void Launch(Vector3 velocity, Health thrower)
    {
        owner = thrower;
        body.velocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (attached)
            return;

        attached = true;
        body.isKinematic = true;

        // 캐릭터에 붙으면 같이 움직여야 하므로 자식으로 들어간다.
        transform.SetParent(collision.transform, true);
        Invoke(nameof(Explode), fuseTime);
    }

    private void Explode()
    {
        // 한 대상이 콜라이더를 여러 개 가질 수 있어 중복 피해를 막는다.
        HashSet<Health> hit = new HashSet<Health>();

        foreach (Collider col in Physics.OverlapSphere(transform.position, radius, damageMask))
        {
            if (!col.TryGetComponent(out Health target) || !hit.Add(target))
                continue;

            // 아군은 피해를 입지 않지만 던진 본인은 휘말린다.
            bool isOwner = target == owner;
            if (!isOwner && owner != null && target.Team == owner.Team)
                continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            float falloff = Mathf.Lerp(1f, minDamageRatio, Mathf.Clamp01(distance / radius));
            target.TakeDamage(maxDamage * falloff);
        }

        Destroy(gameObject);
    }

    // 선택하지 않아도 보이게 해서, 날아가는 폭탄의 폭발 범위를 바로 확인한다.
    // Game 뷰에서 보려면 오른쪽 위 Gizmos 버튼을 켠다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
