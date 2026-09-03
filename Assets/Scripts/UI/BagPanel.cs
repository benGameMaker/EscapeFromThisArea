using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BagPanel : BasePanel
{
    public Button btnExit;
    public Transform Cells;
    public Transform iip;   //物品操作面板（BagPanel预制体中的嵌套子物体，右键物品时显示）

    [HideInInspector]
    public PlayerController pc;
    [HideInInspector]
    public VehicleController vc;
    [HideInInspector]
    public List<Transform> itemCells;
    //记录每种物品（按名称区分）所占据的格子中的最大下标
    public Dictionary<string, int> itemCellMaxIndex = new Dictionary<string, int>();
    //记录每种物品（按名称区分）在背包中总数量
    public Dictionary<string, int> itemsNum = new Dictionary<string, int>();
    public override void Init()
    {
        if (SceneManager.Instance != null) pc = SceneManager.Instance.player;
        btnExit.onClick.AddListener(() =>
        {
            HideMe(() => { });
            Cursor.lockState = CursorLockMode.Locked;
            pc.enableMouseInput(true);
            if (vc != null) vc.enableMouseInput(true);
            Cursor.visible = false;
        });
        CollectCells();
        //ItemDataLoadCell();
        //加载完数据后刷新物品占格最大下标记录
        RefreshItemCellMaxIndexAndQuantity();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
    public void AddItem(BaseItemObjData item)
    {
        if (item == null) return;

        //剩余待分配数量
        int remaining = item.curNum;

        //第一步：优先将数量叠加到名称、图标都相同的格子中，按下标从小到大
        for (int i = 0; i < itemCells.Count && remaining > 0; i++)
        {
            ItemUnit cellItem = GetItemInCell(i);
            //空格子跳过
            if (cellItem == null) continue;
            //名称和图标必须都相同
            if (cellItem.itemName.text != item.itemName) continue;
            if (cellItem.itemIcon.sprite != item.itemIcon) continue;

            int.TryParse(cellItem.curNum.text, out int cur);
            //该格子还能再放下的数量
            int capacity = item.maxStackNum - cur;
            if (capacity <= 0) continue;

            int add = Mathf.Min(remaining, capacity);
            cellItem.curNum.text = (cur + add).ToString();
            //同步最大堆叠数量，保证存档数据正确
            cellItem.maxStackNum = item.maxStackNum;
            //若该格子尚未挂载真实的使用行为(例如由存档JSON创建的占位单元)，补挂当前拾取物的行为
            if (cellItem.GetComponent<BaseItemObjData>() == null)
            {
                cellItem.UseBehavior = null;
                AttachItemBehavior(cellItem, item);
            }

            remaining -= add;
        }



        
        //第二步：还有剩余数量，放到下标最小的空格子中
        if (remaining > 0)
        {
            int emptyIndex = FindFirstEmptyCellIndex();
            if (emptyIndex >= 0)
            {
                ItemUnit newUnit = CreateItemUnit(itemCells[emptyIndex], item.itemName, item.itemIcon,
                    remaining, item.maxStackNum, item.useBehaviorNumber, emptyIndex, null);
                AttachItemBehavior(newUnit, item);
                remaining = 0;
            }
        }
        //暂不处理格子不足、物品无法全部放完的情况
        //布局发生改变，刷新物品占格最大下标记录
        RefreshItemCellMaxIndexAndQuantity();
    }
    public bool ConsumeItem(string itemName, int num)
    {
        //消耗数量小于等于0视为成功
        if (num <= 0) return true;
        if (string.IsNullOrEmpty(itemName)) return false;

        //剩余需要消耗的数量
        int remaining = num;

        //优先从该物品占据的最大下标格子开始消耗（字典缺失时兜底从最后一个格子开始扫描）
        int startIndex = itemCellMaxIndex.TryGetValue(itemName, out int maxIndex) ? maxIndex : itemCells.Count - 1;
        for (int i = startIndex; i >= 0 && remaining > 0; i--)
        {
            ItemUnit cellItem = GetItemInCell(i);
            //空格子或不是同一种物品则跳过
            if (cellItem == null) continue;
            if (cellItem.itemName.text != itemName) continue;

            int.TryParse(cellItem.curNum.text, out int cur);
            if (cur <= 0)
            {
                //数量为0的格子直接销毁物品单元
                Destroy(cellItem.gameObject);
                continue;
            }

            //本次从该格子消耗的数量
            int consume = Mathf.Min(remaining, cur);
            remaining -= consume;
            int left = cur - consume;
            if (left > 0)
            {
                //该格子还有剩余数量，更新显示
                cellItem.curNum.text = left.ToString();
            }
            else
            {
                //该格子的物品被消耗完，销毁格子中的物品单元
                Destroy(cellItem.gameObject);
            }
        }

        //布局可能发生改变，刷新物品占格最大下标记录
        RefreshItemCellMaxIndexAndQuantity();

        //能完全消耗掉指定数量才算成功
        return remaining == 0;
    }
    //重新计算每种物品所占据格子的最大下标，在添加、消耗物品以及布局改变后调用，同时计算每个物品的总数量
    public void RefreshItemCellMaxIndexAndQuantity()
    {
        itemCellMaxIndex.Clear();
        itemsNum.Clear();
        for (int i = 0; i < itemCells.Count; i++)
        {
            ItemUnit cellItem = GetItemInCell(i);
            if (cellItem == null) continue;
            string name = cellItem.itemName.text;
            itemsNum[name] = itemsNum.GetValueOrDefault(name) + int.Parse(cellItem.curNum.text);
            if (string.IsNullOrEmpty(name)) continue;
            //升序遍历，后写入的即为该物品的最大下标
            itemCellMaxIndex[name] = i;
        }
        Debug.Log("当前每个物品的总数量为：");
        foreach (var kvp in itemsNum)
        {
            Debug.Log(kvp.Key + ": " + kvp.Value);
        }
    }
    //将拖拽的物品堆叠到目标格子：能装下则数量全部并入并销毁被拖物品单元；
    //装不下则目标格子装到上限，剩余数量保留在被拖物品单元中，并把它放入下标最小的空格子（无空格子则回到原格子）
    public void StackItemToCell(ItemUnit draggedItem, Transform targetCell, Transform fallbackCell)
    {
        if (draggedItem == null || targetCell == null) return;
        int targetIndex = itemCells.IndexOf(targetCell);
        if (targetIndex < 0) return;
        ItemUnit targetItem = GetItemInCell(targetIndex);
        if (targetItem == null) return;

        //最大堆叠数量取两者中较大值，并统一更新
        int newMax = Mathf.Max(draggedItem.maxStackNum, targetItem.maxStackNum);
        draggedItem.maxStackNum = newMax;
        targetItem.maxStackNum = newMax;

        int.TryParse(draggedItem.curNum.text, out int draggedNum);
        int.TryParse(targetItem.curNum.text, out int targetNum);
        //目标格子还能容纳的数量
        int capacity = newMax - targetNum;

        if (draggedNum <= capacity)
        {
            //目标格子能装下全部数量，并入后销毁被拖物品单元
            targetItem.curNum.text = (targetNum + draggedNum).ToString();
            Destroy(draggedItem.gameObject);
        }
        else
        {
            //目标格子装到上限，剩余数量保留在被拖物品单元中
            targetItem.curNum.text = newMax.ToString();
            draggedItem.curNum.text = (draggedNum - capacity).ToString();
            //把保留剩余数量的物品单元放入下标最小的空格子，没有则回到原格子
            int emptyIndex = FindFirstEmptyCellIndex();
            MoveItemToCell(draggedItem, emptyIndex >= 0 ? itemCells[emptyIndex] : fallbackCell);
        }

        //布局可能变化，刷新物品占格最大下标记录
        RefreshItemCellMaxIndexAndQuantity();
    }
    //获取指定下标格子中的物品，没有则返回null
    ItemUnit GetItemInCell(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= itemCells.Count) return null;
        for (int j = 0; j < itemCells[cellIndex].childCount; j++)
        {
            ItemUnit itemUnit = itemCells[cellIndex].GetChild(j).GetComponent<ItemUnit>();
            if (itemUnit != null) return itemUnit;
        }
        return null;
    }
    //创建新的物品单元并放入指定格子中，填充数据后返回
    ItemUnit CreateItemUnit(Transform cell, string itemName, Sprite itemIcon, int curNum, int maxStackNum, int useBehaviorNumber, int cellIndex, UnityAction action)
    {
        //Debug.Log("创建物品单元：" + itemName + "，数量：" + curNum + "，格子下标：" + cellIndex);
        GameObject itemUnitObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/ItemUnit"));
        itemUnitObj.transform.SetParent(cell, false);
        ItemUnit itemUnit = itemUnitObj.GetComponent<ItemUnit>();
        itemUnit.itemIcon.sprite = itemIcon;
        itemUnit.itemName.text = itemName;
        itemUnit.curNum.text = curNum.ToString();
        itemUnit.maxStackNum = maxStackNum;
        itemUnit.useBehaviorNumber = useBehaviorNumber;
        itemUnit.cellIndex = cellIndex;
        itemUnit.UseBehavior += action;
        itemUnit.iip = iip;
        return itemUnit;
    }
    //在格子内的物品单元上挂载"使用行为"：
    //场景拾取物(参数item所在的GameObject)在拾取后会被销毁，若直接把 item.UseBahaivor
    //存进格子的委托，点击"使用"时组件已销毁，会抛 MissingReferenceException。
    //因此这里给随格子存活的物品单元动态添加同类型的 BaseItemObjData 组件并拷贝数据，
    //让委托指向它，使用行为不再依赖已销毁的场景实例。
    void AttachItemBehavior(ItemUnit itemUnit, BaseItemObjData item)
    {
        if (itemUnit == null || item == null) return;
        //已挂载过(例如同一格子曾绑定过)则不再重复挂载
        if (itemUnit.GetComponent<BaseItemObjData>() != null) return;

        BaseItemObjData behavior = itemUnit.gameObject.AddComponent(item.GetType()) as BaseItemObjData;
        if (behavior == null)
        {
            Debug.LogWarning(item.name + " 的类型无法作为BaseItemObjData挂载：" + item.GetType());
            return;
        }
        behavior.CopyDataFrom(item);
        itemUnit.UseBehavior += behavior.UseBahaivor;
    }
    //找到下标最小的空格子的下标，没有则返回-1
    int FindFirstEmptyCellIndex()
    {
        for (int i = 0; i < itemCells.Count; i++)
        {
            if (GetItemInCell(i) == null) return i;
        }
        return -1;
    }
    //把已有的物品单元移动到指定格子，并同步格子下标、刷新布局
    void MoveItemToCell(ItemUnit itemUnit, Transform cell)
    {
        if (itemUnit == null || cell == null) return;
        itemUnit.transform.SetParent(cell, false);
        itemUnit.cellIndex = itemCells.IndexOf(cell);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cell as RectTransform);
    }
    void ItemDataLoadCell()
    {
        foreach (CellData cell in GameDataMgr.Instance.cellDatas)
        {
            if (cell.cellIndex >= itemCells.Count)
            {
                Debug.LogError(cell.itemName + "的单元格索引超出范围: " + cell.cellIndex);
                continue;
            }
            CreateItemUnit(itemCells[cell.cellIndex], cell.itemName,
                Resources.Load<Sprite>("Sprites/" + cell.itemIconName), cell.curNum,
                cell.maxStackNum, cell.useBehaviorNumber, cell.cellIndex, () => { Debug.Log("Json导入的物品暂时无行为函数"); });
        }
    }
    void SaveItemDatasToCellDatas()
    {
        //清空缓存数据
        GameDataMgr.Instance.cellDatas.Clear();

        //遍历每一个物品格子
        for (int i = 0; i < itemCells.Count; i++)
        {
            //遍历该格子的所有直接子物体
            for (int j = 0; j < itemCells[i].childCount; j++)
            {
                Transform child = itemCells[i].GetChild(j);
                ItemUnit itemUnit = child.GetComponent<ItemUnit>();
                //只处理挂载了ItemUnit组件的直接子物体
                if (itemUnit == null) continue;

                //将ItemUnit的信息一一对应赋值到CellData中
                CellData cellData = new CellData();
                cellData.itemIconName = itemUnit.itemIcon.sprite.name;
                cellData.itemName = itemUnit.itemName.text;
                cellData.curNum = int.Parse(itemUnit.curNum.text);
                cellData.maxStackNum = itemUnit.maxStackNum;
                cellData.useBehaviorNumber = itemUnit.useBehaviorNumber;
                cellData.cellIndex = itemUnit.cellIndex;
                GameDataMgr.Instance.cellDatas.Add(cellData);
            }
        }

        //保存到Json文档
        GameDataMgr.Instance.SaveCellData();
    }
    void CollectCells()
    {
        for(int i = 0; i < Cells.childCount; i++)
        {
            itemCells.Add(Cells.GetChild(i));
        }
    }
    public override void HideMe(UnityAction callback)
    {
        base.HideMe(callback);
        //关闭背包时同步隐藏物品操作面板
        if (iip != null)
        {
            ItemInteractionPanel panel = iip.GetComponent<ItemInteractionPanel>();
            if (panel != null) panel.HideMe(() => { });
        }
        //SaveItemDatasToCellDatas();
    }
}
