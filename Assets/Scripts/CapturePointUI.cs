using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 거점 하나의 상태를 화면에 보여준다. 캐릭터와 상관없는 경기 정보라 PlayerHUD와 따로 둔다.
public class CapturePointUI : MonoBehaviour
{
    [Tooltip("보여줄 거점. 거점도 UI도 씬에 있어서 드래그로 꽂으면 된다.")]
    [SerializeField] private CapturePoint point;

    [Tooltip("원 배경. 상태에 따라 색이 바뀐다.")]
    [SerializeField] private Image background;
    [Tooltip("진행도 링. Filled / Radial 360이어야 한다.")]
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text percent;

    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color openColor = Color.white;
    [SerializeField] private Color capturedColor = new Color(0.3f, 0.6f, 1f, 1f);

    private void OnEnable()
    {
        if (point == null)
            return;

        point.ProgressChanged += OnProgressChanged;
        point.Captured += OnStateChanged;
        point.Unlocked += OnStateChanged;
    }

    private void OnDisable()
    {
        if (point == null)
            return;

        point.ProgressChanged -= OnProgressChanged;
        point.Captured -= OnStateChanged;
        point.Unlocked -= OnStateChanged;
    }

    // 거점의 Awake가 끝난 뒤에 처음 상태를 그려야 잠김 여부가 맞게 나온다.
    private void Start()
    {
        if (label != null && point != null)
            label.text = point.PointName;

        Refresh();
    }

    private void OnProgressChanged(float progress) => Refresh();

    private void OnStateChanged(CapturePoint _) => Refresh();

    private void Refresh()
    {
        if (point == null)
            return;

        Color color = point.IsCaptured ? capturedColor : point.IsLocked ? lockedColor : openColor;

        if (background != null)
            background.color = color;

        if (fill != null)
        {
            fill.fillAmount = point.Progress;
            fill.color = color;
        }

        if (percent != null)
        {
            // 잠긴 거점에 0%를 띄우면 들어가서 채울 수 있는 것처럼 보여서 숨긴다.
            percent.gameObject.SetActive(!point.IsLocked);
            percent.text = $"{Mathf.FloorToInt(point.Progress * 100f)}%";
        }
    }
}
