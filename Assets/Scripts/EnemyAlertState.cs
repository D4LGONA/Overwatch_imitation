using System;
using UnityEngine;

// 시야 밖에서 맞았을 때 때린 쪽을 돌아본다. 돌아봐서 보이면 쫓아가고, 끝내 안 보이면 순찰로 돌아간다.
[Serializable]
public class EnemyAlertState : EnemyState
{
    [Tooltip("때린 쪽을 바라보며 찾는 시간(초).")]
    [SerializeField] private float duration = 2f;

    private Vector3 alertPoint;
    private float until;

    // 맞을 때마다 불린다. 이미 돌아보는 중에 또 맞으면 바라볼 곳과 시간을 새로 정한다.
    public void SetAlert(Vector3 point)
    {
        alertPoint = point;
        until = Time.time + duration;
    }

    public override void Enter()
    {
        // 제자리에 멈춰서 돌아본다.
        ai.Agent.ResetPath();
    }

    public override void Tick()
    {
        if (ai.TargetVisible)
        {
            ai.ChangeState(ai.Chase);
            return;
        }

        ai.LookAt(alertPoint);

        if (Time.time >= until)
            ai.ChangeState(ai.Idle);
    }
}
