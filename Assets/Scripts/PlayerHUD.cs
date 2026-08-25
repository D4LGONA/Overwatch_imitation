using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 내 캐릭터의 상태를 화면에 띄운다. 값을 스스로 찾지 않고 받기만 하므로
// 어떤 캐릭터를 골라도 이 스크립트는 그대로 쓴다.
public class PlayerHUD : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Health")]
    [Tooltip("Image Type이 Filled여야 fillAmount가 먹는다.")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;

    public void SetAmmo(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"{current} / {max}";
    }

    public void SetHealth(float current, float max)
    {
        if (healthFill != null)
            healthFill.fillAmount = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
