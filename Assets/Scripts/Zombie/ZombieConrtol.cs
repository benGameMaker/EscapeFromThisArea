using System.Collections;
using UnityEngine;

public class ZombieConrtol : BaseObject
{
    public float atkDistance;
    public float bashAtkDistance;
    [Tooltip("僵尸离开车辆后延迟多久恢复Rigidbody的Kinematic（给车辆时间把僵尸推开）")]
    public float kinematicRestoreDelay;
    [Tooltip("僵尸受伤时播放的音频(不循环)")]
    public AudioClip hurtSound;
    [Tooltip("僵尸死亡时播放的音频(不循环)")]
    public AudioClip deathSound;
    [Tooltip("僵尸只有玩家在该距离内受伤/死亡时才会播放声音")]
    public float soundHearRange = 30f;
    [Tooltip("僵尸最后一次接触车辆后，超过该时长仍无新的接触事件才判定离开车辆")]
    public float vehicleTouchGrace = 1.2f;

    MeleeRange mrAtk;
    MeleeRange mrBashAtk;
    ZombieConeVisionConfig ZombieConfig;

    // 车辆接触状态：僵尸(追车/玩家开车)碰到车辆 → 立即原地持续攻击车。
    // 接触期间取消kinematic让车辆能推动僵尸；离开后延迟恢复kinematic并继续追逐。
    // 不依赖 Enter/Exit 配对计数(攻击碰撞体会与车身多个Collider反复进出)，
    // 而是靠"最后一次接触事件" + vehicleTouchGrace 保持/清除贴车状态
    bool isTouchingVehicle;
    float lastVehicleTouchTime = float.NegativeInfinity;
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
        UpdateVehicleTouch();
        Move(1f);
        TriggerMelee();
    }

    // 接触结束判定：一段时间没有新的接触事件 → 视为已离开车辆，恢复导航并延迟恢复kinematic
    void UpdateVehicleTouch()
    {
        if (!isTouchingVehicle) return;
        if (Time.time - lastVehicleTouchTime < vehicleTouchGrace) return;

        isTouchingVehicle = false;
        if (restoreRbCoroutine == null)
            restoreRbCoroutine = StartCoroutine(RestoreKinematicAndChase());
    }

    public void Inti(ZombieInfo info)
    {
        maxHP = info.HP;
        atk = info.atk;
        ZombieConfig.agent.speed = info.speed;
    }

    // 攻击动画事件调用：开启本次攻击的伤害判定（一次攻击只结算一次，见MeleeRange）
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

    //受伤音(不循环)：仅玩家在soundHearRange距离内时才播放
    public override void Injured(float harm)
    {
        base.Injured(harm);
        //致死那一次由Death播放死亡音，此处不重复播
        if (!isDead && hurtSound != null && IsPlayerInSoundRange())
            GameDataMgr.Instance.PlaySound(hurtSound);
    }

    //判断玩家是否在僵尸声音可听见范围内
    bool IsPlayerInSoundRange()
    {
        PlayerController player = SceneMgr.Instance != null ? SceneMgr.Instance.player : null;
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.transform.position) <= soundHearRange;
    }

    protected override void Death()
    {
        if (isDead) return;   // 已在死亡流程中，避免重复执行
        //僵尸死亡音(不循环)：仅玩家在一定距离内才播放
        if (deathSound != null && IsPlayerInSoundRange())
            GameDataMgr.Instance.PlaySound(deathSound);
        if (animator != null)
            animator.SetBool("isDead_b", true);
        // 清理车辆接触状态，避免残留协程/触发器干扰
        isTouchingVehicle = false;
        lastVehicleTouchTime = float.NegativeInfinity;
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

        // 死亡后把全身所有碰撞器都失活（根节点身体Trigger、Head/Hips实体盒、
        // MeleeRange攻击碰撞器等），避免尸体继续触发车辆碰撞/接触回调；
        // 刚体改为Kinematic，尸体不再受物理推动。
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allColliders) col.enabled = false;
        if (rb != null) rb.isKinematic = true;

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

        UnityEngine.AI.NavMeshAgent agent = ZombieConfig.agent;
        // 未到达目的点（仍在寻路/移动中，含路径计算中或剩余距离大于停止半径）
        bool isMoving = agent.pathPending || agent.remainingDistance > agent.stoppingDistance;
        animator.SetFloat("MoveSpeed", isMoving ? speedMultiplier : 0f);
    }

    void TriggerMelee()
    {
        if (isDead || animator == null) return;

        // 僵尸身体碰到车辆(追车)时：立即并持续播放重击动画攻击车辆。
        // 不控制朝向/攻击频率；伤害由攻击动画机事件调用TriggerMeleeBashAtk，
        // 交给MeleeRange结算（一次攻击只结算一次）
        if (isTouchingVehicle)
        {
            animator.SetTrigger("isBashAtk_t");
            return;
        }

        // 追击玩家等普通目标：按目标距离触发普攻/重击动画
        if (ZombieConfig == null || ZombieConfig.distance == -1) return;
        if (ZombieConfig.distance <= bashAtkDistance) animator.SetTrigger("isBashAtk_t");
        else if (ZombieConfig.distance <= atkDistance) animator.SetTrigger("isAtk_t");
    }

    protected override void Move()
    {
        base.Move();
    }

    // 碰到车辆(玩家驾驶时被吸引追车)：身体或攻击碰撞体与车身接触都算，立即进入贴车攻击。
    // 跳过Trigger(如车上检测器的球形触发器、伤害区等)只算真接触车身
    private void OnTriggerEnter(Collider other)
    {
        if (isDead || other.isTrigger) return;
        if (other.GetComponentInParent<VehicleDamage>() != null) EnterVehicleContact();
    }

    private void OnTriggerExit(Collider other)
    {
        // 退出不做状态判定，统一由 UpdateVehicleTouch 的 vehicleTouchGrace 处理，
        // 避免攻击窗口启停造成 Exit 抖动导致贴车状态被误清除
        if (isDead || other.isTrigger) return;
        if (other.GetComponentInParent<VehicleDamage>() != null)
            lastVehicleTouchTime = Time.time;
    }

    // 物理碰撞兜底：僵尸身体(非Trigger碰撞体)真正压到车身也立刻进入贴车攻击
    // (NavMesh把僵尸带到车边时，身体可能刚好贴上，走这里比攻击碰撞体更可靠)
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        if (collision.transform.GetComponentInParent<VehicleDamage>() != null) EnterVehicleContact();
    }

    // 进入贴车攻击：刷新最后接触时间并开始攻击车辆(已处于贴车状态则只刷新时间)
    void EnterVehicleContact()
    {
        lastVehicleTouchTime = Time.time;
        if (isTouchingVehicle) return; // 已在贴车状态，无需重复处理

        if (restoreRbCoroutine != null)
        {
            StopCoroutine(restoreRbCoroutine);
            restoreRbCoroutine = null;
        }
        isTouchingVehicle = true;

        // 让车辆能撞动僵尸(僵尸不再推车)：接触期间取消kinematic
        if (rb != null) rb.isKinematic = false;
        // 暂停导航，原地攻击车辆（避免NavMeshAgent与物理冲突）
        if (ZombieConfig != null) ZombieConfig.PauseChasing();
        if (animator != null) animator.SetTrigger("isBashAtk_t");
    }

    IEnumerator RestoreKinematicAndChase()
    {
        if (ZombieConfig != null) ZombieConfig.ResumeChasing();
        yield return new WaitForSeconds(kinematicRestoreDelay);
        restoreRbCoroutine = null;
        if (isDead) yield break;

        if (rb != null) rb.isKinematic = true;
    }

    /// <summary>
    /// 外部强制切换僵尸的追逐目标（车辆球形触发器吸引、玩家上/下车切换控制实体时使用）。
    /// 会先清理"贴车攻击"状态并恢复刚体/navmesh导航，再把持续追踪目标切到指定检测器。
    /// 检测器(DetectionZombies)在玩家与车辆间改挂，僵尸据此自动攻击玩家当前控制的实体。
    /// </summary>
    public void ForceChaseTarget(Transform target)
    {
        if (isDead) return;

        // 结束贴车攻击状态：恢复刚体、停止"离开车辆后延迟恢复"的协程
        isTouchingVehicle = false;
        lastVehicleTouchTime = float.NegativeInfinity;
        if (restoreRbCoroutine != null)
        {
            StopCoroutine(restoreRbCoroutine);
            restoreRbCoroutine = null;
        }
        if (rb != null) rb.isKinematic = true;

        // 切换持续追踪目标（target 为检测器时自动跟随玩家当前控制的实体）
        if (ZombieConfig != null) ZombieConfig.ForceTraceTarget(target);
    }
}
