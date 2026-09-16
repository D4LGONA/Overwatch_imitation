using System;
using System.Collections.Generic;
using UnityEngine;

// 공격팀이 영역 안에 있으면 점령 게이지가 찬다. 영역은 이 오브젝트의 트리거 콜라이더다.
[RequireComponent(typeof(Collider))]
public class CapturePoint : MonoBehaviour
{
    [SerializeField] private string pointName = "A";
    [Tooltip("점령할 수 있는 팀.")]
    [SerializeField] private int attackingTeam;
    [Tooltip("처음부터 끝까지 채우는 데 걸리는 시간.")]
    [SerializeField] private float captureTime = 10f;
    [Tooltip("진행도를 이 개수로 나눈 지점에 닿으면 그 아래로는 떨어지지 않는다. 1이면 체크포인트 없음.")]
    [SerializeField] private int checkpoints = 3;
    [Tooltip("아무도 없을 때 초당 떨어지는 진행도.")]
    [SerializeField] private float decayPerSecond = 0.05f;
    [Tooltip("B처럼 앞 거점을 먼저 점령해야 열리는 곳은 켠다.")]
    [SerializeField] private bool startsLocked;
    [Tooltip("이 거점을 점령하면 열리는 다음 거점.")]
    [SerializeField] private CapturePoint next;

    private readonly HashSet<Health> attackersInside = new HashSet<Health>();
    private float floor;

    public string PointName => pointName;
    public float Progress { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsCaptured { get; private set; }

    public event Action<float> ProgressChanged;
    public event Action<CapturePoint> Captured;
    public event Action<CapturePoint> Unlocked;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        IsLocked = startsLocked;
    }

    public void Unlock()
    {
        if (!IsLocked)
            return;

        IsLocked = false;
        Debug.Log($"[CapturePoint] {pointName} 열림");
        Unlocked?.Invoke(this);
    }

    // 캐릭터 본체의 콜라이더만 센다. 캐릭터에 붙은 폭탄 같은 자식 콜라이더는
    // Health를 직접 갖고 있지 않아서 걸러진다.
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Health health) && health.Team == attackingTeam)
            attackersInside.Add(health);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Health health))
            attackersInside.Remove(health);
    }

    private void Update()
    {
        if (IsLocked || IsCaptured)
            return;

        // 안에서 죽거나 사라지면 나가기 이벤트가 오지 않아서 직접 치운다.
        attackersInside.RemoveWhere(h => h == null || !h.IsAlive);

        float before = Progress;

        if (attackersInside.Count > 0)
            Progress = Mathf.Min(1f, Progress + Time.deltaTime / captureTime);
        else
            Progress = Mathf.Max(floor, Progress - decayPerSecond * Time.deltaTime);

        UpdateCheckpoint();

        if (!Mathf.Approximately(before, Progress))
            ProgressChanged?.Invoke(Progress);

        if (Progress >= 1f)
            Capture();
    }

    private void UpdateCheckpoint()
    {
        if (checkpoints <= 1)
            return;

        float step = 1f / checkpoints;
        float reached = Mathf.Floor(Progress / step) * step;
        if (reached > floor && reached < 1f)
        {
            floor = reached;
            Debug.Log($"[CapturePoint] {pointName} 체크포인트 {Mathf.RoundToInt(floor * 100f)}%");
        }
    }

    private void Capture()
    {
        IsCaptured = true;
        attackersInside.Clear();
        Debug.Log($"[CapturePoint] {pointName} 점령 완료");

        Captured?.Invoke(this);

        if (next != null)
            next.Unlock();
    }

    // 트리거는 게임 화면에 안 보이므로 상태별 색으로 영역을 그린다.
    private void OnDrawGizmos()
    {
        Collider area = GetComponent<Collider>();
        if (area == null)
            return;

        if (Application.isPlaying)
            Gizmos.color = IsCaptured ? Color.green : IsLocked ? Color.gray : Color.yellow;
        else
            Gizmos.color = startsLocked ? Color.gray : Color.yellow;

        Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
    }

    private void OnDestroy()
    {
        ProgressChanged = null;
        Captured = null;
        Unlocked = null;
    }
}
