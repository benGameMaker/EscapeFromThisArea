using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包物品拖拽组件，挂在物品ItemUnit预制体上。
/// 层级结构：Cells(所有格子的父物体) -> Cell格子 -> 物品ItemUnit
/// 支持：拖到其他空格子移动过去、拖到非法位置松开回到原格子、与其他格子中的物品交换位置
/// </summary>
public class DragItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject fatherObj;          // 所有格子的父物体 Cells
    public GameObject originalFatherObj;  // 拖拽前所在的格子

    CanvasGroup cg;
    RectTransform selfRect;
    ItemUnit selfItem;

    private void Start()
    {
        cg = GetComponent<CanvasGroup>();
        selfRect = GetComponent<RectTransform>();
        selfItem = GetComponent<ItemUnit>();
        // 通过层级关系获取Cells：物品的父物体是格子，格子的父物体是Cells
        fatherObj = GetCellsObj();
    }

    // 从层级关系中获取Cells，避免硬编码路径导致空引用
    GameObject GetCellsObj()
    {
        if (transform.parent != null && transform.parent.parent != null)
            return transform.parent.parent.gameObject;
        // 兜底：按路径查找
        return GameObject.Find("Canvas/BagPanel/Cells");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 懒获取，防止Start执行时层级还不完整
        if (fatherObj == null) fatherObj = GetCellsObj();
        if (transform.parent == null) return;

        originalFatherObj = transform.parent.gameObject;
        // 拖拽时关闭射线检测，让鼠标能穿透本物品检测到下面的格子/物品
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 防御：找不到Cells时放弃本次拖拽跟随，避免空引用报错
        if (fatherObj == null) return;

        // 让物品跟随鼠标移动（Overlay模式下世界坐标即屏幕像素坐标）
        transform.position = eventData.position;
        // 拖拽期间把物品临时放到Cells下，脱离格子的布局限制
        if (transform.parent != fatherObj.transform)
            transform.SetParent(fatherObj.transform);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalFatherObj == null) return;

        GameObject bottomObj = eventData.pointerCurrentRaycast.gameObject;
        if (bottomObj == null)
        {
            // 拖到非法位置，回到原格子
            SetSelfParentAndCell(originalFatherObj.transform, true);
        }
        else if (bottomObj.CompareTag("Cell"))
        {
            // 拖到空格子，移动过去
            SetSelfParentAndCell(bottomObj.transform, true);
        }
        else if (bottomObj.CompareTag("ItemUnit"))
        {
            Transform newCell = bottomObj.transform.parent;
            ItemUnit otherItem = bottomObj.GetComponent<ItemUnit>();
            // 名称、图标都相同视为同种物品，进行堆叠
            if (selfItem != null && otherItem != null &&
                selfItem.itemName.text == otherItem.itemName.text &&
                selfItem.itemIcon.sprite == otherItem.itemIcon.sprite)
            {
                // 先恢复射线检测（堆叠后本物品会被销毁）
                cg.blocksRaycasts = true;
                BagPanel bagPanel = UIManager.Instance.GetPanel<BagPanel>();
                if (bagPanel != null)
                {
                    // 数量全部并入目标格子，装不下的部分随被拖物品单元放入空格子
                    bagPanel.StackItemToCell(selfItem, newCell, originalFatherObj.transform);
                }
                else
                {
                    // 找不到背包面板则回到原格子
                    SetSelfParentAndCell(originalFatherObj.transform, true);
                }
            }
            else
            {
                // 不同种物品，交换位置
                bottomObj.transform.SetParent(originalFatherObj.transform, false);
                // 刷新对方物品所在格子的布局，让其居中
                LayoutRebuilder.ForceRebuildLayoutImmediate(originalFatherObj.transform as RectTransform);
                // 同步更新对方物品的格子下标
                if (otherItem != null) otherItem.cellIndex = GetCellIndex(originalFatherObj);
                // 自己移动到目标格子
                SetSelfParentAndCell(newCell, true);
            }
        }
        else
        {
            // 其他非法目标，回到原格子
            SetSelfParentAndCell(originalFatherObj.transform, true);
        }

        //拖拽后背包布局可能变化，刷新每种物品占格的最大下标记录
        UIManager.Instance.GetPanel<BagPanel>()?.RefreshItemCellMaxIndex();
    }

    // 把物品放入指定格子，并强制刷新布局使其居中
    void SetSelfParentAndCell(Transform parent, bool block)
    {
        // worldPositionStays=false：不保留鼠标处的世界坐标，让格子的HorizontalLayoutGroup接管位置
        transform.SetParent(parent, false);
        cg.blocksRaycasts = block;
        selfRect.sizeDelta = new Vector2(115f, 115f);
        // 强制立即刷新格子布局，让物品马上回到格子中央
        RectTransform parentRect = parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        // 同步更新物品自身记录的格子下标
        if (selfItem != null) selfItem.cellIndex = GetCellIndex(parent.gameObject);
    }

    // 从格子名(Cell_0)中解析格子下标
    int GetCellIndex(GameObject cellObj)
    {
        if (cellObj == null) return -1;
        string name = cellObj.name;
        int lastIndex = name.LastIndexOf('_');
        if (lastIndex < 0 || lastIndex >= name.Length - 1) return -1;
        if (int.TryParse(name.Substring(lastIndex + 1), out int index)) return index;
        return -1;
    }
}
