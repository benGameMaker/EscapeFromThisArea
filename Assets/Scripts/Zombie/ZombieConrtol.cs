using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieConrtol : BaseObject
{
    public float atkDistance = 2f;
    public float bashAtkDistance = 1.5f;

    MeleeRange mrAtk;
    MeleeRange mrBashAtk;
    ZombieConeVisionConfig ZombieConfig;
    protected override void Awake()
    {
        base.Awake();
        ZombieConfig = GetComponent<ZombieConeVisionConfig>();
        mrAtk = transform.Find("MeleeRange_Atk").GetComponent<MeleeRange>();
        mrBashAtk = transform.Find("MeleeRange_BashAtk").GetComponent<MeleeRange>();
    }

    // Update is called once per frame
    void Update()
    {
        Move(1f);
        TriggerMelee();
    }

    public void TriggerMeleeAtk(float time)
    {
        mrAtk.TriggerMeleeAtk(time);
    }

    public void TriggerMeleeBashAtk(float time)
    {
        mrBashAtk.TriggerMeleeAtk(time);
    }

    public void PauseNaivgation()
    {
        ZombieConfig.agent.isStopped = !ZombieConfig.agent.isStopped;
    }

    protected override void Death()
    {
        if (animator != null)
            animator.SetBool("isDead_b", true);
        base.Death();
    }

    void Move(float speedMultiplier)
    {
        base.Move();
        if (ZombieConfig == null || ZombieConfig.agent == null || animator == null)
            return;

        NavMeshAgent agent = ZombieConfig.agent;
        // 未到达目的点（仍在寻路/移动中，含路径计算中或剩余距离大于停止半径）
        bool isMoving = agent.pathPending || agent.remainingDistance > agent.stoppingDistance;
        animator.SetFloat("MoveSpeed", isMoving ? speedMultiplier : 0f);
    }
    void TriggerMelee()
    {
        if (isDead || ZombieConfig.distance == -1) return;
        Debug.Log(ZombieConfig.distance);
        if (ZombieConfig.distance <= bashAtkDistance) animator.SetTrigger("isBashAtk_t");
        else if (ZombieConfig.distance <= atkDistance) animator.SetTrigger("isAtk_t");
    }

    protected override void Move()
    {
        base.Move();
    }

}
