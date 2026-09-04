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
    //用于丢弃时加载场景物品预制体的名称(Resources/Prefabs/Items下的名称)
    public string itemPrefabsName;
    public int cellIndex;

    //所属背包的物品操作面板（由BagPanel在创建物品单元时赋值）
    [HideInInspector]
    public Transform iip;
    public UnityAction<int> UseBehavior;

    public void OnPointerClick(PointerEventData eventData)
    {
        //右键点击物品：弹出该物品所在格子的操作面板
        if (eventData.button == PointerEventData.InputButton.Right)
            ShowInteractionPanel();
    }

    //显示操作面板：把自身交给面板，由面板负责定位与记录目标物品
    void ShowInteractionPanel()
    {
        if (transform.parent == null) return;
        ItemInteractionPanel panel = iip != null ? iip.GetComponent<ItemInteractionPanel>() : null;
        if (panel != null)
            panel.ShowAt(this);
    }
}
