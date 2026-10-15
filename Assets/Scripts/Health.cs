using System;
using System.Collections.Generic;
using UnityEngine;

// 체력을 가진 모든 대상이 붙인다. 아군 오사를 막는 판정도 여기서 하므로
// 팀은 레이어가 아니라 이 값으로 정한다. 매치마다 팀이 바뀔 수 있어서다.
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 150f;
    [SerializeField] private Team team;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public Team Team => team;
    public bool IsAlive => Current > 0f;

    // 값이 바뀔 때만 알린다. UI가 매 프레임 들여다볼 필요가 없다.
    public event Action<float, float> Changed;

    // 맞은 순간에만 알린다. 초기화나 회복에는 불리지 않으므로 머리 위 체력바처럼
    // 피격에 반응해야 하는 쪽이 쓴다.
    public event Action Damaged;

    // 체력이 0이 된 순간과, 부활한 순간. 죽음에 반응할 쪽(조작 정지, AI 정지, 리스폰)이 쓴다.
    public event Action Died;
    public event Action Revived;

    // 누군가 죽은 순간 (죽은 쪽, 죽인 쪽). 킬로그처럼 특정 캐릭터가 아니라 전체를 지켜봐야 하는 쪽이 쓴다.
    // 캐릭터가 코드로 생성되기도 해서 하나하나 구독하는 대신 한 곳에서 알린다.
    public static event Action<Health, Health> AnyKilled;

    // 켜져 있는 모든 체력. AI가 상대를 찾을 때마다 씬 전체를 뒤지지 않도록 여기에 모아둔다.
    private static readonly List<Health> all = new List<Health>();
    public static IReadOnlyList<Health> All => all;

    private void Awake()
    {
        Current = maxHealth;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    // 같은 프리팹으로 아군과 적을 모두 만들기 때문에, 생성한 쪽이 팀을 정해준다.
    public void SetTeam(Team value)
    {
        team = value;
    }

    // 다른 팀이면 true. 맞힐 수 있는지, 노릴 대상인지 판단할 때 쓴다.
    public bool IsHostileTo(Health other) => other != null && other.team != team;

    private void Start()
    {
        // UI가 먼저 켜져 있도록 초기값도 한 번 알린다.
        Changed?.Invoke(Current, maxHealth);
    }

    // 되감기처럼 잠시 피해를 받지 않아야 하는 구간에서 켠다.
    public bool IsInvulnerable { get; set; }

    // 마지막으로 나를 때린 상대. 맞았을 때 돌아보는 AI처럼, Damaged를 받은 쪽이 읽는다.
    public Health LastAttacker { get; private set; }

    public void TakeDamage(float amount, Health attacker = null)
    {
        if (!IsAlive || IsInvulnerable)
            return;

        LastAttacker = attacker;
        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(Current, maxHealth);
        Damaged?.Invoke();

        if (!IsAlive)
        {
            Debug.Log($"[Health] {name} 사망");
            Died?.Invoke();
            AnyKilled?.Invoke(this, attacker);
        }
    }

    // 체력을 가득 채워 되살린다. 죽어 있을 때만 의미가 있다.
    public void Revive()
    {
        Current = maxHealth;
        IsInvulnerable = false;
        Changed?.Invoke(Current, maxHealth);
        Revived?.Invoke();
    }

    // 되감기처럼 과거 값을 그대로 되돌릴 때 쓴다. 피해나 회복이 아니라서
    // Damaged는 울리지 않는다.
    public void SetCurrent(float value)
    {
        Current = Mathf.Clamp(value, 0f, maxHealth);
        Changed?.Invoke(Current, maxHealth);
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
        Died = null;
        Revived = null;
    }
}
