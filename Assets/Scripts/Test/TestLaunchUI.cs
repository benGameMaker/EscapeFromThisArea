using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLauchUI : MonoBehaviour
{
    public ItemObjData item1;
    public ItemObjData item2;
    public Transform ThrowPoint;
    public int consumeNum;
    BagPanel bp;
    void Start()
    {
        UIManager.Instance.ShowPanel<BagPanel>();
        bp = UIManager.Instance.GetPanel<BagPanel>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (UIManager.Instance.GetPanel<BagPanel>() == null)
            {
                UIManager.Instance.ShowPanel<BagPanel>();
                bp = UIManager.Instance.GetPanel<BagPanel>();
            }
            if (bp.isShow) bp.HideMe(() => { });
            else bp.ShowMe();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            bp.AddItem(item1);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log((bp.ConsumeItem(item2.itemName, consumeNum) ? "能够消耗" : "不够消耗") + item2.itemName);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            // Instantiate a new grenade at the throw point and trigger its fuse
            GameObject grenade = Instantiate(Resources.Load<GameObject>("Prefabs/Items/Wep_Grenade"), ThrowPoint.position, ThrowPoint.rotation);
            GrenadeControl gc = grenade.GetComponent<GrenadeControl>();
            if (gc != null)
            {
                gc.TriggerFuse();
            }
        }
    }
}
