using System;
using UnityEngine;

// 보이는 상대를 쫓아간다. 시야에서 벗어나면 바로 순찰로 돌아간다.
// 거점을 밟는 게 먼저라, 쫓더라도 목적지를 거점 안으로 묶는다. 공격팀은 거점을 비우면 점령이 멈추고,
// 수비팀은 거점을 비우면 경합으로 막지 못한다.
[Serializable]
public class BotChaseState : BotState
{
    [Tooltip("쫓아갈 때 상대와 이 거리에서 멈춘다.")]
    [SerializeField] private float stopDistance = 3f;
    [Tooltip("거점 중심에서 이 반경 밖으로는 쫓아가지 않는다. 거점(5x5)보다 작게.")]
    [SerializeField] private float holdRadius = 2f;
    [Tooltip("길을 다시 찾는 간격(초). 매 프레임 찾으면 여럿이 쫓을 때 무겁다.")]
    [SerializeField] private float repathInterval = 0.25f;

    private float nextRepathTime;

    public override void Enter()
    {
        ai.Agent.stoppingDistance = stopDistance;
        nextRepathTime = 0f;
    }

    public override void Tick()
    {
        if (!ai.TargetVisible || ai.Target == null || !ai.Target.IsAlive)
        {
            ai.ChangeState(ai.Idle);
            return;
        }

        if (Vector3.Distance(ai.transform.position, ai.Target.transform.position) <= ai.Attack.Range)
        {
            ai.ChangeState(ai.Attack);
            return;
        }

        // 쳐다보기는 매 프레임, 길찾기는 가끔.
        ai.LookAtTarget();
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            ai.Agent.SetDestination(LeashedPosition(holdRadius));
        }
    }

    // 상대 쪽으로 가되 거점 중심에서 radius 안으로 잘라낸 위치.
    // 상대가 그 안에 있으면 상대 위치 그대로다. 거점에서 멀리 있을 때는 이 위치가 거점 쪽이라,
    // 상대를 쳐다보면서 거점으로 돌아가거나 밀고 들어간다.
    private Vector3 LeashedPosition(float radius)
    {
        Vector3 home = ai.Home;
        Vector3 toTarget = ai.Target.transform.position - home;
        toTarget.y = 0f;
        return home + Vector3.ClampMagnitude(toTarget, radius);
    }
}
