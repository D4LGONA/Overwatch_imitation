using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MatchManager match;
    [SerializeField] private ObjectPool hitEffects;
    [Tooltip("팀마다 하나씩. 플레이어 팀이 정해지면 봇을 만들고, 플레이어도 자기 팀 스폰으로 보낸다.")]
    [SerializeField] private BotSpawner[] spawners;

    public MatchManager Match => match;
    public ObjectPool HitEffects => hitEffects;

    // 플레이어가 고른 팀. 승패 판정과 화면 문구가 이걸 기준으로 한다.
    public Team PlayerTeam { get; private set; } = Team.Attack;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 영웅 선택에서 팀을 고르면 부른다. 플레이어를 그 팀 스폰으로 보내고, 양 팀 봇을 만든다.
    // 봇은 플레이어 팀이 정해진 뒤에 만들어야 인원과 이름(아군/적)을 맞출 수 있다.
    public void BeginAs(Team side)
    {
        PlayerTeam = side;

        PlayerController player = PlayerController.Local;
        BotSpawner home = System.Array.Find(spawners, s => s != null && s.Team == side);
        if (player != null)
        {
            player.Health.SetTeam(side);
            if (home != null && home.SpawnPoints.Length > 0)
            {
                Transform spawn = home.SpawnPoints[0];
                player.Teleport(spawn.position, spawn.rotation);
                if (player.TryGetComponent(out Respawner respawner))
                    respawner.SetSpawnPoints(home.SpawnPoints);
            }
        }

        foreach (BotSpawner spawner in spawners)
            if (spawner != null)
                spawner.Spawn(side);
    }

    // 영웅 선택, 결과 화면처럼 게임을 세워두고 버튼을 눌러야 하는 화면이 쓴다.
    // 시간을 멈추면 이동·애니메이션·타이머가 서지만, 마우스 회전이나 점멸처럼 시간에 곱해지지
    // 않는 동작은 계속되므로 입력도 끊는다. 버튼을 누를 수 있게 커서도 푼다.
    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        foreach (InputReader input in FindObjectsOfType<InputReader>())
            input.enabled = !paused;

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }
}