using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletItem : BaseItemObjData
{
    public override void UseBahaivor(int Integer)
    {
        // 行为在"格子内随格子存活的行为组件"上执行，只能读取拾取时拷贝的数据(itemName等)
        // 或单例/管理器，不能访问已被销毁的原场景物体
        Debug.Log("这是" + itemName);
        PlayerController pc = SceneManager.Instance.player;
        if(pc != null )Debug.Log("获取到玩家控制器：" + pc.name);
        else Debug.Log("未获取到玩家控制器");
    }
}
