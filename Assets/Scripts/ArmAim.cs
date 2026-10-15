using UnityEngine;
using UnityEngine.Serialization;

// 공격할 때 팔을 상체 정면으로 쭉 뻗는다. 상체 비틀기처럼 본을 코드로 직접 돌린다.
// 1인칭 플레이어는 대신 손을 카메라 앞 정해진 자리로 가져간다. 어깨는 눈보다 한참 아래라
// 그냥 앞으로 뻗으면 팔이 화면 아래 가장자리에만 걸쳐 보이기 때문이다.
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
    [Tooltip("켜면 왼팔도 같이 뻗는다. 쌍권총처럼 두 손에 드는 무기에 쓴다.")]
    [SerializeField] private bool bothArms;
    [Tooltip("켜면 손을 카메라 앞 Hand Offset 자리로 가져간다. 1인칭 플레이어에 켠다.")]
    [FormerlySerializedAs("followPitch")]
    [SerializeField] private bool firstPerson;
    [Tooltip("1인칭일 때 오른손 자리. 카메라 기준 (오른쪽, 위, 앞) 미터. 왼손은 좌우만 뒤집는다.")]
    [SerializeField] private Vector3 handOffset = new Vector3(0.2f, -0.18f, 0.45f);
    [Tooltip("1인칭에서 기준이 되는 카메라. 비워두면 자식에서 찾는다.")]
    [SerializeField] private Transform view;
    [Tooltip("켜면 처음부터 계속 뻗고 있는다. 무기를 늘 들고 있는 플레이어에 쓴다.")]
    [SerializeField] private bool aimOnStart;

    private Transform leftUpperArm;
    private Transform leftForeArm;
    private Transform leftHand;

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

        if (bothArms)
        {
            leftUpperArm = Find(all, "mixamorig:LeftArm");
            leftForeArm = Find(all, "mixamorig:LeftForeArm");
            leftHand = Find(all, "mixamorig:LeftHand");
        }

        if (firstPerson && view == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                view = cam.transform;
        }

        aiming = aimOnStart;
    }

    private void LateUpdate()
    {
        // 갑자기 홱 펴지지 않게 0과 1 사이를 천천히 오간다.
        float step = blendTime > 0f ? Time.deltaTime / blendTime : 1f;
        weight = Mathf.MoveTowards(weight, aiming ? 1f : 0f, step);

        if (weight <= 0f)
            return;

        if (firstPerson && view != null)
        {
            ReachArm(upperArm, foreArm, hand, ViewPoint(1f));
            if (bothArms)
                ReachArm(leftUpperArm, leftForeArm, leftHand, ViewPoint(-1f));
            return;
        }

        // 상체가 보는 정면으로 수평하게 뻗는다. 적은 상체가 이미 상대를 향해 돌아 있으니
        // 위아래 각도까지 따로 맞출 필요는 없다.
        Vector3 chestForward = Quaternion.Euler(0f, aim.AimYaw, 0f) * Vector3.forward;

        PointArm(upperArm, foreArm, hand, chestForward);
        if (bothArms)
            PointArm(leftUpperArm, leftForeArm, leftHand, chestForward);
    }

    // side가 1이면 오른손, -1이면 왼손 자리.
    private Vector3 ViewPoint(float side)
    {
        return view.position
               + view.right * (handOffset.x * side)
               + view.up * handOffset.y
               + view.forward * handOffset.z;
    }

    // 손이 target에 닿도록 팔꿈치 자리를 구해서 위팔과 아래팔을 돌린다(두 마디 IK).
    // 어깨-팔꿈치-손 세 점이 이루는 삼각형의 변 길이는 정해져 있으니, 코사인 법칙으로
    // 어깨에서 꺾이는 각도를 구한다. 팔꿈치는 아래 바깥쪽으로 굽힌다.
    private void ReachArm(Transform upper, Transform fore, Transform end, Vector3 target)
    {
        if (upper == null || fore == null || end == null)
            return;

        float upperLength = Vector3.Distance(upper.position, fore.position);
        float foreLength = Vector3.Distance(fore.position, end.position);

        Vector3 toTarget = target - upper.position;
        // 너무 멀면 쭉 편 채로, 너무 가까우면 접힌 채로 둔다. 딱 맞는 길이면 계산이 튄다.
        float distance = Mathf.Clamp(toTarget.magnitude, 0.01f, (upperLength + foreLength) * 0.999f);
        Vector3 dir = toTarget.normalized;

        float cosShoulder = (upperLength * upperLength + distance * distance - foreLength * foreLength)
                            / (2f * upperLength * distance);
        float shoulderAngle = Mathf.Acos(Mathf.Clamp(cosShoulder, -1f, 1f));

        // 팔꿈치가 꺾일 방향: 아래쪽과 몸 바깥쪽 사이. 손으로 가는 방향과 수직인 성분만 쓴다.
        Vector3 outward = (fore.position - transform.position);
        outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
        Vector3 hint = Vector3.ProjectOnPlane(-view.up + outward, dir).normalized;

        Vector3 elbow = upper.position
                        + dir * (Mathf.Cos(shoulderAngle) * upperLength)
                        + hint * (Mathf.Sin(shoulderAngle) * upperLength);

        PointAlong(upper, fore, elbow - upper.position);
        PointAlong(fore, end, target - fore.position);
    }

    // 위팔을 돌려 팔꿈치가 정면에 오게 하고, 이어서 아래팔을 돌려 손도 맞춘다.
    // 부모(위팔)를 먼저 돌려야 아래팔이 바뀐 위치에서 계산된다.
    private void PointArm(Transform upper, Transform fore, Transform end, Vector3 direction)
    {
        if (upper == null || fore == null || end == null)
            return;

        PointAlong(upper, fore, direction);
        PointAlong(fore, end, direction);
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
