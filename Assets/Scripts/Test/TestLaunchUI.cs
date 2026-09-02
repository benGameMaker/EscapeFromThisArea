using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLauchUI : MonoBehaviour
{
    public BaseItemObjData item1;
    public BaseItemObjData item2;
    public Transform ThrowPoint;
    public int consumeNum;
    BagPanel bp;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (bp == null)
            {
                UIManager.Instance.ShowPanel<BagPanel>();
                bp = UIManager.Instance.GetPanel<BagPanel>();
            }
            else
            {
                if (bp.isShow) bp.HideMe(() => { });
                else bp.ShowMe();
            }
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            bp.AddItem(item1);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (item2 != null) Debug.Log((bp.ConsumeItem(item2.itemName, consumeNum) ? "能够消耗" : "不够消耗") + item2.itemName);
            else Debug.Log("H键对应的物品为空");
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
