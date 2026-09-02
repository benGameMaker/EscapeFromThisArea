using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 物品单元：显示背包格子中的物品信息，并处理右键点击弹出该物品的操作面板。
/// </summary>
public class ItemUnit : MonoBehaviour, IPointerClickHandler
{
    public Image itemIcon;
    public TextMeshProUGUI curNum;
    public TextMeshProUGUI itemName;
    public int maxStackNum;
    public int useBehaviorNumber;
    public int cellIndex;

    //所属背包的物品操作面板（由BagPanel在创建物品单元时赋值）
    [HideInInspector]
    public Transform iip;
    public UnityAction UseBehavior;

    public void OnPointerClick(PointerEventData eventData)
    {
        //右键点击物品：弹出该物品所在格子的操作面板
        if (eventData.button == PointerEventData.InputButton.Right)
            ShowInteractionPanel();
    }

    //显示操作面板：物品的直接父物体就是所在格子，面板左上角对齐该格子的右上角
    void ShowInteractionPanel()
    {
        if (transform.parent == null) return;
        ItemInteractionPanel panel = iip != null ? iip.GetComponent<ItemInteractionPanel>() : null;
        if (panel != null)
        {
            panel.ShowAtAndSetUseBehavor(transform.parent);
            panel.PrcessingItem = this;
        }
    }
}
