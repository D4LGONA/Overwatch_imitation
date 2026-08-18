using UnityEngine;

// 명령을 받아 캐릭터를 실제로 움직인다. 입력을 직접 읽지 않기 때문에
// 로컬 플레이어와 리모트 플레이어가 이 컴포넌트를 그대로 공유한다.
[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1f;

    [Header("Upper Body")]
    [Tooltip("골반에서 가슴 순서로. 에임 오프셋을 이 본들에 나눠 싣는다.")]
    [SerializeField] private Transform[] spineBones;
    [Tooltip("멈춰 있을 때 하체가 따라 돌기 시작하는 상체 비틀림 각도.")]
    [SerializeField] private float turnThreshold = 45f;
    [SerializeField] private float bodyTurnSpeed = 360f;
    [SerializeField] private float spinePitchWeight = 0.6f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDamp = 0.1f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

    private CharacterController controller;
    private CharacterCommand command;
    private float verticalVelocity;

    public float Pitch => command.Pitch;

    // 몸이 에임을 따라오지 못한 각도. 카메라와 상체가 같이 쓴다.
    public float YawOffset => Mathf.DeltaAngle(transform.eulerAngles.y, command.AimYaw);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        command.AimYaw = transform.eulerAngles.y;
    }

    // 입력 제공자가 자기 Update에서 직접 부른다. 컴포넌트 간 Update 실행
    // 순서는 보장되지 않으므로 모터는 자체 Update를 두지 않는다.
    public void Tick(CharacterCommand cmd)
    {
        command = cmd;

        TurnBody();
        Vector3 worldMove = ApplyMovement();
        ApplyAnimation(worldMove);
    }

    // 애니메이터가 포즈를 덮어쓴 뒤에 실행돼야 상체 오프셋이 살아남는다.
    private void LateUpdate()
    {
        if (spineBones == null || spineBones.Length == 0)
            return;

        // 부모 본을 돌리면 자식도 따라 돌기 때문에, 본마다 같은 몫을 더하면
        // 가슴에 도달했을 때 정확히 YawOffset 만큼 비틀린다.
        Vector3 aimRight = Quaternion.Euler(0f, command.AimYaw, 0f) * Vector3.right;
        float yawPerBone = YawOffset / spineBones.Length;
        float pitchPerBone = command.Pitch * spinePitchWeight / spineBones.Length;

        foreach (Transform bone in spineBones)
        {
            bone.rotation = Quaternion.AngleAxis(yawPerBone, Vector3.up)
                          * Quaternion.AngleAxis(pitchPerBone, aimRight)
                          * bone.rotation;
        }
    }

    // 이동 중이면 하체를 에임에 붙이고, 멈춰 있으면 비틀림이 임계값을 넘을 때만
    // 제자리에서 따라 돈다.
    private void TurnBody()
    {
        bool moving = command.Move.sqrMagnitude > 0.01f;
        float yawOffset = YawOffset;
        if (!moving && Mathf.Abs(yawOffset) <= turnThreshold)
            return;

        float target = moving ? command.AimYaw : command.AimYaw - Mathf.Sign(yawOffset) * turnThreshold;
        float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, target, bodyTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private Vector3 ApplyMovement()
    {
        if (controller.isGrounded)
        {
            // 착지 상태에서 살짝 눌러줘야 CharacterController가 경사면에서 붕 뜨지 않는다.
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (command.Jump)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 worldMove = Quaternion.Euler(0f, command.AimYaw, 0f)
                          * new Vector3(command.Move.x, 0f, command.Move.y);

        Vector3 velocity = worldMove * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        return worldMove;
    }

    // 이동은 에임 기준이지만 걷기 애니메이션은 하체 기준이라 축을 되돌린다.
    private void ApplyAnimation(Vector3 worldMove)
    {
        Vector3 localMove = transform.InverseTransformDirection(worldMove);
        animator.SetFloat(MoveX, localMove.x, animationDamp, Time.deltaTime);
        animator.SetFloat(MoveY, localMove.z, animationDamp, Time.deltaTime);

        // 공중 상태 전이는 트리거가 아니라 이 두 값으로 굴린다. 트리거는 전이
        // 타이밍이 어긋나면 신호가 씹혀서 공중에 낀 채로 남는다. 상승이면
        // 도약부터, 하강이면 곧장 체공으로 들어가므로 낙하도 같은 길로 처리된다.
        animator.SetBool(IsGrounded, controller.isGrounded);
        animator.SetFloat(VerticalVelocity, verticalVelocity);
    }
}
