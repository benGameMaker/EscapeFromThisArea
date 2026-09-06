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
        // 刚接触：按"撞击"倍率立即结算一次，并重置该目标的节流计时
        TryInjure(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        // 持续接触：每hitInterval秒才对同一目标结算一次（撞击倍率减半，作为持续碾压伤害）
        TryInjure(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        // 目标离开后清除其计时，重新接触时能立刻再次结算
        BaseObject obj = other.gameObject.GetComponentInParent<BaseObject>();
        if (obj != null) nextHitTimes.Remove(obj);
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

        obj.Injured(harm);
        nextHitTimes[obj] = Time.time + hitInterval;
    }
}
