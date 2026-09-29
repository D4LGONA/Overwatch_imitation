using System.Collections;
using UnityEngine;

// 죽으면 몸을 숨기고 행동을 멈췄다가, 잠시 뒤 자기 팀 스폰 지점에서 되살린다.
// 캐릭터를 새로 만들지 않고 같은 오브젝트를 옮겨 쓰므로, 카메라나 UI에 걸어둔 연결이 유지된다.
[RequireComponent(typeof(Health))]
public class Respawner : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 10f;
    [Tooltip("부활할 위치들. 여러 개면 그중 하나를 무작위로 고르고, 비워두면 죽은 자리에서 되살아난다.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("죽어 있는 동안 멈출 컴포넌트. 플레이어는 PlayerController와 Tracer, 적은 AI를 넣는다.")]
    [SerializeField] private Behaviour[] disableWhileDead;

    private Health health;
    private Renderer[] bodyRenderers;
    private Collider[] bodyColliders;

    private void Awake()
    {
        health = GetComponent<Health>();
        bodyRenderers = GetComponentsInChildren<Renderer>();
        // CharacterController도 Collider라서 캐릭터든 큐브든 같이 잡힌다.
        bodyColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable() => health.Died += OnDied;

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void OnDied()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetAlive(false);
        Debug.Log($"[Respawner] {name} {respawnDelay}초 뒤 부활");

        yield return new WaitForSeconds(respawnDelay);

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (spawn != null)
            {
                transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                Physics.SyncTransforms();
            }
        }

        // 컴포넌트를 먼저 다시 켜야 한다. Tracer 같은 쪽은 켜질 때 Revived를 구독하므로,
        // 순서가 바뀌면 부활 알림을 못 받아 탄약과 쿨다운이 초기화되지 않는다.
        SetAlive(true);
        health.Revive();
    }

    private void SetAlive(bool alive)
    {
        foreach (Behaviour behaviour in disableWhileDead)
            if (behaviour != null)
                behaviour.enabled = alive;

        foreach (Renderer body in bodyRenderers)
            if (body != null)
                body.enabled = alive;

        // 죽은 몸이 총알을 막거나 길을 막지 않게 한다. 거점은 죽은 캐릭터를 이미 걸러내고 있다.
        foreach (Collider body in bodyColliders)
            if (body != null)
                body.enabled = alive;
    }
}
