using UnityEngine;

// 트레이서의 능력. 슬롯이 눌리면 여기서 해당 능력으로 갈라진다.
public class Tracer : MonoBehaviour
{
    [Tooltip("비워두면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private InputReader input;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<InputReader>();
    }

    private void OnEnable()
    {
        input.AbilityPressed += HandleAbility;
    }

    private void OnDisable()
    {
        if (input != null)
            input.AbilityPressed -= HandleAbility;
    }

    private void HandleAbility(AbilitySlot slot)
    {
        switch (slot)
        {
            case AbilitySlot.Primary:   Debug.Log("[Tracer] 펄스 권총");   break;
            case AbilitySlot.Secondary: Debug.Log("[Tracer] 보조 공격");   break;
            case AbilitySlot.Ability1:  Debug.Log("[Tracer] 점멸");        break;
            case AbilitySlot.Ability2:  Debug.Log("[Tracer] 귀환");        break;
            case AbilitySlot.Ultimate:  Debug.Log("[Tracer] 펄스 폭탄");   break;
        }
    }
}
