using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairItem : BaseItemObjData
{
    public override void UseBahaivor(int Integer)
    {
        if (SceneMgr.Instance.player.vd != null)
        {
            if (SceneMgr.Instance.player.vd.curIntegrity >= 100)
            {
                UIManager.Instance.GetPanel<PromptPanel>().ShowPromptText("车辆完整度已满！");
                return;
            }
            SceneMgr.Instance.player.vd.RepairVehicle(1);
            UIManager.Instance.GetPanel<BagPanel>().ConsumeItem(itemName, 1, Integer);
            Debug.Log("进行车辆修补");
        }
        else
        {
            Debug.Log("玩家不在车上");
        }
    }
}
