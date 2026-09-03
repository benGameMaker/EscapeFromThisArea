using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 物品操作面板：右键点击格子中的物品时显示，用于对物品执行"使用/丢弃"等操作。
/// 作为BagPanel的子物体存在于背包中，继承BasePanel，默认隐藏(isShow=false)，
/// 显示/隐藏使用BasePanel的ShowMe/HideMe实现淡入淡出。
/// 显示时面板左上角与所在格子的右上角重合；显示期间点击面板以外的任意位置(左键或右键)会隐藏面板。
/// </summary>
public class ItemInteractionPanel : BasePanel
{
    public Button btnUse;            //使用按钮
    public Button btnDiscard;        //丢弃按钮
    public TMP_InputField ipfNum;    //丢弃数量输入框

    //当前操作的目标物品单元（由ShowAt记录）
    public ItemUnit ProcessingItem;
    //目标物品当前所在格子的下标：打开面板时以实际父物体实时计算，避免使用可能过期的缓存cellIndex
    int curCellIndex = -1;

    //记录本面板刚被显示的帧号，同一帧内跳过"点击面板外部"的隐藏检测，
    //避免"右键打开面板"与"点击外部立即隐藏"在同帧发生冲突
    int justShownFrame = -1;

    public override void Init()
    {
        //优先使用场景/预制体中的引用；缺失时按节点名查找兜底
        if (btnUse == null) btnUse = transform.Find("btnUse")?.GetComponent<Button>();
        if (btnDiscard == null) btnDiscard = transform.Find("btnDiscard")?.GetComponent<Button>();
        if (ipfNum == null) ipfNum = transform.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();

        if (btnUse != null) btnUse.onClick.AddListener(OnUse);
        if (btnDiscard != null) btnDiscard.onClick.AddListener(OnDiscard);

        //若创建后被同帧的右键打开过则保持显示，否则默认隐藏
        if (!isShow) HideMe(() => { });
    }

    protected override void Update()
    {
        base.Update();

        //显示期间：点击面板以外的任意位置(左键或右键)则隐藏
        if (isShow && Time.frameCount != justShownFrame)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                if (!IsPointerInside(Input.mousePosition))
                    HideMe(() => { });
            }
        }
    }

    //判断屏幕坐标点是否落在面板矩形范围内
    bool IsPointerInside(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(loaclRt, screenPos, canvas.worldCamera);
    }

    //在目标物品所在格子的右上角显示操作面板，并记录该物品及其所在格子下标
    public void ShowAt(ItemUnit itemUnit)
    {
        if (itemUnit == null) return;
        RectTransform cellRt = itemUnit.transform.parent as RectTransform;
        if (cellRt == null) return;

        //记录目标与它当前所在格子的下标(实时计算)
        ProcessingItem = itemUnit;
        BagPanel bag = GetBag();
        curCellIndex = bag != null ? bag.GetItemCellIndex(itemUnit) : -1;

        //面板锚点与定位点都设为左上角，使摆放以左上角为基准
        loaclRt.anchorMin = new Vector2(0f, 1f);
        loaclRt.anchorMax = new Vector2(0f, 1f);
        loaclRt.pivot = new Vector2(0f, 1f);
        //格子右上角的世界坐标，作为面板左上角的放置位置
        Vector3 topRightWorld = cellRt.TransformPoint(new Vector3(cellRt.rect.xMax, cellRt.rect.yMax, 0f));
        loaclRt.position = topRightWorld;

        //本帧刚显示，跳过同帧的外部点击隐藏检测
        justShownFrame = Time.frameCount;
        ShowMe();
    }

    //点击"使用"：执行该物品绑定的使用行为，然后关闭面板
    void OnUse()
    {
        if (ProcessingItem == null || curCellIndex < 0)
        {
            Debug.LogWarning("使用物品失败：未正确记录目标物品或其格子下标");
            HideMe(() => { });
            return;
        }
        if (ProcessingItem.UseBehavior != null)
        {
            //行为内部(如RepairItem)会调用 BagPanel.ConsumeItem(name, num, curCellIndex) 精确消耗该格
            ProcessingItem.UseBehavior.Invoke(curCellIndex);
        }
        else
        {
            Debug.LogWarning("该物品没有可用的使用行为：" + ProcessingItem.itemName.text);
        }
        //使用结束后关闭操作面板
        HideMe(() => { });
    }

    //[丢弃功能预告-尚未实现] 点击"丢弃"：预期流程为——
    //1. 读取 ipfNum 输入的数量num；
    //2. 调用 BagPanel.ConsumeItem(ProcessingItem.itemName.text, num, curCellIndex) 扣除该格数量，
    //   数量被扣到0时会销毁格子内的物品单元(ConsumeItem已处理)；
    //3. 用一份携带BaseItemObjData的物品预制体装载被丢弃物品的数据与数量，
    //   从玩家物体(如腹部)朝某方向(如正前方)施加力/速度把它丢到场景中。
    //待实现丢弃时需要用到：丢弃发射点/方向/力度等引用字段，届时在此补充即可。
    void OnDiscard()
    {
        //TODO(丢弃预告)：实现上述丢弃逻辑
    }

    //获取所属背包（面板是BagPanel的子物体，找不到时回退到UIManager）
    BagPanel GetBag()
    {
        BagPanel bag = GetComponentInParent<BagPanel>();
        if (bag == null) bag = UIManager.Instance.GetPanel<BagPanel>();
        return bag;
    }
}
