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

    public PlayerController pc;
    public List<Transform> itemCells;
    //记录每种物品（按名称区分）所占据的格子中的最大下标
    public Dictionary<string, int> itemCellMaxIndex = new Dictionary<string, int>();
    public override void Init()
    {
        pc = SceneManager.Instance.player;
        btnExit.onClick.AddListener(() =>
        {
            HideMe(() => { });
            Cursor.lockState = CursorLockMode.Locked;
            pc.enableMouseInput(true);
            Cursor.visible = false;
        });
        CollectCells();
        //ItemDataLoadCell();
        //加载完数据后刷新物品占格最大下标记录
        RefreshItemCellMaxIndex();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
    public void AddItem(ItemObjData item)
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

            remaining -= add;
        }



        
        //第二步：还有剩余数量，放到下标最小的空格子中
        if (remaining > 0)
        {
            int emptyIndex = FindFirstEmptyCellIndex();
            if (emptyIndex >= 0)
            {
                CreateItemUnit(itemCells[emptyIndex], item.itemName, item.itemIcon,
                    remaining, item.maxStackNum, item.useBehaviorNumber, emptyIndex,
                    item.UseBahaivor);
                remaining = 0;
                break;
            }
        }
        //暂不处理格子不足、物品无法全部放完的情况
        //布局发生改变，刷新物品占格最大下标记录
        RefreshItemCellMaxIndex();
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
        RefreshItemCellMaxIndex();

        //能完全消耗掉指定数量才算成功
        return remaining == 0;
    }
    //重新计算每种物品所占据格子的最大下标，在添加、消耗物品以及布局改变后调用
    public void RefreshItemCellMaxIndex()
    {
        itemCellMaxIndex.Clear();
        for (int i = 0; i < itemCells.Count; i++)
        {
            ItemUnit cellItem = GetItemInCell(i);
            if (cellItem == null) continue;
            string name = cellItem.itemName.text;
            if (string.IsNullOrEmpty(name)) continue;
            //升序遍历，后写入的即为该物品的最大下标
            itemCellMaxIndex[name] = i;
        }
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
        GameObject itemUnitObj;
        ItemUnit itemUnit;
        foreach (CellData cell in GameDataMgr.Instance.cellDatas)
        {
            itemUnitObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/ItemUnit"));
            if (cell.cellIndex < itemCells.Count)
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
        //SaveItemDatasToCellDatas();
    }
}
