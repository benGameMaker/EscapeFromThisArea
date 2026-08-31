using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletControl : MonoBehaviour
{
    public float speed = 40f;
    public GameObject effectPrefab;
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
        var go = Instantiate(effectPrefab, transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
        Destroy(go, 1f);
        if (owner == null) Debug.Log("owner为空");
        if (collision.gameObject.CompareTag("Zombie"))
        {
            string part = collision.collider.gameObject.tag;
            Debug.Log("子弹击中了" + collision.gameObject.name + "的" + part);
            collision.gameObject.GetComponentInParent<BaseObject>().Injured(owner, (part == "Zombie_Body" ? weaponControl.harmMultiplier : weaponControl.harmMultiplier * 1.5f));
        }
        else
        {
            Debug.Log("子弹击中的" + collision.gameObject.name+"不是对象基类，他的类型是"+collision.gameObject.tag);

        }
        // 碰撞后回收子弹到对象池
        ReleaseToPool();
    }
}
