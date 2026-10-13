using UnityEngine;

// 공격할 때 오른팔을 상체 정면으로 쭉 뻗는다. 상체 비틀기처럼 본을 코드로 직접 돌린다.
// 모델이 Generic이라 Unity 기본 IK를 쓸 수 없어서 이렇게 한다.
// 몸통이 다 비틀어진 뒤에 팔을 맞춰야 해서 UpperBodyAim보다 늦게 실행한다.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(UpperBodyAim))]
public class ArmAim : MonoBehaviour
{
    [Tooltip("비워두면 mixamorig:RightArm / RightForeArm / RightHand를 이름으로 찾는다.")]
    [SerializeField] private Transform upperArm;
    [SerializeField] private Transform foreArm;
    [SerializeField] private Transform hand;
    [Tooltip("팔을 뻗거나 내리는 데 걸리는 시간(초).")]
    [SerializeField] private float blendTime = 0.2f;

    private UpperBodyAim aim;
    private float weight;
    private bool aiming;

    public void SetAiming(bool on)
    {
        aiming = on;
    }

    private void Awake()
    {
        aim = GetComponent<UpperBodyAim>();

        Transform[] all = GetComponentsInChildren<Transform>(true);
        if (upperArm == null) upperArm = Find(all, "mixamorig:RightArm");
        if (foreArm == null) foreArm = Find(all, "mixamorig:RightForeArm");
        if (hand == null) hand = Find(all, "mixamorig:RightHand");
    }

    private void LateUpdate()
    {
        // 갑자기 홱 펴지지 않게 0과 1 사이를 천천히 오간다.
        float step = blendTime > 0f ? Time.deltaTime / blendTime : 1f;
        weight = Mathf.MoveTowards(weight, aiming ? 1f : 0f, step);

        if (weight <= 0f || upperArm == null || foreArm == null || hand == null)
            return;

        // 상체가 보는 정면으로 수평하게 뻗는다. 상체가 이미 상대를 향해 돌아 있으니
        // 위아래 각도까지 따로 맞출 필요는 없다.
        Vector3 chestForward = Quaternion.Euler(0f, aim.AimYaw, 0f) * Vector3.forward;

        // 위팔을 돌려 팔꿈치가 정면에 오게 하고, 이어서 아래팔을 돌려 손도 맞춘다.
        // 부모(위팔)를 먼저 돌려야 아래팔이 바뀐 위치에서 계산된다.
        PointAlong(upperArm, foreArm, chestForward);
        PointAlong(foreArm, hand, chestForward);
    }

    // bone에서 child로 가는 방향이 direction을 향하도록 bone을 돌린다.
    // 지금 방향과 목표 방향의 차이만큼만 돌리므로 매 프레임 불러도 쌓이지 않는다.
    private void PointAlong(Transform bone, Transform child, Vector3 direction)
    {
        Vector3 current = child.position - bone.position;
        Quaternion fullTurn = Quaternion.FromToRotation(current, direction);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, fullTurn, weight) * bone.rotation;
    }

    private Transform Find(Transform[] all, string boneName)
    {
        Transform found = System.Array.Find(all, t => t.name == boneName);
        if (found == null)
            Debug.LogWarning($"[ArmAim] {name}에서 {boneName}을 못 찾았다.");
        return found;
    }
}
