using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject fatherObj;
    public GameObject originalFatherObj;

    CanvasGroup cg;
    RectTransform selfRect;
    private void Start()
    {
        fatherObj = GameObject.Find("Canvas/BagPanel/Cells");
        cg = GetComponent<CanvasGroup>();
        selfRect = GetComponent<RectTransform>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalFatherObj = transform.parent.gameObject;
        cg.blocksRaycasts = false;
    }
    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
        transform.SetParent(fatherObj.transform);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject bottomObj = eventData.pointerCurrentRaycast.gameObject;
        if ((bottomObj == null))
        {
            SetSelfParentAndCell(originalFatherObj.transform, 115f, true);
        } 
        else if (bottomObj != null && bottomObj.CompareTag("Cell"))
        {
            SetSelfParentAndCell(bottomObj.transform, 115f, true);
        }
        else if (bottomObj != null && bottomObj.CompareTag("ItemUnit"))
        {
            Transform newCell = bottomObj.transform.parent;
            bottomObj.transform.SetParent(originalFatherObj.transform);

            SetSelfParentAndCell(newCell.transform, 115f, true);
        }
        else
        {
            SetSelfParentAndCell(originalFatherObj.transform, 115f, true);
        }
    }
    void SetSelfParentAndCell(Transform parent, float size,bool block)
    {
        transform.SetParent(parent);
        cg.blocksRaycasts = block;
        selfRect.sizeDelta = new Vector2(size, size);
    }
}
