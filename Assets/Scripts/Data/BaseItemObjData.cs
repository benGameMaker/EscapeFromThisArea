using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseItemObjData : MonoBehaviour
{
    //物品图标
    public Sprite itemIcon;
    //物品名字
    public string itemName;
    //当前物品数量
    public int curNum;
    //在背包格子中的该物品的最大堆叠数量
    public int maxStackNum;
    //在背包使用它时，所要执行的行为函数编号
    public int useBehaviorNumber;

    // 在背包中使用时它要执行的行为方法，在子类中实现
    public abstract void UseBahaivor(int Integer = 0);

    // 数据解耦，与场景中的原物品断开关系
    public virtual void CopyDataFrom(BaseItemObjData other)
    {
        itemIcon = other.itemIcon;
        itemName = other.itemName;
        curNum = other.curNum;
        maxStackNum = other.maxStackNum;
        useBehaviorNumber = other.useBehaviorNumber;
    }
}
