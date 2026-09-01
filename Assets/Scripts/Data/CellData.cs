using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellData : MonoBehaviour
{
    //背包格子中的物品图标
    public string itemIconName;
    //背包格子中的物品名字
    public string itemName;
    //背包格子中的当前物品数量
    public int curNum;
    //背包格子中的该物品的最大堆叠数量
    public int maxStackNum;
    //在背包使用它时，所要执行的行为函数编号
    public int useBehaviorNumber;
    //该物品在背包中的格子下标
    public int cellIndex;
}
