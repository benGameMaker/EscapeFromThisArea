using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeRange : MonoBehaviour
{
    public float harmMulitpler = 1f;
    public GameObject BloodEffect;
    BaseObject executor;
    BoxCollider MeleeRangeBox;
    Coroutine activeCoroutine;

    // Start is called before the first frame update
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

    // Update is called once per frame
    void Update()
    {

    }

    public void TriggerMeleeAtk(float time)
    {
        if (MeleeRangeBox == null) return;

        // 重新激活前先取消旧的失活协程，避免连续调用导致提前失活
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
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
    }

    IEnumerator DisableAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);
        if (MeleeRangeBox != null)
            MeleeRangeBox.enabled = false;
        activeCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 近战可以破坏车辆（玩家与僵尸的近战都适用），车辆没有BaseObject，单独走VehicleDamage
        VehicleDamage vehicle = other.GetComponentInParent<VehicleDamage>();
        if (vehicle != null)
        {
            if (executor == null) return;
            Debug.Log(other.name + "车辆被" + executor.gameObject.name + "近战攻击");
            vehicle.DamageVehicle(executor.atk * harmMulitpler * 0.3f);
            return;
        }

        BaseObject obj = other.GetComponentInParent<BaseObject>();
        Debug.Log("攻击到" + (other.transform.parent==null ? other.name : other.transform.parent.name));
        // 打到自己/同阵营/无BaseObject的目标时忽略
        if (obj == null || obj == executor || other.CompareTag(gameObject.tag))
        {
            Debug.Log("刀到了" + gameObject.tag + "||" + gameObject.name);
            return;
        }
        if(BloodEffect != null)
        {
            GameObject blood = Instantiate(BloodEffect, transform.position, transform.rotation);
            Destroy(blood, 1f);
        }
        Debug.Log(other.gameObject.transform.parent.name + "被" + gameObject.transform.parent.name + "攻击了");
        obj.Injured(executor, harmMulitpler);
    }
}
