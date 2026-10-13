using UnityEngine;

// 조준 방향을 받아서 상체는 먼저, 하체는 뒤따라 돌린다. 조준을 누가 정하는지는 모른다 —
// 플레이어는 마우스로, 적은 타겟 쪽으로 넣어준다. 쓰는 곳이 둘이 되어 따로 뺐다.
public class UpperBodyAim : MonoBehaviour
{
    [Tooltip("골반에서 가슴 순서로. 비워두면 mixamorig:Spine, Spine1, Spine2를 이름으로 찾는다.")]
    [SerializeField] private Transform[] spineBones;
    [Tooltip("멈춰 있을 때 하체가 따라 돌기 시작하는 상체 비틀림 각도.")]
    [SerializeField] private float turnThreshold = 45f;
    [SerializeField] private float bodyTurnSpeed = 360f;
    [SerializeField] private float spinePitchWeight = 0.6f;

    private static readonly string[] DefaultSpineNames =
        { "mixamorig:Spine", "mixamorig:Spine1", "mixamorig:Spine2" };

    // 비틀기 전 포즈와, 비튼 뒤 포즈. 애니메이터가 없으면 아무도 포즈를 되돌려주지 않아서
    // 비틀기가 매 프레임 쌓인다. 지난번에 비튼 그대로 남아 있으면 먼저 되돌리는 데 쓴다.
    private Quaternion[] basePose;
    private Quaternion[] twistedPose;

    public float AimYaw { get; private set; }
    public float Pitch { get; private set; }

    // 몸이 조준을 따라오지 못한 각도. 카메라와 상체가 같이 쓴다.
    public float YawOffset => Mathf.DeltaAngle(transform.eulerAngles.y, AimYaw);

    private void Awake()
    {
        AimYaw = transform.eulerAngles.y;
        if (spineBones == null || spineBones.Length == 0)
            spineBones = FindDefaultSpine();

        basePose = new Quaternion[spineBones.Length];
        twistedPose = new Quaternion[spineBones.Length];
        for (int i = 0; i < spineBones.Length; i++)
            if (spineBones[i] != null)
                basePose[i] = twistedPose[i] = spineBones[i].localRotation;
    }

    public void SetAim(float yaw, float pitch)
    {
        AimYaw = yaw;
        Pitch = pitch;
    }

    // 이동 중이면 하체를 조준 방향에 붙이고, 멈춰 있으면 비틀림이 임계값을 넘을 때만
    // 제자리에서 따라 돈다.
    public void TurnBody(bool moving)
    {
        float yawOffset = YawOffset;
        if (!moving && Mathf.Abs(yawOffset) <= turnThreshold)
            return;

        float target = moving ? AimYaw : AimYaw - Mathf.Sign(yawOffset) * turnThreshold;
        float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, target, bodyTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // 애니메이터가 포즈를 덮어쓴 뒤에 실행돼야 상체 오프셋이 살아남는다.
    private void LateUpdate()
    {
        if (spineBones == null || spineBones.Length == 0)
            return;

        // 부모 본을 돌리면 자식도 따라 돌기 때문에, 본마다 같은 몫을 더하면
        // 가슴에 도달했을 때 정확히 YawOffset 만큼 비틀린다.
        Vector3 aimRight = Quaternion.Euler(0f, AimYaw, 0f) * Vector3.right;
        float yawPerBone = YawOffset / spineBones.Length;
        float pitchPerBone = Pitch * spinePitchWeight / spineBones.Length;

        // 1. 애니메이터가 이번 프레임에 포즈를 안 덮어썼으면(지난번 비튼 그대로면) 되돌린다.
        //    덮어썼으면 그게 새 기준 포즈다. 자식 본까지 다 되돌린 다음에 비틀어야 해서 따로 돈다.
        for (int i = 0; i < spineBones.Length; i++)
        {
            Transform bone = spineBones[i];
            if (bone == null)
                continue;

            if (bone.localRotation == twistedPose[i])
                bone.localRotation = basePose[i];
            basePose[i] = bone.localRotation;
        }

        // 2. 기준 포즈 위에 비틀기를 얹고, 얹은 결과를 기억해둔다.
        for (int i = 0; i < spineBones.Length; i++)
        {
            Transform bone = spineBones[i];
            if (bone == null)
                continue;

            bone.rotation = Quaternion.AngleAxis(yawPerBone, Vector3.up)
                          * Quaternion.AngleAxis(pitchPerBone, aimRight)
                          * bone.rotation;
            twistedPose[i] = bone.localRotation;
        }
    }

    private Transform[] FindDefaultSpine()
    {
        Transform[] found = new Transform[DefaultSpineNames.Length];
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < DefaultSpineNames.Length; i++)
        {
            found[i] = System.Array.Find(all, t => t.name == DefaultSpineNames[i]);
            if (found[i] == null)
            {
                Debug.LogWarning($"[UpperBodyAim] {name}에서 {DefaultSpineNames[i]}를 못 찾아 상체 비틀기를 끈다.");
                return new Transform[0];
            }
        }
        return found;
    }
}
