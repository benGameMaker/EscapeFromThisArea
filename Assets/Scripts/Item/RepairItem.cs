using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairItem : BaseItemObjData
{
    public override void UseBahaivor(int Integer)
    {
        if (SceneManager.Instance.player.vd != null)
        {
            if (SceneManager.Instance.player.vd.curIntegrity >= 100)
            {
                UIManager.Instance.GetPanel<PromptPanel>().ShowPromptText("The vehicle has been repaired");
                return;
            }
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
