using System;
using UnityEngine;

// 능력 슬롯. 영웅이 바뀌어도 이 구성은 그대로고, 슬롯에 무엇이 꽂히는지만 달라진다.
public enum AbilitySlot
{
    Primary,
    Secondary,
    Ability1,
    Ability2,
    Ultimate,
    Punch,
    Reload,
}

// 키를 읽어 논리적인 입력으로 바꾼다. 키 설정이 이 파일에만 있으므로
// 리바인딩할 때 다른 코드를 건드릴 필요가 없다.
public class InputReader : MonoBehaviour
{
    [Header("Key Bindings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode primaryKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode secondaryKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode ability1Key = KeyCode.LeftShift;
    [SerializeField] private KeyCode ability2Key = KeyCode.E;
    [SerializeField] private KeyCode ultimateKey = KeyCode.Q;
    [SerializeField] private KeyCode punchKey = KeyCode.V;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    // 상태는 프로퍼티로 노출한다. "지금 얼마나 기울어져 있나"라서 매 프레임 읽어야 한다.
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    // 사건은 이벤트로 알린다. "방금 눌렸다"는 한 순간이라 놓치면 안 된다.
    public event Action JumpPressed;
    public event Action<AbilitySlot> AbilityPressed;

    private void Update()
    {
        // GetAxis는 입력을 0에서 1까지 채워주기 때문에 가속과 감속이 붙는다.
        Move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (true == Input.GetKeyDown(jumpKey))
            JumpPressed?.Invoke();

        // 지금은 전부 누른 순간만 본다. 연사가 필요해지면 주 공격만 GetKey로 바꾸면 된다.
        Fire(primaryKey, AbilitySlot.Primary);
        Fire(secondaryKey, AbilitySlot.Secondary);
        Fire(ability1Key, AbilitySlot.Ability1);
        Fire(ability2Key, AbilitySlot.Ability2);
        Fire(ultimateKey, AbilitySlot.Ultimate);
        Fire(punchKey, AbilitySlot.Punch);
        Fire(reloadKey, AbilitySlot.Reload);
    }

    private void Fire(KeyCode key, AbilitySlot slot)
    {
        if (true == Input.GetKeyDown(key))
            AbilityPressed?.Invoke(slot);
    }

    // 누르고 있는 동안 계속 나가는 능력이 쓴다. 트레이서 펄스 권총처럼
    // 연사하는 무기는 눌린 순간만으로는 부족하다.
    public bool IsHeld(AbilitySlot slot)
    {
        return Input.GetKey(KeyOf(slot));
    }

    private KeyCode KeyOf(AbilitySlot slot)
    {
        switch (slot)
        {
            case AbilitySlot.Primary:   return primaryKey;
            case AbilitySlot.Secondary: return secondaryKey;
            case AbilitySlot.Ability1:  return ability1Key;
            case AbilitySlot.Ability2:  return ability2Key;
            case AbilitySlot.Ultimate:  return ultimateKey;
            case AbilitySlot.Punch:     return punchKey;
            case AbilitySlot.Reload:    return reloadKey;
            default:                    return KeyCode.None;
        }
    }

    // 구독자가 해지를 빠뜨린 채 이 오브젝트가 사라져도 참조가 남지 않도록 비운다.
    private void OnDestroy()
    {
        JumpPressed = null;
        AbilityPressed = null;
    }
}
