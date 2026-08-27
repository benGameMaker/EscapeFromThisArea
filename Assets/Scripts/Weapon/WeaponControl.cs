using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponControl : MonoBehaviour
{
    public Transform FirePoint;
    public GameObject BulletPre;
    public GameObject FirePre;

    public float harmMultiplier = 1;
    public int capacity;
    public int bulletPoolDefaultCapacity = 10;
    public int bulletPoolMaxSize = 100;

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
        return Instantiate(BulletPre);
    }

    void OnBulletGet(GameObject bullet)
    {
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
