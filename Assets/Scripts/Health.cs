using System;
using UnityEngine;

// 체력을 가진 모든 대상이 붙인다. 아군 오사를 막는 판정도 여기서 하므로
// 팀은 레이어가 아니라 이 값으로 정한다. 매치마다 팀이 바뀔 수 있어서다.
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 150f;
    [SerializeField] private int team;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public int Team => team;
    public bool IsAlive => Current > 0f;

    // 값이 바뀔 때만 알린다. UI가 매 프레임 들여다볼 필요가 없다.
    public event Action<float, float> Changed;

    // 맞은 순간에만 알린다. 초기화나 회복에는 불리지 않으므로 머리 위 체력바처럼
    // 피격에 반응해야 하는 쪽이 쓴다.
    public event Action Damaged;

    private void Awake()
    {
        Current = maxHealth;
    }

    private void Start()
    {
        // UI가 먼저 켜져 있도록 초기값도 한 번 알린다.
        Changed?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
            return;

        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(Current, maxHealth);
        Damaged?.Invoke();

        if (!IsAlive)
            Debug.Log($"[Health] {name} 사망");
    }

    public void Heal(float amount)
    {
        if (!IsAlive)
            return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    private void OnDestroy()
    {
        Changed = null;
        Damaged = null;
    }
}
