using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class VehicleDamage : MonoBehaviour
{
    public bool intiVehicleDamage = false;// 初始化时就将车辆损毁
    public float curIntegrity;
    public float maxIntegrity = 100f;
    public bool isBroke = false;
    public GameObject brokenEffectPrefab;
    public AudioClip brokenSound;
    [HideInInspector]
    public float curVelocity;
    [HideInInspector]
    public VehiclePanel vp;
    [HideInInspector]
    public VehicleController vc;
    Rigidbody rb;

    [Tooltip("吸引僵尸的球形触发器(挂在车辆上，玩家驾驶时进入范围的僵尸会被导航来攻击车辆；不拖拽则自动在子物体上查找)")]
    public SphereCollider attractTrigger;
    [Tooltip("对同一目标两次碰撞结算车辆伤害的最小间隔(秒)，避免接触抖动导致每帧重复扣血")]
    public float hitInterval = 0.5f;
    readonly Dictionary<Transform, float> nextHitTimes = new Dictionary<Transform, float>();
    [Tooltip("当前是否正被玩家驾驶(由VehicleInteraction上/下车时通过SetDriven设置)")]
    bool isDriven = false;
    // 当前活跃的检测器(DetectionZombies)，代表玩家当前控制的实体(徒步=玩家、开车=车辆)
    DetectionZombies detector;
    // 通过球形触发器进入吸引范围的僵尸名单，下车时统一切回攻击玩家
    readonly List<ZombieConrtol> attractedZombies = new List<ZombieConrtol>();


    // 新增：粒子系统引用
    private ParticleSystem brokenParticleSystem;
    private ParticleSystem.EmissionModule emissionModule;
    private bool isEffectInitialized = false;
    private void Start()
    {
        curIntegrity = maxIntegrity;
        rb = GetComponent<Rigidbody>();
        vc = GetComponent<VehicleController>();
        // 未在Inspector拖拽球形触发器时，自动在子物体上查找第一个SphereCollider
        if (attractTrigger == null) attractTrigger = GetComponentInChildren<SphereCollider>(true);
        InitializeEffect();
        if(intiVehicleDamage)
        {
            DamageVehicle(maxIntegrity);
        }
    }

    private void FixedUpdate()
    {
        PruneHitTimers();
        curVelocity = transform.InverseTransformDirection(rb.velocity).z;
        if (vp != null && vp.isShow) vp.UpdataCurVelocity(curVelocity);
        //else Debug.Log("未获取到VehiclePanel面板或者该面板已经隐藏");
        // 新增：每帧更新烟雾特效
        UpdateSmokeEffect();
    }

    public void RepairVehicle(int num)
    {
        curIntegrity += num * Random.Range(1f, 5f);
        //Debug.Log("车辆恢复了" + curIntegrity + "完整度");
        if (curIntegrity > maxIntegrity) curIntegrity = maxIntegrity;
        if (vp != null) vp.UpdataCurIntegrity(curIntegrity, maxIntegrity);
        if (curIntegrity >= 80f && isBroke)
        {
            isBroke = false;
            if (vc != null) vc.SetVehicleState(true);
            if (vp != null) vp.ShowOrHideNotice(false);
            //Debug.Log("车辆车辆已基本修复");
        }
    }
    public void DamageVehicle(float value)
    {
        if (curIntegrity <= 0) return; // 已损坏无需重复处理
        curIntegrity -= value;
        if (curIntegrity < 0) curIntegrity = 0;
        if(curIntegrity >= maxIntegrity)
        {
            curIntegrity = maxIntegrity;
        }
        //Debug.Log("这次撞击损失了" + value + "车辆完整度");
        if (vp != null) vp.UpdataCurIntegrity(curIntegrity, maxIntegrity);
        if (curIntegrity <= 0)
        {
            isBroke = true;
            if (vc != null) vc.SetVehicleState(false);
            if (vp != null) vp.ShowOrHideNotice(true);
            GameDataMgr.Instance.PlaySound(brokenSound, 0.9f);
            //Debug.Log("车辆已损坏");
        }
    }

    // 初始化特效
    private void InitializeEffect()
    {
        if (brokenEffectPrefab != null)
        {
            brokenParticleSystem = brokenEffectPrefab.GetComponent<ParticleSystem>();
            if (brokenParticleSystem != null)
            {
                emissionModule = brokenParticleSystem.emission;
                isEffectInitialized = true;
                // 初始设置 RateOverTime 为 0
                SetEmissionRate(0f);
                // 初始时禁用特效
                brokenEffectPrefab.SetActive(false);
            }
            else
            {
                Debug.LogWarning("brokenEffectPrefab 没有 ParticleSystem 组件！");
            }
        }
        else
        {
            Debug.LogWarning("brokenEffectPrefab 未赋值！");
        }
    }
    // 设置 Emission Rate
    private void SetEmissionRate(float rate)
    {
        if (!isEffectInitialized) return;

        var rateOverTime = emissionModule.rateOverTime;
        rateOverTime.constant = rate;
        emissionModule.rateOverTime = rateOverTime;
    }
    // 更新烟雾特效
    private void UpdateSmokeEffect()
    {
        if (!isEffectInitialized) return;

        // 计算当前完整性百分比 (0~100)
        float integrityPercent = curIntegrity;

        // 判断是否应该显示特效
        if (integrityPercent >= 80f)
        {
            // 完整度 >= 80：特效关闭，Rate = 0
            if (brokenEffectPrefab.activeSelf)
            {
                brokenEffectPrefab.SetActive(false);
            }
            SetEmissionRate(0f);
        }
        else if (integrityPercent > 0f)
        {
            // 0 < 完整度 < 80：特效开启，Rate 从 5 到 25 匀速变化
            if (!brokenEffectPrefab.activeSelf)
            {
                brokenEffectPrefab.SetActive(true);
            }

            // 计算 Rate：范围从 5 到 25
            // 当 integrity = 80 时，rate = 5
            // 当 integrity 无限接近 0 时，rate = 25
            float t = 1f - (integrityPercent / 80f); // 0~1，其中 0 对应 80，1 对应 0
            float rate = Mathf.Lerp(5f, 25f, t);

            // 确保在 5~25 范围内
            rate = Mathf.Clamp(rate, 5f, 25f);
            SetEmissionRate(rate);
        }
        else // integrityPercent <= 0
        {
            // 完整度 = 0：特效开启，Rate = 50
            if (!brokenEffectPrefab.activeSelf)
            {
                brokenEffectPrefab.SetActive(true);
            }
            SetEmissionRate(50f);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"{collision.gameObject.name} frame={Time.frameCount} t={Time.fixedTime}");
        if (collision.gameObject.CompareTag("Player")) return;
        //Debug.Log("车辆碰撞了" + collision.gameObject.name + "，此时速度为：" + curVelocity);

        // 简单节流：碰撞体抖动会导致同一物体每物理帧"重新进入"，
        // 对同一目标(以碰撞体根节点区分)在hitInterval内只结算一次车辆伤害。
        Transform otherRoot = collision.transform.root;
        if (nextHitTimes.TryGetValue(otherRoot, out float nextTime) && Time.time < nextTime) return;

        float damageCoefficientt;
        if (collision.gameObject.CompareTag("Zombie")) damageCoefficientt = 2f;
        else damageCoefficientt = 5f;
        if (Mathf.Abs(curVelocity) > 20f) DamageVehicle(damageCoefficientt * 0.8f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 15f) DamageVehicle(damageCoefficientt * 0.5f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 5f) DamageVehicle(damageCoefficientt * 0.3f * curVelocity);
        else return;

        nextHitTimes[otherRoot] = Time.time + hitInterval;
    }

    // 清理过期/已销毁目标的节流记录，避免字典无限增长
    void PruneHitTimers()
    {
        if (nextHitTimes.Count == 0) return;
        foreach (Transform key in new List<Transform>(nextHitTimes.Keys))
        {
            if (key == null || Time.time >= nextHitTimes[key]) nextHitTimes.Remove(key);
        }
    }

    /// <summary>
    /// 由 VehicleInteraction 在上/下车时调用，同步"玩家当前控制的实体"到车辆。
    /// 上车(driven=true)：进入球形触发器的僵尸将被吸引攻击车辆；
    /// 下车(driven=false)：把仍在被本车吸引的僵尸统一切回攻击玩家。
    /// activeDetector 为当前活跃的 DetectionZombies（上下车时它会被改挂到玩家/车辆下）。
    /// </summary>
    public void SetDriven(bool driven, DetectionZombies activeDetector)
    {
        isDriven = driven;
        detector = activeDetector;

        // 下车：切换目标回玩家（检测器此时已挂回玩家，僵尸据此自动追玩家）
        if (!driven) RetargetAttractedZombies();
    }

    // 球形触发器：玩家驾驶时，僵尸进入范围就强制追踪当前控制实体(车辆)
    private void OnTriggerEnter(Collider other)
    {
        if (!isDriven || detector == null) return;

        ZombieConrtol zombie = other.GetComponentInParent<ZombieConrtol>();
        if (zombie == null || zombie.isDead) return;
        if (attractedZombies.Contains(zombie)) return;

        attractedZombies.Add(zombie);
        zombie.ForceChaseTarget(detector.transform);
    }

    // 球形触发器：僵尸离开范围时从名单移除(不额外打断其当前行为)
    private void OnTriggerExit(Collider other)
    {
        ZombieConrtol zombie = other.GetComponentInParent<ZombieConrtol>();
        if (zombie != null) attractedZombies.Remove(zombie);
    }

    // 下车时把被本车吸引的僵尸统一切回攻击玩家，并清空名单
    void RetargetAttractedZombies()
    {
        Transform target = detector != null ? detector.transform : null;
        for (int i = 0; i < attractedZombies.Count; i++)
        {
            if (attractedZombies[i] != null)
                attractedZombies[i].ForceChaseTarget(target);
        }
        attractedZombies.Clear();
    }
}
