using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class WeaponControl : MonoBehaviour
{
    public Transform FirePoint;
    public GameObject BulletPre;
    public GameObject FirePre;
    [Tooltip("用于车上使用武器时的武器射速")]
    public float fireRate = 100f;
    public float ReloadTime;
    public float harmMultiplier = 1;
    public int capacity;
    public string bulletTypeName;
    public AudioClip gunShot;
    public AudioClip reloadSound;
    public int bulletPoolDefaultCapacity = 10;
    public int bulletPoolMaxSize = 100;
    [Tooltip("使用这把武器开枪时会吸引僵尸的最远距离")]
    public float attractiveDistance;
    [Tooltip("在启用子弹忽略层级，这把武器打出的子弹不会命中的层级")]
    public LayerMask ignorelLayer;
    public bool enableExclusionLayer;
    public Sprite WeaponIcon;
    public Sprite BulletIcon;

    LayerMask originalExcludeLayer;

    public int curCapacity;
    [HideInInspector]
    public BaseObject owner;
    [Tooltip("当前是否处于换弹中（用于阻止重复触发/换弹中开火等打断行为）")]
    [HideInInspector]
    public bool isReloading = false;

    public event UnityAction<int> OnCapacityChanged;
    public event UnityAction<float> OnReloadStart;
    [Tooltip("开火事件：每实际打出一发子弹触发一次（参数为该武器），供枪声吸引僵尸等系统监听")]
    public event UnityAction<WeaponControl> OnFired;
    BagPanel bp;
    // 子弹对象池
    ObjectPool <GameObject> bulletPool;
    private void Awake()
    {
        InitCapacity();
    }

    // 初始化弹匣。物体初始 inactive 时 Awake 不会执行，需由外部（如WeaponSwitching）主动调用
    public void InitCapacity()
    {
        //curCapacity = capacity;
        bp = UIManager.Instance.GetPanel<BagPanel>();
    }
    void Start()
    {
        FirePoint = transform.Find("FirePoint");
        owner = GetComponentInParent<BaseObject>();

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
            GameDataMgr.Instance.PlaySound(gunShot);
            curCapacity--;
            if(curCapacity < 0) { curCapacity = 0; }
            // 广播弹药数量变化，由订阅的面板（PlayerPanel/VehiclePanel等）刷新显示
            OnCapacityChanged?.Invoke(curCapacity);
            // 从对象池获取子弹
            bulletPool.Get();
            // 生成火焰特效
            Destroy(Instantiate(FirePre, FirePoint.transform.position, FirePoint.transform.rotation), 0.1f);
            // 广播该次开火（携带武器引用），供DetectionZombies根据attractiveDistance把附近僵尸吸引到开枪位置
            OnFired?.Invoke(this);
        }

    }
    //换弹：按 弹匣最大容量capacity - 弹匣现有数量curCapacity 的差值从背包补充对应子弹
    public void Reload()
    {
        // 换弹完成（或被打断后恢复数据）：复位换弹状态，之后开火/换弹输入才重新生效
        isReloading = false;

        if (bp == null) bp = UIManager.Instance.GetPanel<BagPanel>();
        if (bp == null) return;

        //本次需要从背包补充的数量
        int need = capacity - curCapacity;
        if (need > 0)
        {
            //背包中该种子弹的剩余数量
            int bagAmmo = bp.itemsNum.GetValueOrDefault(bulletTypeName);
            //实际补充数量不超过背包余量
            int take = Mathf.Min(need, bagAmmo);
            if (take > 0)
            {
                curCapacity += take;
                bp.ConsumeItem(bulletTypeName, take);
            }
        }

        //通知UI刷新弹匣数量（面板会同时重新读取背包余量刷新MaxCapacity）
        OnCapacityChanged?.Invoke(curCapacity);
    }

    // 开始换弹：广播换弹开始事件（携带ReloadTime），由外部（玩家/车辆）在换弹行为发生时调用
    public void StartReload()
    {
        // 已在换弹中：忽略重复触发（再按换弹键/空仓开火键不得重置换弹进度，也不得打断换弹）
        if (isReloading) return;
        isReloading = true;

        Debug.Log("WeaponControl: StartReload");
        GameDataMgr.Instance.PlaySound(reloadSound);
        OnReloadStart?.Invoke(ReloadTime);
    }

    // 中断换弹（目前仅"切换武器"允许）：复位换弹状态，UI进度由调用方(面板/控制器)一并清除
    public void CancelReload()
    {
        isReloading = false;
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
