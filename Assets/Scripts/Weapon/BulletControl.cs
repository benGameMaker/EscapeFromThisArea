using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletControl : MonoBehaviour
{
    public float speed = 40f;
    public GameObject effectPrefab;
    public GameObject BloodPrefab;
    public float lifetime = 1f;

    Rigidbody rb;
    BaseObject owner;
    WeaponControl weaponControl;
    Coroutine lifetimeCoroutine;
    bool isInPool = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // 对象池获取时调用，初始化子弹状态
    public void Init(WeaponControl wc)
    {
        weaponControl = wc;
        owner = wc.owner;
        isInPool = false;
        //if (wc.owner == null) Debug.Log(wc.name + "这把武器没有主人");
        // 重置刚体速度，避免复用时残留上次的力
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 重新施加初速度
        rb.AddForce(transform.forward * speed, ForceMode.Impulse);

        // 重启生命周期计时
        if (lifetimeCoroutine != null)
            StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
    }

    IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        ReleaseToPool();
    }

    void ReleaseToPool()
    {
        // 防止双重释放（寿命到期与碰撞同时触发等场景）
        if (isInPool) return;
        isInPool = true;

        // 停止生命周期计时
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // 清理刚体速度
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 回收到对象池
        if (weaponControl != null)
            weaponControl.ReleaseBullet(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 已回收则忽略：同一物理步内子弹可能同时与僵尸多个子碰撞体产生回调
        if (isInPool) return;

        // 先取好碰撞信息（子弹随后会被回收/停用）。
        // 目标若在受击时死亡会立刻禁用其碰撞体，届时contacts可能为空，故这里做安全取值
        Vector3 hitPos = transform.position;
        Vector3 hitNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : transform.forward;
        bool isZombie = collision.gameObject.CompareTag("Zombie");
        BaseObject target = isZombie ? collision.gameObject.GetComponentInParent<BaseObject>() : null;
        string partTag = collision.collider != null ? collision.collider.gameObject.tag : "";

        // 先回收子弹：僵尸被打死时会立即禁用碰撞体，
        // 子弹若仍留在其体内会被物理求解器弹飞(表现为"打死僵尸的子弹不消失反而被弹飞")
        ReleaseToPool();

        if (isZombie)
        {
            Quaternion rotation = Quaternion.LookRotation(hitNormal) * Quaternion.Euler(0, -90, 0);
            Destroy(Instantiate(BloodPrefab, hitPos, rotation), 1f);
            Debug.Log("子弹击中了" + collision.gameObject.name + "的" + partTag);
            if (target != null && weaponControl != null)
                target.Injured(owner, (partTag == "Zombie_Body" ? weaponControl.harmMultiplier : weaponControl.harmMultiplier * 1.5f));
        }
        else
        {
            Destroy(Instantiate(effectPrefab, hitPos, Quaternion.LookRotation(hitNormal)), 1f);
            Debug.Log("子弹击中的" + collision.gameObject.name + "不是对象基类，他的类型是" + collision.gameObject.tag);
        }
    }
}
