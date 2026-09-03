using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletInteraction : BaseInteractivity
{
    BulletItem bulletItem;
    private void Start()
    {
        bulletItem = GetComponent<BulletItem>();
        if (bulletItem == null) Debug.Log("该子弹缺乏物品数据");
    }
    public override void InteractionBehavior(PlayerController obj)
    {
        if (UIManager.Instance.GetPanel<BagPanel>() != null && bulletItem != null)
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(bulletItem);
            //Debug.Log("拾取了" + this.name);
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("背包未初始化或该子弹无物品数据，无法拾取");
        }
    }
}
