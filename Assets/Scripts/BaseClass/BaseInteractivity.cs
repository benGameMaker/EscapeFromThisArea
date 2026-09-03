using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInteractivity : MonoBehaviour
{
    public string guidingText;
    public string itemName;

    public Vector2 panelOffset;
    InteractionPanel interactionPanel;

    public abstract void InteractionBehavior(PlayerController obj);

    InteractionPanel GetInteractionPanel()
    {
        if (interactionPanel == null)
        {
            interactionPanel = UIManager.Instance.GetPanel<InteractionPanel>();
            if (interactionPanel == null)
            {
                interactionPanel = UIManager.Instance.ShowPanel<InteractionPanel>();
            }
        }
        return interactionPanel;
    }
    public void ShowInteractionPanel()
    {
        var panel = GetInteractionPanel();
        if (panel == null)
        {
            Debug.LogWarning(name + " 无法获取InteractionPanel，跳过显示");
            return;
        }
        if (panel.isShow == false) panel.ShowMe(guidingText, itemName, transform, panelOffset);
    }
    public void HideInteractionPanel()
    {
        var panel = GetInteractionPanel();
        if (panel == null)
        {
            return;
        }
        if (panel.isShow == true) panel.HideMe(() => { });
    }

    protected virtual void PickToBag<T>(PlayerController obj = null) where T : BaseItemObjData
    {
        T itemData;
        itemData = GetComponent<T>();
        if (UIManager.Instance.GetPanel<BagPanel>() != null && itemData != null)
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(itemData);
            Debug.Log("拾取了" + this.name);
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("背包未初始化或该物无物品数据，无法拾取");
        }
    }
}
