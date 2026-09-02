using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletItem : BaseItemObjData
{
    public override void UseBahaivor()
    {
        Debug.Log("这是" + this.gameObject.name);
    }
}
