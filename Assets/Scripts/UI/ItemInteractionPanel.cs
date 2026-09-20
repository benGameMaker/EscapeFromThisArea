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
        btnUse.onClick.AddListener(OnUse);
        btnDiscard.onClick.AddListener(OnDiscard);
        
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
 
        //格子右上角的世界坐标，作为面板左上角的放置位置
        Vector3 topRightWorld = cellRt.TransformPoint(new Vector3(cellRt.rect.xMax, cellRt.rect.yMax, 0f));
        loaclRt.position = topRightWorld;

        //本帧刚显示，跳过同帧的外部点击隐藏检测
        justShownFrame = Time.frameCount;
        ShowMe();
    }

    //判断屏幕坐标点是否落在面板矩形范围内
    bool IsPointerInside(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(loaclRt, screenPos, canvas.worldCamera);
    }
    //点击"使用"：执行该物品绑定的使用行为，面板保持显示，直到点击面板外部/关闭背包
    void OnUse()
    {
        if (ProcessingItem == null || curCellIndex < 0)
        {
            Debug.LogWarning("使用物品失败：未正确记录目标物品或其格子下标");
            return;
        }
        if (ProcessingItem.UseBehavior != null)
        {
            ProcessingItem.UseBehavior.Invoke(curCellIndex);
        }
        else
        {
            Debug.LogWarning("该物品没有可用的使用行为：" + ProcessingItem.itemName.text);
        }
    }
    //点击丢弃,读取输入框中的数量，从该格扣除并把物品从当前操作物体的DiscardPoint正前方丢出
    void OnDiscard()
    {
        BagPanel bag = GetBag();
        if (ProcessingItem == null || bag == null)
        {
            Debug.LogWarning("丢弃失败：未正确记录目标物品或找不到背包");
            UIManager.Instance.GetPanel<PromptPanel>().ShowPromptText("内部发生错误，拖动后再尝试", 2f);
            return;
        }

        //读取输入框数值；为空或非法时按0处理(不丢弃)
        int num = 0;
        if (ipfNum != null) int.TryParse(ipfNum.text, out num);

        if (num > 0)
        {
            bag.DiscardItem(ProcessingItem, num);
        }
        else
        {
            Debug.LogWarning("请输入有效的丢弃数量");
        }
    }
    //获取所属背包（面板是BagPanel的子物体，找不到时回退到UIManager）
    BagPanel GetBag()
    {
        BagPanel bag = GetComponentInParent<BagPanel>();
        if (bag == null) bag = UIManager.Instance.GetPanel<BagPanel>();
        return bag;
    }

    public override void ShowMe()
    {
        base.ShowMe();
        canvasGroup.blocksRaycasts = true;
    }
    public override void HideMe(UnityAction callback)
    {
        base.HideMe(callback);
        canvasGroup.blocksRaycasts = false;
    }
}
