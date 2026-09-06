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
    public BagLogicProcess bagOpener;   //记录由哪个可操作物体(BagLogicProcess)打开了背包，关闭按钮据此恢复输入
    [HideInInspector]
    public List<Transform> itemCells;
    //记录每种物品（按名称区分）所占据的格子中的最大下标
    public Dictionary<string, int> itemCellMaxIndex = new Dictionary<string, int>();
    //记录每种物品（按名称区分）在背包中总数量
    public Dictionary<string, int> itemsNum = new Dictionary<string, int>();
    //背包中物品数据(数量/分布)重新计算后触发，供HUD(PlayerPanel/VehiclePanel等)实时刷新显示
    public event UnityAction OnBagItemsChanged;
    [Tooltip("丢弃物品时施加的初速度大小")]
    public float throwSpeed = 8f;
    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            //关闭背包并恢复打开者的输入，由记录在bagOpener中的BagLogicProcess处理；未记录时兜底直接隐藏
            if (bagOpener != null)
            {
                bagOpener.CloseBag();
            }
            else
            {
                HideMe(() => { });
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
            //若该格原本没有记录预制体名(例如从存档生成)，补上当前拾取物的预制体名，保证丢弃时可生成场景物品
            if (string.IsNullOrEmpty(cellItem.itemPrefabsName))
                cellItem.itemPrefabsName = item.itemPrefabsName;

            remaining -= add;
        }



        
        //第二步：还有剩余数量，放到下标最小的空格子中
        if (remaining > 0)
        {
            int emptyIndex = FindFirstEmptyCellIndex();
            if (emptyIndex >= 0)
            {
                ItemUnit newUnit = CreateItemUnit(itemCells[emptyIndex], item.itemName, item.itemIcon,
                    remaining, item.maxStackNum, item.itemPrefabsName, emptyIndex, null);
                AttachItemBehavior(newUnit, item);
                remaining = 0;
            }
        }
        //暂不处理格子不足、物品无法全部放完的情况
        //布局发生改变，刷新物品占格最大下标记录
        RefreshItemCellMaxIndexAndQuantity();
    }
    public bool ConsumeItem(string itemName, int num, int index = -1)
    {
        //消耗数量小于等于0视为成功
        if (num <= 0) return true;
        if (string.IsNullOrEmpty(itemName)) return false;

        //指定了格子下标(>=0)：只消耗该下标格子的物品（供物品操作面板的“使用/丢弃”精确到格子的消耗）
        if (index >= 0)
        {
            return ConsumeItemFromCell(itemName, num, index);
        }

        //未指定下标(-1)：按原来的逻辑，从该物品占据的最大下标格子开始向前消耗
        int remaining = num;

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
        Debug.Log("已消耗" + itemName + itemsNum.GetValueOrDefault(itemName) + "个");
        //能完全消耗掉指定数量才算成功
        return remaining == 0;
    }
    //只消耗指定下标格子中的物品：格子里没有物品或物品名称不匹配时打印警告
    bool ConsumeItemFromCell(string itemName, int num, int index)
    {
        if (index < 0 || index >= itemCells.Count)
        {
            Debug.LogWarning("无法消耗" + itemName + "：格子下标" + index + "超出背包范围");
            return false;
        }
        ItemUnit cellItem = GetItemInCell(index);
        if (cellItem == null)
        {
            Debug.LogWarning("无法消耗" + itemName + "：下标" + index + "的格子为空");
            return false;
        }
        if (cellItem.itemName.text != itemName)
        {
            Debug.LogWarning("无法消耗" + itemName + "：下标" + index + "的格子里是" + cellItem.itemName.text);
            return false;
        }

        int.TryParse(cellItem.curNum.text, out int cur);
        if (cur <= 0)
        {
            //数量为0的空堆叠直接销毁
            Destroy(cellItem.gameObject);
            RefreshItemCellMaxIndexAndQuantity();
            return false;
        }

        if (cur >= num)
        {
            //该格子数量足够，扣除num
            int left = cur - num;
            if (left > 0) cellItem.curNum.text = left.ToString();
            else Destroy(cellItem.gameObject);
            RefreshItemCellMaxIndexAndQuantity();
            return true;
        }
        else
        {
            //该格子数量不足num，只能把现有数量全部消耗掉
            Destroy(cellItem.gameObject);
            RefreshItemCellMaxIndexAndQuantity();
            Debug.LogWarning("消耗" + itemName + "数量不足：下标" + index + "的格子只有" + cur + "个，需要" + num + "个");
            return false;
        }
    }
    //丢弃入口：把指定格子中的物品丢弃num个到场景中（步行/驾车通用）。
    //丢弃点取当前操作物体(打开背包的BagLogicProcess对应物体)上的DiscardPoint，朝其正前方抛出。
    public bool DiscardItem(ItemUnit itemUnit, int num)
    {
        if (itemUnit == null || num <= 0) return false;

        int index = GetItemCellIndex(itemUnit);
        ItemUnit cellItem = GetItemInCell(index);
        if (cellItem == null)
        {
            Debug.LogWarning("丢弃失败：格子为空或物品已被移动");
            return false;
        }

        int.TryParse(cellItem.curNum.text, out int cur);
        if (cur <= 0) return false;
        //丢弃数量不能超过该堆叠现有数量(输入超过则整堆丢弃)
        int count = Mathf.Min(num, cur);

        Transform point = GetDiscardPoint();
        if (point == null)
        {
            Debug.LogWarning("丢弃失败：未找到可用的丢弃点DiscardPoint");
            return false;
        }

        //先在场中生成被丢弃物品，成功后再从背包扣除，避免物品凭空消失
        if (!SpawnDroppedItem(cellItem, count, point))
            return false;

        ConsumeItemFromCell(cellItem.itemName.text, count, index);
        return true;
    }

    //在丢弃点正前方生成被丢弃的物品，把数据与数量装载到其BaseItemObjData组件上，并给它初速度
    bool SpawnDroppedItem(ItemUnit source, int count, Transform point)
    {
        //优先取格子单元上记录的预制体名，其次取随格子存活的行为组件上的
        string prefabName = source.itemPrefabsName;
        BaseItemObjData srcData = source.GetComponent<BaseItemObjData>();
        if (string.IsNullOrEmpty(prefabName) && srcData != null)
            prefabName = srcData.itemPrefabsName;
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning("丢弃失败：" + source.itemName.text + " 未配置itemPrefabsName");
            return false;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Items/" + prefabName);
        if (prefab == null)
        {
            Debug.LogWarning("丢弃失败：未找到预制体 Prefabs/Items/" + prefabName);
            return false;
        }

        GameObject drop = Instantiate(prefab, point.position, point.rotation);
        BaseItemObjData data = drop.GetComponent<BaseItemObjData>();
        if (data != null)
        {
            if (srcData != null)
                data.CopyDataFrom(srcData);
            else
            {
                //格子单元未附带行为组件时，用格子单元自身信息填充
                data.itemName = source.itemName.text;
                data.itemIcon = source.itemIcon.sprite;
                data.maxStackNum = source.maxStackNum;
                data.itemPrefabsName = prefabName;
            }
            //本次丢弃数量以输入为准
            data.curNum = count;
        }

        //从丢弃点的正前方抛出
        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = point.forward * throwSpeed;

        return true;
    }

    //获取当前操作的丢弃点：优先用打开背包的可操作物体(BagLogicProcess)上的，否则退回玩家
    Transform GetDiscardPoint()
    {
        if (bagOpener != null && bagOpener.DiscardPoint != null)
            return bagOpener.DiscardPoint;
        PlayerController pc = SceneMgr.Instance != null ? SceneMgr.Instance.player : null;
        if (pc != null && pc.DiscardPoint != null) return pc.DiscardPoint;
        return null;
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
        //Debug.Log("当前每个物品的总数量为：");
        foreach (var kvp in itemsNum)
        {
            Debug.Log(kvp.Key + ": " + kvp.Value);
        }
        //通知HUD等外部(如PlayerPanel/VehiclePanel)背包数据已变化，刷新显示
        OnBagItemsChanged?.Invoke();
    }
    void OnDestroy()
    {
        //清空事件，避免面板销毁后残留引用导致空引用
        OnBagItemsChanged = null;
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
    //获取物品单元当前所在格子的下标：以它实际的父物体为准实时计算，避免使用可能过期的缓存cellIndex
    public int GetItemCellIndex(ItemUnit itemUnit)
    {
        if (itemUnit == null || itemUnit.transform.parent == null) return -1;
        return itemCells.IndexOf(itemUnit.transform.parent);
    }
    //创建新的物品单元并放入指定格子中，填充数据后返回
    ItemUnit CreateItemUnit(Transform cell, string itemName, Sprite itemIcon, int curNum, int maxStackNum, string itemPrefabsName, int cellIndex, UnityAction<int> action)
    {
        //Debug.Log("创建物品单元：" + itemName + "，数量：" + curNum + "，格子下标：" + cellIndex);
        GameObject itemUnitObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/ItemUnit"));
        itemUnitObj.transform.SetParent(cell, false);
        ItemUnit itemUnit = itemUnitObj.GetComponent<ItemUnit>();
        itemUnit.itemIcon.sprite = itemIcon;
        itemUnit.itemName.text = itemName;
        itemUnit.curNum.text = curNum.ToString();
        itemUnit.maxStackNum = maxStackNum;
        itemUnit.itemPrefabsName = itemPrefabsName;
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
                cell.maxStackNum, "", cell.cellIndex, (v) => { Debug.Log("Json导入的物品暂时无行为函数"); });
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