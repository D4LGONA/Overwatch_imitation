using System;
using UnityEngine;

// 한 판의 시간과 승패를 맡는다. 거점은 점령만 하고, 그게 이기는 건지는 여기서 정한다.
// 화면에 시간을 그리는 건 MatchTimerUI가 한다(거점과 CapturePointUI처럼 규칙과 표시를 나눈다).
public class MatchManager : MonoBehaviour
{
    [Tooltip("판이 시작되기 전 준비 시간(초). 이 동안 공격팀은 스폰에 갇혀 있고 수비팀은 자리를 잡는다.")]
    [SerializeField] private float setupTime = 30f;
    [Tooltip("준비 시간 동안 공격팀 스폰을 막는 문. 준비 시간이 끝나면 꺼진다.")]
    [SerializeField] private GameObject[] spawnDoors;
    [Tooltip("공격팀이 처음 받는 시간(초).")]
    [SerializeField] private float matchTime = 240f;
    [Tooltip("마지막이 아닌 거점을 점령할 때마다 늘어나는 시간(초).")]
    [SerializeField] private float bonusTimeOnCapture = 60f;
    [Tooltip("점령해야 하는 순서대로 넣는다. 마지막 거점을 점령하면 공격팀 승리.")]
    [SerializeField] private CapturePoint[] points;
    [Tooltip("연장전 중 공격팀이 거점을 비우고도 버틸 수 있는 시간(초). 이 안에 다시 밟으면 계속된다.")]
    [SerializeField] private float overtimeGrace = 1f;

    private float remaining;
    private float setupLeft;

    // 남은 경기 시간과 준비 시간(초). 표시하는 쪽이 읽는다.
    public float Remaining => remaining;
    public float SetupRemaining => Mathf.Max(0f, setupLeft);
    private int targetIndex;
    private float graceLeft;

    public bool IsOver { get; private set; }

    // 준비 시간 중이면 true. 이 동안은 경기 시간이 흐르지 않고 거점도 점령할 수 없다.
    public bool IsSetup { get; private set; }

    // 시간은 다 됐지만 공격팀이 거점 위에 있어서 끝나지 않은 상태.
    public bool IsOvertime { get; private set; }

    // 공격팀이 지금 노리는 거점. 다 점령했으면 null.
    public CapturePoint CurrentPoint => targetIndex < points.Length ? points[targetIndex] : null;

    // 준비 시간이 끝나 문이 열린 순간.
    public event Action MatchStarted;

    // 공격팀이 이겼으면 true.
    public event Action<bool> MatchEnded;

    private void OnEnable()
    {
        foreach (CapturePoint point in points)
            if (point != null)
                point.Captured += OnPointCaptured;
    }

    private void OnDisable()
    {
        foreach (CapturePoint point in points)
            if (point != null)
                point.Captured -= OnPointCaptured;
    }

    private void Start()
    {
        remaining = matchTime;
        setupLeft = setupTime;
        IsSetup = setupTime > 0f;
        SetPointsEnabled(!IsSetup);
        SetDoorsClosed(IsSetup);
    }

    private void Update()
    {
        if (IsOver)
            return;

        if (IsSetup)
        {
            setupLeft -= Time.deltaTime;
            if (setupLeft <= 0f)
                BeginMatch();
            return;
        }

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        if (remaining <= 0f)
            UpdateOvertime();
    }

    // 시간이 다 됐을 때 공격팀이 거점 위에 있으면 끝내지 않는다. 다 비우면 잠깐 기다렸다가,
    // 그 안에 아무도 다시 밟지 않으면 수비팀 승리.
    private void UpdateOvertime()
    {
        CapturePoint point = CurrentPoint;
        bool onPoint = point != null && point.HasAttackers;

        if (!IsOvertime)
        {
            if (!onPoint)
            {
                End(false);
                return;
            }

            IsOvertime = true;
            graceLeft = overtimeGrace;
            Debug.Log("[Match] 연장전");
            SoundManager.Play(SoundId.Overtime);
        }

        if (onPoint)
        {
            graceLeft = overtimeGrace;
            return;
        }

        graceLeft -= Time.deltaTime;
        if (graceLeft <= 0f)
            End(false);
    }

    private void OnPointCaptured(CapturePoint point)
    {
        if (IsOver)
            return;

        targetIndex = Array.IndexOf(points, point) + 1;
        SoundManager.Play(SoundId.PointCaptured);

        if (targetIndex >= points.Length)
        {
            End(true);
        }
        else
        {
            // 연장전 중에 점령하면 시간이 생기니 연장전에서 빠져나온다.
            remaining += bonusTimeOnCapture;
            IsOvertime = false;
        }
    }

    private void BeginMatch()
    {
        IsSetup = false;
        SetPointsEnabled(true);
        SetDoorsClosed(false);
        Debug.Log("[Match] 준비 시간 끝, 공격 시작");
        SoundManager.Play(SoundId.DoorOpen);
        MatchStarted?.Invoke();
    }

    private void SetPointsEnabled(bool on)
    {
        foreach (CapturePoint point in points)
            if (point != null)
                point.enabled = on;
    }

    private void SetDoorsClosed(bool closed)
    {
        foreach (GameObject door in spawnDoors)
            if (door != null)
                door.SetActive(closed);
    }

    private void End(bool attackersWon)
    {
        IsOver = true;

        // 끝난 뒤에 거점을 계속 채울 수 없게 멈춘다.
        SetPointsEnabled(false);

        Debug.Log(attackersWon ? "[Match] 공격팀 승리" : "[Match] 수비팀 승리 (시간 종료)");
        MatchEnded?.Invoke(attackersWon);
    }

    private void OnDestroy()
    {
        MatchStarted = null;
        MatchEnded = null;
    }
}
