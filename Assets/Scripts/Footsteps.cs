using UnityEngine;

// 땅 위에서 일정 거리를 걸을 때마다 발밑에서 발소리를 낸다. 플레이어와 봇이 같이 쓴다.
// 이동 방식(CharacterController, NavMeshAgent)과 상관없이 실제로 움직인 거리로 센다.
public class Footsteps : MonoBehaviour
{
    [Tooltip("이 거리를 걸을 때마다 한 걸음.")]
    [SerializeField] private float stepDistance = 1.5f;
    [Tooltip("한 프레임에 이보다 멀리 움직이면 걸은 게 아니라 순간이동(점멸, 부활)으로 본다.")]
    [SerializeField] private float teleportDistance = 2f;

    private CharacterController controller;
    private Health health;
    private Vector3 lastPosition;
    private float walked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        bool grounded = controller == null || controller.isGrounded;
        bool alive = health == null || health.IsAlive;
        if (!grounded || !alive || delta.magnitude > teleportDistance)
            return;

        walked += delta.magnitude;
        if (walked >= stepDistance)
        {
            walked -= stepDistance;
            SoundManager.Play(SoundId.Footstep, transform.position);
        }
    }
}
