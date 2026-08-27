using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestItem : BaseInteractivity
{

    public override void InteractionBehavior(GameObject obj)
    {
        Debug.Log(obj + "捡起了" + name);
        Destroy(this.gameObject, 1f);
    }
}
