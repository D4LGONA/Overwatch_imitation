using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 한 판이 끝나면 결과를 띄우고 게임을 멈춘다.
public class ResultScreen : MonoBehaviour
{
    [Tooltip("결과가 뜰 때 켜지는 패널. 처음엔 알아서 꺼둔다.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [Tooltip("없어도 된다. 있으면 누를 때 판을 다시 시작한다.")]
    [SerializeField] private Button restartButton;
    [Tooltip("없어도 된다. 있으면 누를 때 타이틀 씬으로 돌아간다.")]
    [SerializeField] private Button titleButton;
    [SerializeField] private string titleScene = "Title";

    [SerializeField] private string victoryText = "Victory!";
    [SerializeField] private string defeatText = "Defeat";

    private MatchManager match;

    private void Start()
    {
        panel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
        if (titleButton != null)
            titleButton.onClick.AddListener(BackToTitle);

        // 싱글톤은 다른 스크립트의 Awake 순서와 엉키지 않도록 Start에서 꺼낸다.
        match = GameManager.Instance.Match;
        match.MatchEnded += OnMatchEnded;
    }

    private void OnDestroy()
    {
        if (match != null)
            match.MatchEnded -= OnMatchEnded;
        if (restartButton != null)
            restartButton.onClick.RemoveListener(Restart);
        if (titleButton != null)
            titleButton.onClick.RemoveListener(BackToTitle);
    }

    // 플레이어가 고른 팀 기준으로 이겼는지 본다.
    private void OnMatchEnded(bool attackersWon)
    {
        bool won = attackersWon == (GameManager.Instance.PlayerTeam == Team.Attack);
        resultText.text = won ? victoryText : defeatText;
        panel.SetActive(true);
        // 죽어 있는 채로 판이 끝나도 결과 소리는 들려야 한다.
        SoundManager.SetMuted(false);
        SoundManager.Play(won ? SoundId.Victory : SoundId.Defeat);

        GameManager.Instance.SetPaused(true);
    }

    private void Restart()
    {
        SoundManager.Play(SoundId.UiClick);
        // 되돌리지 않으면 새로 불러온 씬도 멈춘 채로 시작한다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BackToTitle()
    {
        SoundManager.Play(SoundId.UiClick);
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleScene);
    }
}
