using TMPro;
using UnityEngine;

// 화면 위 가운데 타이머. MatchManager의 상태를 읽어 글자로 그린다.
// 규칙(MatchManager)과 표시를 나눠서, 문구나 꾸밈을 바꿔도 경기 규칙 코드는 건드리지 않는다.
public class MatchTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Color overtimeColor = new Color(1f, 0.61f, 0.1f);

    private MatchManager match;

    // 매 프레임 같은 글자를 새로 만들지 않도록, 화면에 보이는 값이 바뀔 때만 다시 쓴다.
    private int lastSeconds = -1;
    private bool lastSetup, lastOvertime, lastOver;
    private CapturePoint lastPoint;

    private void Start()
    {
        // 싱글톤은 다른 스크립트의 Awake 순서와 엉키지 않도록 Start에서 꺼낸다.
        match = GameManager.Instance.Match;
    }

    private void Update()
    {
        if (match == null)
            return;

        CapturePoint point = match.CurrentPoint;

        // 올림해야 0:00이 되는 순간이 실제로 끝나는 순간과 맞는다.
        int seconds = Mathf.CeilToInt(match.IsSetup ? match.SetupRemaining : match.Remaining);
        if (seconds == lastSeconds && point == lastPoint && match.IsSetup == lastSetup
            && match.IsOvertime == lastOvertime && match.IsOver == lastOver)
            return;

        lastSeconds = seconds;
        lastPoint = point;
        lastSetup = match.IsSetup;
        lastOvertime = match.IsOvertime;
        lastOver = match.IsOver;

        string pointName = point != null ? point.PointName : "";
        string time = $"{seconds / 60:00}:{seconds % 60:00}";
        // 원작처럼 내 팀 입장에서 쓴다. 수비면 "지켜라".
        string goal = GameManager.Instance.PlayerTeam == Team.Defend ? "DEFEND" : "CAPTURE";

        // 원작처럼 목표는 작게, 남은 시간은 크게.
        if (match.IsSetup)
            text.text = $"<size=55%>SETUP</size>\n{time}";
        else if (match.IsOvertime && !match.IsOver)
            text.text = $"<size=55%><color=#{ColorUtility.ToHtmlStringRGB(overtimeColor)}>OVERTIME</color></size>\n{goal} {pointName}";
        else if (match.IsOver || point == null)
            text.text = time;
        else
            text.text = $"<size=55%>{goal} POINT {pointName}</size>\n{time}";
    }
}
