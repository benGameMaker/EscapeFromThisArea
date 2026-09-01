using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObjData : MonoBehaviour
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

}
