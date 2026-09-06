using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在玩家身上的球形粗筛检测器。
/// 上车时由 VehicleInteraction 改挂到驾驶位(DropOffPos)并激活、下车时挂回玩家，
/// 从而保证徒步与车载状态下功能都可用。
/// 职责：
///  1. 用触发器维护"附近僵尸名单"（字典按僵尸BaseObject索引并缓存其锥形视野配置）；
///  2. 每帧对名单内僵尸做锥形视野检测（见 ZombieConeVisionConfig），被看见的僵尸开始追踪玩家；
///  3. 订阅当前实体（徒步=玩家、开车=车辆）下所有武器的开火事件：
///     开火瞬间把名单中距离开枪点不超过该武器 attractiveDistance 的僵尸，
///     导航到"那次开枪的位置"（开火瞬间的固定快照，而非玩家/车辆的实时位置）。
/// </summary>
public class DetectionZombies : MonoBehaviour
{
    /// <summary>附近僵尸名单：Key=僵尸根节点的BaseObject，Value=缓存的锥形视野配置（避免每帧GetComponent）</summary>
    readonly Dictionary<BaseObject, ZombieConeVisionConfig> Zombies =
        new Dictionary<BaseObject, ZombieConeVisionConfig>();

    // 已订阅开火事件的武器及它们所属的根节点。上下车后本物体被改挂，根节点变化时需重新订阅
    Transform boundRoot;
    readonly List<WeaponControl> boundWeapons = new List<WeaponControl>();
    readonly List<BaseObject> deadZombies = new List<BaseObject>();

    void Start()
    {
        RebindFireEvents();
    }

    void Update()
    {
        // 上下车时本物体会被改挂到玩家/车辆下，根节点变化后重新订阅该实体上的武器开火事件
        if (transform.root != boundRoot) RebindFireEvents();
        ScanForZombies();
    }

    /// <summary>取消旧订阅，改为订阅当前根节点下所有武器的开火事件</summary>
    void RebindFireEvents()
    {
        for (int i = 0; i < boundWeapons.Count; i++)
            if (boundWeapons[i] != null) boundWeapons[i].OnFired -= OnWeaponFired;
        boundWeapons.Clear();

        boundRoot = transform.root;
        if (boundRoot == null) return;
        boundWeapons.AddRange(boundRoot.GetComponentsInChildren<WeaponControl>(true));
        for (int i = 0; i < boundWeapons.Count; i++)
            if (boundWeapons[i] != null) boundWeapons[i].OnFired += OnWeaponFired;
    }

    /// <summary>
    /// 武器开火回调：把距离开枪点在 attractiveDistance 以内的僵尸导航到"该次开枪的位置"。
    /// 开枪位置取开火瞬间的固定快照（徒步=玩家根节点，开车=驾驶位 DropOffPos），
    /// 后续玩家/车辆移动不会再改变这些僵尸的目标点。
    /// </summary>
    void OnWeaponFired(WeaponControl weapon)
    {
        if (weapon == null || weapon.attractiveDistance <= 0f) return;

        // 该次开枪的位置：以本检测器挂载的父级（玩家根节点/驾驶位）为准，开火瞬间记录一次
        Transform shotRoot = transform.parent != null ? transform.parent : transform;
        Vector3 shotPoint = shotRoot.position;
        float sqrAttract = weapon.attractiveDistance * weapon.attractiveDistance;

        foreach (KeyValuePair<BaseObject, ZombieConeVisionConfig> kvp in Zombies)
        {
            BaseObject zombie = kvp.Key;
            if (zombie == null || zombie.isDead) continue;
            if ((zombie.transform.position - shotPoint).sqrMagnitude > sqrAttract) continue;

            ZombieConeVisionConfig zombieConeVision = kvp.Value;
            if (zombieConeVision != null) zombieConeVision.NavigationToTargetPoint(shotPoint);
        }
    }

    /// <summary>判断玩家是否进入僵尸的锥形视野内（被看见则僵尸开始追踪）</summary>
    void ScanForZombies()
    {
        deadZombies.Clear();
        foreach (KeyValuePair<BaseObject, ZombieConeVisionConfig> kvp in Zombies)
        {
            BaseObject zombie = kvp.Key;
            ZombieConeVisionConfig zombieConeVision = kvp.Value;
            if (zombie == null || zombie.isDead)
            {
                deadZombies.Add(zombie);
                continue;
            }
            if (zombieConeVision == null) continue;

            Transform zombiePos = zombieConeVision.DetectionPoint;
            if (zombiePos == null) continue;

            // 僵尸到玩家（检测点）的方向
            Vector3 ZombieToPlayer = transform.position - zombiePos.position;
            // 判断是否进入僵尸可视距离
            float distance = ZombieToPlayer.magnitude;
            if (distance > zombieConeVision.detectionRadius)
            {
                //Debug.Log(zombie.name + "离玩家太远了，距离：" + distance);
                continue;
            }
            // 判断是否进入僵尸视野角度（方向需归一化，与forward点积得到cos值）
            float dot = Vector3.Dot(zombiePos.forward, ZombieToPlayer.normalized);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            if (angle > zombieConeVision.detectionHalfAngle)
            {
                //Debug.Log(zombie.name + "没看见玩家，角度：" + angle);
                continue;
            }
            // 让僵尸发出射线判断是否有遮挡，没有则将僵尸导航至玩家附近
            zombieConeVision.VisibilityDetection(transform);
        }

        // 清理已死亡/已销毁的僵尸
        for (int i = 0; i < deadZombies.Count; i++)
        {
            if (deadZombies[i] != null)
                Zombies.Remove(deadZombies[i]);

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseObject zombie = other.GetComponentInParent<BaseObject>();
        if (zombie == null || zombie.isDead || Zombies.ContainsKey(zombie)) return;
        ZombieConeVisionConfig zombieConeVision = zombie.GetComponent<ZombieConeVisionConfig>();
        if (zombieConeVision == null) return; // 只有能被视野检测并导航的僵尸才纳入名单
        //Debug.Log("检测到僵尸进入范围：" + zombie.name);

        Zombies.Add(zombie, zombieConeVision);
    }

    private void OnTriggerExit(Collider other)
    {
        BaseObject zombie = other.GetComponentInParent<BaseObject>();
        if (zombie != null && Zombies.ContainsKey(zombie))
        {
            Zombies.Remove(zombie);
        }
    }
}
