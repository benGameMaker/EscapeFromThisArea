using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class VehicleDamage : MonoBehaviour
{
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


    // 新增：粒子系统引用
    private ParticleSystem brokenParticleSystem;
    private ParticleSystem.EmissionModule emissionModule;
    private bool isEffectInitialized = false;
    private void Start()
    {
        curIntegrity = maxIntegrity;
        rb = GetComponent<Rigidbody>();
        vc = GetComponent<VehicleController>();
        InitializeEffect();
    }

    private void FixedUpdate()
    {
        curVelocity = transform.InverseTransformDirection(rb.velocity).z;
        if (vp != null && vp.isShow) vp.UpdataCurVelocity(curVelocity);
        //else Debug.Log("未获取到VehiclePanel面板或者该面板已经隐藏");
        // 新增：每帧更新烟雾特效
        UpdateSmokeEffect();
    }

    public void RepairVehicle(int num)
    {
        curIntegrity += num * Random.Range(0.2f, 2f);
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
        //Debug.Log("这次撞击损失了" + value + "车辆完整度");
        if (vp != null) vp.UpdataCurIntegrity(curIntegrity, maxIntegrity);
        if (curIntegrity <= 0)
        {
            isBroke = true;
            if (vc != null) vc.SetVehicleState(false);
            if (vp != null) vp.ShowOrHideNotice(true);
            GameDataMgr.Instance.PlaySound(brokenSound, 1f);
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
        if (collision.gameObject.CompareTag("Player")) return;
        //Debug.Log("车辆碰撞了" + collision.gameObject.name + "，此时速度为：" + curVelocity);
        float damageCoefficientt;
        if (collision.gameObject.CompareTag("Zombie")) damageCoefficientt = 2f;
        else damageCoefficientt = 5f;
        if (Mathf.Abs(curVelocity) > 20f) DamageVehicle(damageCoefficientt * 0.8f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 15f) DamageVehicle(damageCoefficientt * 0.5f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 5f) DamageVehicle(damageCoefficientt * 0.3f * curVelocity);
    }
}
