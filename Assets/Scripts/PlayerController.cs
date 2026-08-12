using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1f;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Upper Body")]
    [Tooltip("골반에서 가슴 순서로. 에임 오프셋을 이 본들에 나눠 싣는다.")]
    [SerializeField] private Transform[] spineBones;
    [Tooltip("멈춰 있을 때 하체가 따라 돌기 시작하는 상체 비틀림 각도.")]
    [SerializeField] private float turnThreshold = 45f;
    [SerializeField] private float bodyTurnSpeed = 360f;
    [SerializeField] private float spinePitchWeight = 0.6f;

    [Header("First Person")]
    [Tooltip("내려다볼 때 자기 몸이 화면에 들어오는 걸 막는다. 3인칭으로 확인할 땐 꺼라.")]
    [SerializeField] private bool hideOwnBody = true;
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private SkinnedMeshRenderer[] bodyRenderers;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDamp = 0.1f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    private CharacterController controller;
    private float aimYaw;
    private float pitch;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        aimYaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (bodyRenderers == null || bodyRenderers.Length == 0)
            bodyRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        // 렌더러를 끄지 않고 그림자 전용으로 돌린다. 몸은 사라지지만 바닥 그림자는 남는다.
        ShadowCastingMode mode = hideOwnBody ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        foreach (SkinnedMeshRenderer bodyRenderer in bodyRenderers)
            bodyRenderer.shadowCastingMode = mode;
    }

    private void Update()
    {
        Look();
        Move();
    }

    // 애니메이터가 포즈를 덮어쓴 뒤에 실행돼야 상체 오프셋이 살아남는다.
    private void LateUpdate()
    {
        float yawOffset = YawOffset();
        cameraPivot.localRotation = Quaternion.Euler(pitch, yawOffset, 0f);

        // 부모 본을 돌리면 자식도 따라 돌기 때문에, 본마다 같은 몫을 더하면
        // 가슴에 도달했을 때 정확히 yawOffset 만큼 비틀린다.
        Vector3 aimRight = Quaternion.Euler(0f, aimYaw, 0f) * Vector3.right;
        float yawPerBone = yawOffset / spineBones.Length;
        float pitchPerBone = pitch * spinePitchWeight / spineBones.Length;

        foreach (Transform bone in spineBones)
        {
            bone.rotation = Quaternion.AngleAxis(yawPerBone, Vector3.up)
                          * Quaternion.AngleAxis(pitchPerBone, aimRight)
                          * bone.rotation;
        }
    }

    private void Look()
    {
        aimYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);
    }

    private void Move()
    {
        // GetAxis는 입력을 0에서 1까지 채워주기 때문에 가속과 감속이 붙는다.
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        TurnBody(input.sqrMagnitude > 0.01f);

        if (controller.isGrounded)
        {
            // 착지 상태에서 살짝 눌러줘야 CharacterController가 경사면에서 붕 뜨지 않는다.
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * Time.deltaTime;

        // 이동은 에임 기준, 걷기 애니메이션은 하체 기준이라 축을 따로 잡는다.
        Vector3 worldMove = Quaternion.Euler(0f, aimYaw, 0f) * new Vector3(input.x, 0f, input.y);
        Vector3 velocity = worldMove * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        Vector3 localMove = transform.InverseTransformDirection(worldMove);
        animator.SetFloat(MoveX, localMove.x, animationDamp, Time.deltaTime);
        animator.SetFloat(MoveY, localMove.z, animationDamp, Time.deltaTime);
    }

    // 이동 중이면 하체를 에임에 붙이고, 멈춰 있으면 비틀림이 임계값을 넘을 때만
    // 제자리에서 따라 돈다.
    private void TurnBody(bool moving)
    {
        float yawOffset = YawOffset();
        if (!moving && Mathf.Abs(yawOffset) <= turnThreshold)
            return;

        float target = moving ? aimYaw : aimYaw - Mathf.Sign(yawOffset) * turnThreshold;
        float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, target, bodyTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private float YawOffset()
    {
        return Mathf.DeltaAngle(transform.eulerAngles.y, aimYaw);
    }
}
