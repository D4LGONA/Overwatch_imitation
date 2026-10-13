using System;
using UnityEngine;

// 보이는 상대를 쫓아간다. 시야에서 벗어나면 바로 순찰로 돌아간다.
[Serializable]
public class EnemyChaseState : EnemyState
{
    [Tooltip("쫓아갈 때 상대와 이 거리에서 멈춘다.")]
    [SerializeField] private float stopDistance = 3f;

    public override void Enter()
    {
        ai.Agent.stoppingDistance = stopDistance;
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

        ai.Agent.SetDestination(ai.Target.transform.position);
        ai.LookAtTarget();
    }
}
