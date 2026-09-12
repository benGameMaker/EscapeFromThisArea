using System.Collections;
using UnityEngine;

public class MeleeRange : MonoBehaviour
{
    public float harmMulitpler = 1f;
    public GameObject BloodEffect;
    BaseObject executor;
    BoxCollider MeleeRangeBox;
    Coroutine activeCoroutine;
    // 本次攻击(一次TriggerMeleeAtk窗口)是否已结算过伤害。
    // 僵尸打玩家时正常；打车辆时该碰撞体会与车身多个Collider同时接触，
    // 一次窗口会连续触发多次OnTriggerEnter，此标记保证"一次攻击只结算一次伤害"
    bool strikeSettled;

    void Start()
    {
        executor = GetComponentInParent<BaseObject>();
        MeleeRangeBox = GetComponent<BoxCollider>();

        if (MeleeRangeBox != null)
        {
            MeleeRangeBox.enabled = false;
            MeleeRangeBox.isTrigger = true;
        }
    }

    /// <summary>
    /// 由攻击动画事件调用：开启本次攻击的伤害判定窗口。
    /// 一个窗口(本次攻击)内无论碰撞体与多少个Collider接触，只结算一次伤害。
    /// </summary>
    public void TriggerMeleeAtk(float time)
    {
        if (MeleeRangeBox == null) return;

        // 重新激活前先取消旧的失活协程，避免连续调用导致提前失活/窗口叠加
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
        activeCoroutine = null;
        strikeSettled = false;          // 新一次攻击：重置结算标记
        MeleeRangeBox.enabled = true;
        activeCoroutine = StartCoroutine(DisableAfterSeconds(time));
    }

    // 攻击者死亡/行为被打断时调用：立刻关闭近战判定并取消定时关闭协程
    public void CancelMelee()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        if (MeleeRangeBox != null)
            MeleeRangeBox.enabled = false;
        strikeSettled = false;
    }

    IEnumerator DisableAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);
        if (MeleeRangeBox != null)
            MeleeRangeBox.enabled = false;
        activeCoroutine = null;
        strikeSettled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(this.transform.parent.name + "MeleeRange OnTriggerEnter: " + other.transform.parent.name);
        // 本次攻击已结算过：忽略同一窗口内的后续触发（打车辆时车身多Collider会重复触发）
        if (strikeSettled) return;

        // 近战可以破坏车辆（玩家与僵尸的近战都适用），车辆没有BaseObject，单独走VehicleDamage
        VehicleDamage vehicle = other.GetComponentInParent<VehicleDamage>();
        if (vehicle != null)
        {
            if (executor == null) return;
            vehicle.DamageVehicle(executor.atk * harmMulitpler * 0.3f);
            EndCurrentStrike();   // 结算后立即关闭本次攻击窗口
            return;
        }

        BaseObject obj = other.GetComponentInParent<BaseObject>();
        // 打到自己/无BaseObject的目标时忽略（不消耗本次攻击）
        if (obj == null || obj == executor) return;
        // 同阵营(Tag相同)不误伤：不消耗本次攻击，同窗口仍可结算合法目标
        if (other.CompareTag(gameObject.tag)) return;
        // 僵尸不打自己人：攻击者与目标都是僵尸时忽略（僵尸各部位Tag不统一，
        // 单靠上面的CompareTag判不出，需按执行者类型过滤）
        if (executor is ZombieConrtol && obj is ZombieConrtol) return;

        if (BloodEffect != null)
        {
            GameObject blood = Instantiate(BloodEffect, transform.position, transform.rotation);
            Destroy(blood, 1f);
        }
        obj.Injured(executor, harmMulitpler);
        EndCurrentStrike();   // 结算后立即关闭本次攻击窗口
    }

    // 本次攻击已结算：标记后立即结束窗口，防止后续帧/后续Collider再触发
    void EndCurrentStrike()
    {
        strikeSettled = true;
        CancelMelee();
    }
}
