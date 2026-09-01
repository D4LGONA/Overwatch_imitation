using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 능력 슬롯 하나의 표시. 충전이 여러 개인 점멸도, 하나뿐인 스킬도 같은 걸 쓴다.
public class AbilitySlotUI : MonoBehaviour
{
    [Tooltip("충전이 없을 때 어둡게 만들 아이콘.")]
    [SerializeField] private Image icon;
    [Tooltip("아이콘 위를 덮는 이미지. Filled / Radial 360이어야 한다.")]
    [SerializeField] private Image cooldownFill;
    [Tooltip("남은 충전 수. 최대가 1이면 자동으로 숨긴다.")]
    [SerializeField] private TMP_Text countText;

    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    // progress는 다음 충전까지의 진행도(0~1). 가득 차 있으면 1을 넘긴다.
    public void Set(int charges, int maxCharges, float progress)
    {
        if (icon != null)
            icon.color = charges > 0 ? readyColor : emptyColor;

        // 덮개가 줄어들면서 아이콘이 드러나는 방향이라 1에서 빼준다.
        if (cooldownFill != null)
            cooldownFill.fillAmount = charges >= maxCharges ? 0f : 1f - Mathf.Clamp01(progress);

        if (countText != null)
        {
            bool showCount = maxCharges > 1;
            countText.gameObject.SetActive(showCount);
            if (showCount)
                countText.text = charges.ToString();
        }
    }
}
