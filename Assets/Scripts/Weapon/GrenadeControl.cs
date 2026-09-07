using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeControl : MonoBehaviour
{
    public float maxHarm = 120f;
    public float minHarm = 30f;
    //等待时间后爆炸
    public float waitTime = 6f;
    //可以穿透障碍物进行伤害
    public bool isIgnoreObstacle = false;

    public AudioClip audio;
    SphereCollider sc;
    //同一物体在爆炸瞬间只结算一次伤害，避免多碰撞体重复扣血
    HashSet<BaseObject> damagedObjects = new HashSet<BaseObject>();

    private void Start()
    {
        sc = GetComponent<SphereCollider>();
        sc.enabled = false;
    }

    // 拉环：等待 waitTime 秒后爆炸
    public void TriggerFuse()
    {
        StartCoroutine(ExplodeAfterWait());
    }

    IEnumerator ExplodeAfterWait()
    {
        yield return new WaitForSeconds(waitTime);
        Explode();
    }

    void Explode()
    {
        GameDataMgr.Instance.PlaySound(audio);
        GameObject explodeEff = Instantiate(Resources.Load<GameObject>("Prefabs/GrenadeExplode"), transform.position, Quaternion.identity);
        sc.enabled = true;
        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseObject target = other.GetComponentInParent<BaseObject>();
        Debug.Log(other.name+"在范围内");
        if (target == null) return;
        if (!damagedObjects.Add(target)) return;

        // 最大伤害距离为球形碰撞体的半径：中心处伤害 maxHarm，球面边缘处伤害 minHarm
        float dist = Vector3.Distance(transform.position, other.ClosestPoint(transform.position));
        float maxRange = sc.bounds.extents.x;
        float t = Mathf.Clamp01(dist / maxRange);
        float harm = Mathf.Lerp(maxHarm, minHarm, t);
        Debug.Log("对" + target.gameObject.name + "造成伤害：" + harm);
        target.Injured(harm);
    }
}
