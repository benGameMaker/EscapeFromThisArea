using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZombieInfo
{
    public int id;//僵尸ID
    public string res;//僵尸预制体在Resources文件夹的位置
    public float HP;//血量
    public float atk;//攻击力
    public float speed;//移动速度

}
