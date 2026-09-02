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
    public abstract void UseBahaivor();

    // 拾取时把场景拾取物的数据拷贝到"背包格子内存活的行为组件"上。
    // 场景拾取物在进入背包后会被销毁，若行为委托仍指向它，点击使用时必然
    // 抛 MissingReferenceException。因此背包在创建格子单元时，会用本方法把
    // 数据迁到一个随格子存活的同类型组件上，行为执行时不再依赖场景实例。
    // 注意：使用行为内部只能读取这些已拷贝的数据或单例/管理器，
    // 不能再访问原场景物体的 Unity 成员。
    public virtual void CopyDataFrom(BaseItemObjData other)
    {
        itemIcon = other.itemIcon;
        itemName = other.itemName;
        curNum = other.curNum;
        maxStackNum = other.maxStackNum;
        useBehaviorNumber = other.useBehaviorNumber;
    }
}
