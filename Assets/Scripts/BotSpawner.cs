using UnityEngine;
using UnityEngine.AI;

// 한 팀의 AI 캐릭터를 만든다. 씬에 하나씩 배치하면 스폰 지점 연결을 하나하나 해줘야 해서,
// 한 곳에서 만들고 같은 스폰 지점을 나눠 준다. 같은 프리팹으로 팀만 바꿔서 팀마다 하나씩 둔다.
// 플레이어가 어느 팀인지 정해진 뒤에 GameManager가 Spawn을 부른다. 플레이어 팀은 한 명 적게 만든다.
public class BotSpawner : MonoBehaviour
{
    [SerializeField] private GameObject botPrefab;
    [Tooltip("이 팀의 인원. 플레이어가 이 팀이면 봇은 한 명 적게 만든다.")]
    [SerializeField] private int teamSize = 5;
    [Tooltip("만든 캐릭터의 팀.")]
    [SerializeField] private Team team = Team.Defend;
    [Tooltip("처음 나타나고, 죽으면 되살아나는 곳. 여러 개면 돌아가며 쓴다.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("같은 스폰 지점에서 여러 명이 겹치지 않게 이 반경 안에 흩어 놓는다.")]
    [SerializeField] private float scatterRadius = 2f;

    public Team Team => team;
    public Transform[] SpawnPoints => spawnPoints;

    public void Spawn(Team playerTeam)
    {
        if (botPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[BotSpawner] 봇 프리팹이나 스폰 지점이 비어 있다.");
            return;
        }

        bool isPlayerTeam = playerTeam == team;
        int count = isPlayerTeam ? teamSize - 1 : teamSize;
        for (int i = 0; i < count; i++)
        {
            Transform spawn = spawnPoints[i % spawnPoints.Length];
            GameObject bot = Instantiate(botPrefab, ScatteredPosition(spawn.position), spawn.rotation);
            // 킬로그에 뜨는 이름이라 플레이어 기준으로 붙인다.
            bot.name = $"{(isPlayerTeam ? "Ally" : "Enemy")} {i + 1}";

            if (bot.TryGetComponent(out Health health))
                health.SetTeam(team);

            if (bot.TryGetComponent(out Respawner respawner))
                respawner.SetSpawnPoints(spawnPoints);
        }
    }

    // NavMesh 밖에 놓이면 NavMeshAgent가 움직이지 못해서 가장 가까운 NavMesh 위로 맞춘다.
    private Vector3 ScatteredPosition(Vector3 center)
    {
        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
        return NavMesh.SamplePosition(candidate, out NavMeshHit hit, scatterRadius, NavMesh.AllAreas) ? hit.position : center;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = team == Team.Attack ? Color.blue : Color.red;
        foreach (Transform spawn in spawnPoints)
            if (spawn != null)
                Gizmos.DrawWireSphere(spawn.position, scatterRadius);
    }
}
