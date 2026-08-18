using UnityEngine;

// 캐릭터를 움직이는 한 프레임 분량의 명령.
//
// 로컬 입력이든 네트워크로 받은 값이든 같은 형태로 모터에 들어간다. 모터가
// 명령의 출처를 모르기 때문에 리모트 캐릭터가 같은 코드를 그대로 쓸 수 있고,
// 나중에 클라이언트 예측에서 같은 명령을 재생하면 같은 결과가 나온다.
public struct CharacterCommand
{
    // 에임 기준 이동 입력. x가 좌우, y가 앞뒤.
    public Vector2 Move;

    // 절대 각도. 몸 회전과 별개로 굴러간다.
    public float AimYaw;
    public float Pitch;

    // 이번 프레임에 눌렸는지. 누르고 있는 상태가 아니다.
    public bool Jump;
}
