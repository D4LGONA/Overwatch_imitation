using TMPro;
using UnityEngine;

// 내가 죽으면 화면을 어둡게 덮고, 누구에게 당했는지와 부활까지 남은 시간을 보여준다.
// 부활할 때까지는 소리도 모두 끈다.
public class DeathScreen : MonoBehaviour
{
    [Tooltip("죽어 있는 동안 켜지는 패널.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text killedByText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);

    private Health health;
    private Respawner respawner;

    private void Start()
    {
        panel.SetActive(false);
        SoundManager.SetMuted(false);
        PlayerController.WhenReady(Bind);
    }

    private void Bind(PlayerController player)
    {
        if (this == null)
            return;

        health = player.Health;
        respawner = player.GetComponent<Respawner>();
        health.Died += OnDied;
        health.Revived += OnRevived;
    }

    private void OnDestroy()
    {
        if (health == null)
            return;

        health.Died -= OnDied;
        health.Revived -= OnRevived;
    }

    private void OnDied()
    {
        Health killer = health.LastAttacker;
        string color = ColorUtility.ToHtmlStringRGB(enemyColor);
        killedByText.text = killer != null && killer != health
            ? $"ELIMINATED BY <color=#{color}>{killer.name.ToUpperInvariant()}</color>"
            : "YOU WERE ELIMINATED";
        panel.SetActive(true);
        SoundManager.SetMuted(true);
    }

    private void OnRevived()
    {
        panel.SetActive(false);
        SoundManager.SetMuted(false);
    }

    private void Update()
    {
        if (!panel.activeSelf || respawner == null)
            return;

        countdownText.text = $"RESPAWN IN {Mathf.CeilToInt(respawner.RespawnRemaining)}";
    }
}
