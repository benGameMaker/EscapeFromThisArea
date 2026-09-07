using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletInteraction : BaseInteractivity
{
    public override void InteractionBehavior(PlayerController obj)
    {
        GameDataMgr.Instance.PlaySound(audio);
        PickToBag<BulletItem>();
    }
}
