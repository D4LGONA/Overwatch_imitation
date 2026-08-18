using UnityEngine;

// 마우스와 키보드를 읽어 모터에 넘긴다. 내가 조작하는 캐릭터에만 붙는다.
// 리모트 캐릭터는 이 자리에 네트워크 수신값을 넣는 컴포넌트가 들어간다.
[RequireComponent(typeof(CharacterMotor))]
public class LocalInputProvider : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterMotor motor;
    private float aimYaw;
    private float pitch;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        aimYaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        aimYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);

        // GetAxis는 입력을 0에서 1까지 채워주기 때문에 가속과 감속이 붙는다.
        Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        // 모터를 직접 부른다. Update 실행 순서에 기대지 않기 위해서다.
        motor.Tick(new CharacterCommand
        {
            Move = move,
            AimYaw = aimYaw,
            Pitch = pitch,
            Jump = Input.GetButtonDown("Jump"),
        });
    }
}
