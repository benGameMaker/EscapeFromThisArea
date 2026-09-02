using System;
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

    public ItemUnit PrcessingItem;
    //记录本面板刚被显示的帧号，同一帧内跳过"点击面板外部"的隐藏检测，
    //避免"右键打开面板"与"点击外部立即隐藏"在同帧发生冲突
    int justShownFrame = -1;

    public override void Init()
    {
        btnUse.onClick.AddListener(() =>
        {
            PrcessingItem.UseBehavior?.Invoke();
        });
        btnDiscard.onClick.AddListener(() =>
        {

        });
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
                {
                    HideMe(() => { });
                }
            }
        }
    }

    //判断屏幕坐标点是否落在面板矩形范围内
    bool IsPointerInside(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(loaclRt, screenPos, canvas.worldCamera);
    }

    //在指定格子的右上角显示操作面板
    public void ShowAtAndSetUseBehavor(Transform cell)
    {
        RectTransform cellRt = cell as RectTransform;
        if (cellRt == null) return;

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
}
