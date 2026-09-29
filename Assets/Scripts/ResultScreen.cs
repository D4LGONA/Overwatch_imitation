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

    [SerializeField] private string victoryText = "Victory!";
    [SerializeField] private string defeatText = "Defeat";

    private MatchManager match;

    private void Start()
    {
        panel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);

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
    }

    // 이 판은 공격팀(0)으로 플레이하니까 공격팀이 이기면 승리다.
    private void OnMatchEnded(bool attackersWon)
    {
        resultText.text = attackersWon ? victoryText : defeatText;
        panel.SetActive(true);

        // 시간을 멈추면 이동·애니메이션·파티클·타이머가 선다. 하지만 마우스 회전이나
        // 점멸처럼 시간에 곱해지지 않는 동작은 계속되므로 입력도 끊는다.
        Time.timeScale = 0f;
        foreach (InputReader input in FindObjectsOfType<InputReader>())
            input.enabled = false;

        // 버튼을 누를 수 있게 잠가둔 커서를 푼다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Restart()
    {
        // 되돌리지 않으면 새로 불러온 씬도 멈춘 채로 시작한다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
