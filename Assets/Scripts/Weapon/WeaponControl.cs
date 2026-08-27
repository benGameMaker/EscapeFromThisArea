using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponControl : MonoBehaviour
{
    public Transform FirePoint;
    public GameObject BulletPre;
    public GameObject FirePre;
    [Tooltip("用于车上使用武器时的武器射速")]
    public float fireRate = 100f;

    public float harmMultiplier = 1;
    public int capacity;
    public int bulletPoolDefaultCapacity = 10;
    public int bulletPoolMaxSize = 100;
    [Tooltip("在启用子弹忽略层级，这把武器打出的子弹不会命中的层级")]
    public LayerMask ignorelLayer;
    public bool enableExclusionLayer;
    LayerMask originalExcludeLayer;

    [HideInInspector]
    public int curCapacity;
    [HideInInspector]
    public BaseObject owner;


    // 子弹对象池
    ObjectPool <GameObject> bulletPool;

    void Start()
    {
        FirePoint = transform.Find("FirePoint");
        owner = GetComponentInParent<BaseObject>();
        curCapacity = capacity;

        bulletPool = new ObjectPool<GameObject>(
            CreateBullet,
            OnBulletGet,
            OnBulletRelease,
            OnDestroyBullet,
            collectionCheck: true,
            bulletPoolDefaultCapacity,
            bulletPoolMaxSize
        );

    }

    //通过函数设置武器归属者（上车时由VehicleController调用传入玩家，用于子弹伤害归属）
    public void SetOwner(BaseObject newOwner)
    {
        //if(newOwner!=null)Debug.Log(name + "获取到了" + newOwner + "的信息");
        //else Debug.Log(name + "获取到了" + newOwner + "的信息-------失败！！！！！！！！");
        owner = newOwner;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Fire()
    {
        if (curCapacity > 0)
        {
            curCapacity--;
            if(curCapacity < 0) { curCapacity = 0; }
            // 从对象池获取子弹
            bulletPool.Get();
            // 生成火焰特效
            Destroy(Instantiate(FirePre, FirePoint.transform.position, FirePoint.transform.rotation), 0.1f);
        }

    }


    GameObject CreateBullet()
    {
        if (bulletPool.CountAll <= 0)
        {
            GameObject bullet = Instantiate(BulletPre);
            originalExcludeLayer = bullet.GetComponent<Rigidbody>().excludeLayers;
            return bullet;
        }
        else
        {
            return Instantiate(BulletPre);
        }
    }

    void OnBulletGet(GameObject bullet)
    {
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (enableExclusionLayer) bulletRb.excludeLayers = ignorelLayer;
        else bulletRb.excludeLayers = originalExcludeLayer;
        bullet.transform.SetPositionAndRotation(FirePoint.position, FirePoint.rotation);
        bullet.SetActive(true);
        var bc = bullet.GetComponent<BulletControl>();
        if (bc != null) bc.Init(this);
    }

    void OnBulletRelease(GameObject bullet)
    {
        bullet.SetActive(false);
    }

    void OnDestroyBullet(GameObject bullet)
    {
        Destroy(bullet);
    }

    // 供 BulletControl 回收子弹到对象池
    public void ReleaseBullet(GameObject bullet)
    {
        bulletPool.Release(bullet);
    }
}
