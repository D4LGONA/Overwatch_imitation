using System;
using UnityEngine;

// 궁극기 게이지. 캐릭터마다 필요량만 다르고 채우는 규칙은 같아서 따로 둔다.
public class UltimateCharge : MonoBehaviour
{
    [Tooltip("가득 차는 데 필요한 양. 대미지 1이 1만큼 채운다.")]
    [SerializeField] private float required = 1500f;
    [Tooltip("아무것도 안 해도 초당 차는 양.")]
    [SerializeField] private float passivePerSecond = 4f;

    private float charge;

    public bool IsReady => charge >= required;
    public float Ratio => required > 0f ? Mathf.Clamp01(charge / required) : 1f;

    public event Action<float> Changed;

    private void Update()
    {
        // 가득 찼으면 더 쌓지 않는다. 넘겨 받아둔 만큼 다음 궁극기가 빨리 차면 안 된다.
        if (IsReady)
            return;

        Add(passivePerSecond * Time.deltaTime);
    }

    // 아군 오사나 자해로는 차면 안 되므로, 부르는 쪽에서 적에게 준 피해만 넘긴다.
    public void Add(float amount)
    {
        if (amount <= 0f || IsReady)
            return;

        charge = Mathf.Min(required, charge + amount);
        Changed?.Invoke(Ratio);
    }

    public void Consume()
    {
        charge = 0f;
        Changed?.Invoke(Ratio);
    }

    private void OnDestroy()
    {
        Changed = null;
    }
}
