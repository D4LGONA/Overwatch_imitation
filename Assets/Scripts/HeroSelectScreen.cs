using UnityEngine;
using UnityEngine.UI;

// 판이 시작되기 전에 팀(공격/수비)과 영웅을 고르는 화면. 오버워치처럼 게임 씬 안에서 띄우고,
// 고를 때까지 게임을 멈춰둔다. 영웅을 누르는 순간 고른 팀으로 판이 시작된다.
// 지금은 영웅이 트레이서 하나라 씬에 있는 플레이어를 그대로 쓴다. 두 번째 영웅이
// 생기면 고른 영웅을 생성하도록 바꾼다.
// 플레이어가 Start에서 커서를 잠그므로 그 뒤에 풀어야 해서 늦게 실행한다.
[DefaultExecutionOrder(100)]
public class HeroSelectScreen : MonoBehaviour
{
    [Tooltip("영웅 선택 패널. 고르면 꺼진다.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button tracerButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Color selectedColor = new Color(1f, 0.6f, 0.1f);
    [SerializeField] private Color unselectedColor = new Color(0.35f, 0.38f, 0.45f);

    private Team side = Team.Attack;

    private void Start()
    {
        tracerButton.onClick.AddListener(Pick);
        attackButton.onClick.AddListener(ChooseAttack);
        defendButton.onClick.AddListener(ChooseDefend);
        RefreshSide();
        Show();
    }

    private void OnDestroy()
    {
        tracerButton.onClick.RemoveListener(Pick);
        attackButton.onClick.RemoveListener(ChooseAttack);
        defendButton.onClick.RemoveListener(ChooseDefend);
    }

    private void ChooseAttack() => SetSide(Team.Attack);
    private void ChooseDefend() => SetSide(Team.Defend);

    private void SetSide(Team value)
    {
        side = value;
        SoundManager.Play(SoundId.UiClick);
        RefreshSide();
    }

    // 고른 쪽 버튼만 강조한다.
    private void RefreshSide()
    {
        attackButton.image.color = side == Team.Attack ? selectedColor : unselectedColor;
        defendButton.image.color = side == Team.Defend ? selectedColor : unselectedColor;
    }

    private void Show()
    {
        panel.SetActive(true);
        GameManager.Instance.SetPaused(true);
    }

    private void Pick()
    {
        SoundManager.Play(SoundId.UiSelect);
        panel.SetActive(false);
        GameManager.Instance.BeginAs(side);
        GameManager.Instance.SetPaused(false);
    }
}
