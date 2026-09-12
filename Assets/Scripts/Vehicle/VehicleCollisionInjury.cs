using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleCollisionInjury : MonoBehaviour
{
    [Tooltip("是否只在车辆正向行驶时才造成伤害，若为false则车辆反向行驶时也会造成伤害")]
    public bool PositiveVelocityOnly = true;
    [Tooltip("持续碾压(OnTriggerStay)时，对同一目标两次结算伤害的最小间隔(秒)")]
    public float hitInterval = 0.5f;

    VehicleDamage vd;
    // 记录每个目标下一次允许被结算伤害的时间，避免OnTriggerStay每物理帧都重复扣血
    readonly Dictionary<BaseObject, float> nextHitTimes = new Dictionary<BaseObject, float>();

    void Start()
    {
        vd = GetComponentInParent<VehicleDamage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!other.CompareTag("Zombie")) return; 
        // 刚接触：按"撞击"倍率立即结算一次（是否真的结算由TryInjure里的节流决定）
        //Debug.Log("车辆碰撞到" + other.name);
        TryInjure(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        // 持续接触：每hitInterval秒才对同一目标结算一次（撞击倍率减半，作为持续碾压伤害）
        TryInjure(other, false);
    }

    private void Update()
    {
        PruneTimers();
    }

    // 简单粗暴的处理：不依赖OnTriggerExit清计时。
    // 碰撞体抖动会让Enter/Exit在同一物理帧反复交替，若一Exit就Remove计时，
    // 每次重新Enter都能立刻结算伤害，节流形同虚设。
    // 这里只定期清掉过期/已销毁目标的记录，让持续接触严格按hitInterval结算一次。
    void PruneTimers()
    {
        if (nextHitTimes.Count == 0) return;
        foreach (BaseObject key in new List<BaseObject>(nextHitTimes.Keys))
        {
            if (key == null || Time.time >= nextHitTimes[key]) nextHitTimes.Remove(key);
        }
    }

    void TryInjure(Collider other, bool entering)
    {
        if (vd == null) vd = GetComponentInParent<VehicleDamage>();

        // 僵尸/玩家等角色的碰撞体往往挂在子骨骼上（如Head/Hips），需向父级找BaseObject
        BaseObject obj = other.gameObject.GetComponentInParent<BaseObject>();
        if (obj == null || vd == null || other.name == "Head") return;

        // 节流：同一目标在hitInterval内只结算一次（进入也算一次，避免Enter+Stay同帧双倍结算）
        if (nextHitTimes.TryGetValue(obj, out float nextTime) && Time.time < nextTime) return;

        bool canDamage = PositiveVelocityOnly ? vd.curVelocity > 0f : vd.curVelocity < 0f;
        if (!canDamage) return;

        float speed = Mathf.Abs(vd.curVelocity);
        float harm;
        if (speed > 20f) harm = entering ? 3.2f * speed : 1.6f * speed;
        else if (speed > 15f) harm = entering ? 2f * speed : 1f * speed;
        else if (speed > 5f) harm = entering ? 1.2f * speed : 0.6f * speed;
        else return;
        Debug.Log("车辆对" + obj.name + "造成了伤害，速度为：" + speed);
        obj.Injured(Mathf.Abs(harm));
        nextHitTimes[obj] = Time.time + hitInterval;
    }
}
