using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairItem : BaseItemObjData
{
    public override void UseBahaivor(int Integer)
    {
        if (SceneManager.Instance.player.vd != null)
        {
            SceneManager.Instance.player.vd.RepairVehicle(1);
            UIManager.Instance.GetPanel<BagPanel>().ConsumeItem(itemName, 1, Integer);
            Debug.Log("进行车辆修补");
        }
        else
        {
            Debug.Log("玩家不在车上");
        }
    }
}
