using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class InteractionPanel : BasePanel
{
    public TextMeshProUGUI txtGuidance;
    public TextMeshProUGUI txtItem;

    [Tooltip("UI空间偏移，作用于最终面板位置（以像素为单位的微调）")]
    public Vector2 uiOffset = Vector2.zero;

    bool hasBeenInitialize = false;

    Transform itemPos;
    Vector2? overrideUiOffset;

    public override void Init()
    {
        loaclRt = GetComponent<RectTransform>();
        HideMe(() => { });
        Debug.Log("InteractionPanel初始化完成");
    }

    protected override void Update()
    {
        base.Update();
        if (itemPos != null) syncItemPosToUIScreen();
    }

    public void ShowMe(string textG, string textI, Transform itemPos)
    {
        base.ShowMe();
        this.itemPos = itemPos;
        this.overrideUiOffset = null;

        txtGuidance.text = textG;
        txtItem.text = textI;
        Debug.Log("正在显示" + itemPos.gameObject.name + "交互信息");
    }

    /// <summary>
    /// 显示交互面板，并临时覆盖UI空间的偏移（像素单位）
    /// </summary>
    public void ShowMe(string textG, string textI, Transform itemPos, Vector2 offset)
    {
        base.ShowMe();
        this.itemPos = itemPos;
        this.overrideUiOffset = offset;

        txtGuidance.text = textG;
        txtItem.text = textI;
        Debug.Log("正在显示" + itemPos.gameObject.name + "交互信息");
    }

    Vector3 screenPostion;
    void syncItemPosToUIScreen()
    {
        Vector3 targetWorldPos = itemPos.position;
        screenPostion = cam.WorldToScreenPoint(targetWorldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPostion,
            canvas.worldCamera,
            out Vector2 loaclPoint
        );
        Vector2 finalUiOffset = overrideUiOffset ?? uiOffset;
        loaclRt.anchoredPosition = loaclPoint + finalUiOffset;

        Debug.Log("正在跟踪" + itemPos.gameObject.name + "的位置");
    }
    public override void ShowMe()
    {
        base.ShowMe();
        if (!hasBeenInitialize)
        {
            isShow = false;
            hasBeenInitialize = true;
        }
    }
    public override void HideMe(UnityAction callback)
    {
        isShow = false;
        hideCallBack = callback;
    }
}
