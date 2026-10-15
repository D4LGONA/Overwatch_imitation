using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임을 켜면 처음 뜨는 화면. 시작하면 게임 씬으로 넘어간다.
public class TitleScreen : MonoBehaviour
{
    [Tooltip("시작을 누르면 불러올 씬 이름. Build Settings에 들어 있어야 한다.")]
    [SerializeField] private string gameScene = "SampleScene";
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // 게임에서 결과 화면을 거쳐 돌아오면 시간이 멈추고 커서가 잠겨 있을 수 있다.
        Time.timeScale = 1f;
        SoundManager.SetMuted(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playButton.onClick.AddListener(Play);
        quitButton.onClick.AddListener(Quit);
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(Play);
        quitButton.onClick.RemoveListener(Quit);
    }

    private void Play()
    {
        SoundManager.Play(SoundId.UiClick);
        SceneManager.LoadScene(gameScene);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        // 에디터에서는 Application.Quit이 아무것도 안 해서 플레이 모드를 끈다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
