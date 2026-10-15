// 효과음 종류. 문자열 대신 이걸로 부르면 오타가 컴파일 때 잡힌다.
// SoundTable 에셋에는 숫자로 저장되므로, 중간에 끼워 넣지 말고 새 값은 끝에 번호를 붙여 추가한다.
public enum SoundId
{
    TracerFire = 0,
    HitMarker = 1,
    KillConfirm = 2,
    PlayerHurt = 3,
    Death = 4,
    Blink = 5,
    Recall = 6,
    PulseThrow = 7,
    PulseStick = 8,
    PulseExplode = 9,
    Melee = 10,
    Reload = 11,
    BotFire = 12,
    UiClick = 13,
    UiSelect = 14,
    PointCaptured = 15,
    Overtime = 16,
    DoorOpen = 17,
    Victory = 18,
    Defeat = 19,
    Footstep = 20,
}
