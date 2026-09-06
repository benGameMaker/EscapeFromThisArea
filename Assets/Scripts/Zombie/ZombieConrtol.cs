using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieConrtol : BaseObject
{
    public float atkDistance;
    public float bashAtkDistance;
    [Tooltip("僵尸离开车辆后延迟多久恢复Rigidbody的Kinematic（给车辆时间把僵尸推开）")]
    public float kinematicRestoreDelay;

    MeleeRange mrAtk;
    MeleeRange mrBashAtk;
    ZombieConeVisionConfig ZombieConfig;

    // 车辆接触攻击状态：僵尸(追车/玩家开车)碰触到车辆 → 取消kinematic以便车辆推动，并原地攻击车辆
    int touchingVehicleCount;
    bool isTouchingVehicle;
    Coroutine restoreRbCoroutine;

    protected override void Awake()
    {
        base.Awake();
        BodyParts.Add(transform.Find("Head").GetComponent<BoxCollider>());
        BodyParts.Add(transform.Find("Hips_jnt").GetComponent<BoxCollider>());
        ZombieConfig = GetComponent<ZombieConeVisionConfig>();
        mrAtk = transform.Find("MeleeRange_Atk").GetComponent<MeleeRange>();
        mrBashAtk = transform.Find("MeleeRange_BashAtk").GetComponent<MeleeRange>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (isDead) return;   // 死亡后不再执行任何移动/攻击行为
        Move(1f);
        TriggerMelee();
    }

    public void Inti(ZombieInfo info)
    {
        maxHP = info.HP;
        atk = info.atk;
        ZombieConfig.agent.speed = info.speed;
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
        if (isDead) return;   // 已在死亡流程中，避免重复执行
        if (animator != null)
            animator.SetBool("isDead_b", true);
        // 清理车辆攻击状态，避免残留协程/触发器干扰
        isTouchingVehicle = false;
        touchingVehicleCount = 0;
        if (restoreRbCoroutine != null)
        {
            StopCoroutine(restoreRbCoroutine);
            restoreRbCoroutine = null;
        }

        // 死亡后立即停止一切移动行为：暂停锥形视野的持续追踪，并停掉NavMeshAgent，
        // 避免尸体继续沿路径滑动或被"枪声吸引"等重新下令移动
        if (ZombieConfig != null)
        {
            ZombieConfig.PauseChasing();
            if (ZombieConfig.agent != null)
            {
                ZombieConfig.agent.isStopped = true;
                ZombieConfig.agent.ResetPath();
            }
        }

        // 打断正在进行中的近战攻击判定（还在生效的近战碰撞体窗口）
        if (mrAtk != null) mrAtk.CancelMelee();
        if (mrBashAtk != null) mrBashAtk.CancelMelee();

        base.Death();
    }

    void Move(float speedMultiplier)
    {
        base.Move();
        if (ZombieConfig == null || ZombieConfig.agent == null || animator == null)
            return;

        // 导航被暂停期间（贴车攻击中 / 离开车辆后延迟恢复）原地不动，不播放行走
        if (ZombieConfig.isNavigationPaused)
        {
            animator.SetFloat("MoveSpeed", 0f);
            return;
        }

        NavMeshAgent agent = ZombieConfig.agent;
        // 未到达目的点（仍在寻路/移动中，含路径计算中或剩余距离大于停止半径）
        bool isMoving = agent.pathPending || agent.remainingDistance > agent.stoppingDistance;
        animator.SetFloat("MoveSpeed", isMoving ? speedMultiplier : 0f);
    }
    void TriggerMelee()
    {
        if (isDead || animator == null) return;

        // 贴车状态：原地持续用普通攻击打车辆
        if (isTouchingVehicle)
        {
            animator.SetTrigger("isAtk_t");
            return;
        }
        if (ZombieConfig.distance == -1) return;
        //Debug.Log(ZombieConfig.distance);
        if (ZombieConfig.distance <= bashAtkDistance) animator.SetTrigger("isBashAtk_t");
        else if (ZombieConfig.distance <= atkDistance) animator.SetTrigger("isAtk_t");
    }

    protected override void Move()
    {
        base.Move();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 僵尸的"车辆接触"触发器：追车（玩家开车）时碰到车辆就进入贴车攻击状态。
        // 跳过Trigger（如DetectionZombies的球形触发器挂在车上时会误判为接触车辆）
        if (isDead || other.isTrigger) return;
        if (other.GetComponentInParent<VehicleDamage>() == null) return;

        touchingVehicleCount++;
        if (touchingVehicleCount > 1) return; // 已有其他部位接触，保持当前状态

        // 取消离开时待执行的恢复协程（可能是刚被推开又再次贴上）
        if (restoreRbCoroutine != null)
        {
            StopCoroutine(restoreRbCoroutine);
            restoreRbCoroutine = null;
        }
        isTouchingVehicle = true;

        // 让车辆能推动自己：接触瞬间取消kinematic
        if (rb != null) rb.isKinematic = false;
        // 停止追逐/导航，原地开始攻击车辆
        if (ZombieConfig != null) ZombieConfig.PauseChasing();
        if (animator != null) animator.SetTrigger("isAtk_t");
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead || other.isTrigger) return;
        if (other.GetComponentInParent<VehicleDamage>() == null) return;
        touchingVehicleCount--;
        if (touchingVehicleCount > 0) return;
        if (touchingVehicleCount < 0) touchingVehicleCount = 0;

        // 离开车辆：停止攻击，延迟恢复kinematic后继续按原规则追逐
        isTouchingVehicle = false;
        if (restoreRbCoroutine == null)
            restoreRbCoroutine = StartCoroutine(RestoreKinematicAndChase());
    }

    IEnumerator RestoreKinematicAndChase()
    {
        if (ZombieConfig != null) ZombieConfig.ResumeChasing();
        yield return new WaitForSeconds(kinematicRestoreDelay);
        restoreRbCoroutine = null;
        if (isDead) yield break;

        if (rb != null) rb.isKinematic = true;
    }
}
