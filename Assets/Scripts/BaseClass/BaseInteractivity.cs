using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInteractivity : MonoBehaviour
{
    public string guidingText;
    public string itemName;

    public Vector2 panelOffset;
    InteractionPanel interactionPanel;

    public abstract void InteractionBehavior(GameObject obj);

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

}
