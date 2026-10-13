using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1f;

    [Header("Input")]
    [Tooltip("비워두면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private InputReader input;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

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
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

    private CharacterController controller;
    private UpperBodyAim aim;
    private float aimYaw;
    private float pitch;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (input == null)
            input = GetComponent<InputReader>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 프리팹에 안 붙어 있어도 돌아가게 없으면 만든다. 척추 본은 이름으로 알아서 찾는다.
        aim = GetComponent<UpperBodyAim>();
        if (aim == null)
            aim = gameObject.AddComponent<UpperBodyAim>();

        aimYaw = transform.eulerAngles.y;
    }

    // 구독과 해지는 항상 짝으로 둔다. 종료 순서에 따라 input이 먼저 파괴될 수 있어
    // 해지 쪽은 null을 확인한다.
    private void OnEnable()
    {
        input.JumpPressed += OnJumpPressed;
    }

    private void OnDisable()
    {
        if (input != null)
            input.JumpPressed -= OnJumpPressed;
    }

    private void OnJumpPressed()
    {
        if (controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
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

    // 몸이 다 돈 뒤에 카메라를 맞춰야 한 프레임 어긋나지 않는다. 상체 비틀기는 UpperBodyAim이 한다.
    private void LateUpdate()
    {
        cameraPivot.localRotation = Quaternion.Euler(pitch, aim.YawOffset, 0f);
    }

    private void Look()
    {
        aimYaw += input.Look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, minPitch, maxPitch);
        aim.SetAim(aimYaw, pitch);
    }

    private void Move()
    {
        Vector2 move = input.Move;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        aim.TurnBody(move.sqrMagnitude > 0.01f);

        // 착지 상태에서 살짝 눌러줘야 CharacterController가 경사면에서 붕 뜨지 않는다.
        // 점프로 이미 위로 튄 프레임을 덮지 않도록 하강 중일 때만 누른다.
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        // 이동은 에임 기준, 걷기 애니메이션은 하체 기준이라 축을 따로 잡는다.
        Vector3 worldMove = Quaternion.Euler(0f, aimYaw, 0f) * new Vector3(move.x, 0f, move.y);
        Vector3 velocity = worldMove * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

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
