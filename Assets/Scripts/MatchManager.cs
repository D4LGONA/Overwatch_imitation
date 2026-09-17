using System;
using TMPro;
using UnityEngine;

// 한 판의 시간과 승패를 맡는다. 거점은 점령만 하고, 그게 이기는 건지는 여기서 정한다.
public class MatchManager : MonoBehaviour
{
    [Tooltip("공격팀이 처음 받는 시간(초).")]
    [SerializeField] private float matchTime = 240f;
    [Tooltip("마지막이 아닌 거점을 점령할 때마다 늘어나는 시간(초).")]
    [SerializeField] private float bonusTimeOnCapture = 60f;
    [Tooltip("점령해야 하는 순서대로 넣는다. 마지막 거점을 점령하면 공격팀 승리.")]
    [SerializeField] private CapturePoint[] points;
    [SerializeField] private TMP_Text timerText;

    private float remaining;
    private int targetIndex;

    public bool IsOver { get; private set; }

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
        RefreshTimer();
    }

    private void Update()
    {
        if (IsOver)
            return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        RefreshTimer();

        if (remaining <= 0f)
            End(false);
    }

    private void OnPointCaptured(CapturePoint point)
    {
        if (IsOver)
            return;

        targetIndex = Array.IndexOf(points, point) + 1;

        if (targetIndex >= points.Length)
            End(true);
        else
            remaining += bonusTimeOnCapture;

        RefreshTimer();
    }

    private void End(bool attackersWon)
    {
        IsOver = true;

        // 끝난 뒤에 거점을 계속 채울 수 없게 멈춘다.
        foreach (CapturePoint point in points)
            if (point != null)
                point.enabled = false;

        Debug.Log(attackersWon ? "[Match] 공격팀 승리" : "[Match] 수비팀 승리 (시간 종료)");
        MatchEnded?.Invoke(attackersWon);
    }

    private void RefreshTimer()
    {
        if (timerText == null)
            return;

        // 올림해야 0:00이 되는 순간이 실제로 끝나는 순간과 맞는다.
        int seconds = Mathf.CeilToInt(remaining);
        string time = $"{seconds / 60:00}:{seconds % 60:00}";

        if (IsOver || targetIndex >= points.Length)
            timerText.text = time;
        else
            timerText.text = $"{time} Capture Point {points[targetIndex].PointName}";
    }

    private void OnDestroy()
    {
        MatchEnded = null;
    }
}
