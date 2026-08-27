using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DetectionZombies : MonoBehaviour
{
    Dictionary<string, BaseObject> Zombies = new Dictionary<string, BaseObject>();

    void Start()
    {
        
    }

    void Update()
    {
        ScanForZombies();
    }

    Queue<string> deadZombies = new Queue<string>();
    /// <summary>
    /// 判断玩家是否进入僵尸的锥形视野内
    /// </summary>
    void ScanForZombies()
    {
        //僵尸的视野配置
        ZombieConeVisionConfig zombieConeVision;
        foreach (BaseObject zombie in Zombies.Values)
        {
            if (zombie.isDead)
            {
                deadZombies.Enqueue(zombie.gameObject.name);
                continue;
            }
            zombieConeVision = zombie.gameObject.GetComponent<ZombieConeVisionConfig>();
            if (zombieConeVision != null)
            {
                Debug.Log("开始检测" + zombie.name + "是否可见玩家");
                Transform zombiePos = zombieConeVision.DetectionPoint;
                //僵尸到玩家的方向
                Vector3 ZombieToPlayer = transform.position - zombiePos.position;
                //判断是否进入僵尸可视距离
                float distance = ZombieToPlayer.magnitude;
                if (distance > zombieConeVision.detectionRadius)
                {
                    Debug.Log("距离不够");
                    continue;
                }
                //判断是否进入僵尸视野角度（方向需归一化，与forward点积得到cos值）
                float dot = Vector3.Dot(zombiePos.forward, ZombieToPlayer.normalized);
                float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
                if (angle > zombieConeVision.detectionHalfAngle)
                {
                    Debug.Log("角度不对");
                    continue;
                }
                //让僵尸发出射线判断是否有遮挡，没有则将僵尸导航至玩家附近
                zombieConeVision.VisibilityDetection(transform);
                Debug.Log("玩家被" + zombie.name + "发现了");
            }
        }
        while(deadZombies.Count > 0)
        {
            string zombieName = deadZombies.Dequeue();
            if (Zombies.ContainsKey(zombieName))
            {
                Zombies.Remove(zombieName);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string zombiesName = other.name;
        BaseObject zombie = other.gameObject.GetComponent<BaseObject>();
        if (zombie != null && !zombie.isDead)
        {
            Zombies.Add(zombiesName, zombie);
            Debug.Log(zombiesName + "进入了玩家的球形粗筛范围内");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        string zombiesName = other.name;
        if (Zombies.ContainsKey(zombiesName))
        {
            Zombies.Remove(zombiesName);
            Debug.Log(zombiesName + "离开了玩家的球形粗筛范围内");
        }
    }
}
