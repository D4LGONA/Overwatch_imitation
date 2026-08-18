using UnityEngine;
using UnityEngine.Rendering;

// 1인칭 시점 표현만 담당한다. 남의 캐릭터를 보는 데는 필요 없으므로
// 리모트 캐릭터에는 붙이지 않는다.
[RequireComponent(typeof(CharacterMotor))]
public class FirstPersonView : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;

    [Tooltip("내려다볼 때 자기 몸이 화면에 들어오는 걸 막는다. 3인칭으로 확인할 땐 꺼라.")]
    [SerializeField] private bool hideOwnBody = true;
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private SkinnedMeshRenderer[] bodyRenderers;

    private CharacterMotor motor;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
    }

    private void Start()
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0)
            bodyRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        // 렌더러를 끄지 않고 그림자 전용으로 돌린다. 몸은 사라지지만 바닥 그림자는 남는다.
        ShadowCastingMode mode = hideOwnBody ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        foreach (SkinnedMeshRenderer bodyRenderer in bodyRenderers)
            bodyRenderer.shadowCastingMode = mode;
    }

    // 모터가 몸을 돌린 뒤에 실행돼야 오프셋이 맞는다.
    private void LateUpdate()
    {
        cameraPivot.localRotation = Quaternion.Euler(motor.Pitch, motor.YawOffset, 0f);
    }
}
