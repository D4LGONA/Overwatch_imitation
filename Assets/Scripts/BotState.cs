using System;

// 봇 상태 하나. 들어올 때(Enter), 있는 동안 매 프레임(Tick), 나갈 때(Exit) 할 일을 정한다.
// MonoBehaviour가 아니라 BotAI 안에 들어가 있는 평범한 클래스라서, 설정값을
// 인스펙터에서 상태별로 묶어 보여줄 수 있다.
[Serializable]
public abstract class BotState
{
    // 시야, 조준, 길찾기처럼 상태들이 같이 쓰는 기능은 BotAI가 갖고 있다.
    protected BotAI ai;

    public void Init(BotAI owner)
    {
        ai = owner;
    }

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }
}
