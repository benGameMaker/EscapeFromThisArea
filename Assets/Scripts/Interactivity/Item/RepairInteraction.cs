using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairInteraction : BaseInteractivity
{
    RepairItem repairItem;
    private void Start()
    {
        repairItem = GetComponent<RepairItem>();
    }
    public override void InteractionBehavior(PlayerController obj)
    {
        if (UIManager.Instance.GetPanel<BagPanel>() != null && repairItem != null)
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(repairItem);
            //Debug.Log("拾取了" + this.name);
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("背包未初始化或该维修物无物品数据，无法拾取");
        }
    }
}
