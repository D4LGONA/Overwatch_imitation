using UnityEngine;

// 탄퍼짐 각도에 맞춰 조준선 원의 크기를 맞춘다. 각도를 스스로 정하지 않고
// 무기를 든 캐릭터가 알려주므로, 캐릭터가 바뀌면 원 크기도 따라 바뀐다.
[RequireComponent(typeof(RectTransform))]
public class Crosshair : MonoBehaviour
{
    [Tooltip("비워두면 메인 카메라를 쓴다.")]
    [SerializeField] private Camera targetCamera;

    public void SetSpread(float angleDeg)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        float radius = ScreenRadius(targetCamera, angleDeg);
        ((RectTransform)transform).sizeDelta = Vector2.one * radius * 2f;
    }

    // fieldOfView는 수직 기준이라 화면 높이로 계산해야 비율이 맞는다.
    private static float ScreenRadius(Camera cam, float angleDeg)
    {
        float pixelsAtOneMeter = Screen.height * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return pixelsAtOneMeter * Mathf.Tan(angleDeg * Mathf.Deg2Rad);
    }
}
