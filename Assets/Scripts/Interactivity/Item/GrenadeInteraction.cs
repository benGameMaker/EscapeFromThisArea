using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeInteraction : BaseInteractivity
{
    public override void InteractionBehavior(PlayerController obj)
    {
        GameDataMgr.Instance.PlaySound(audio);
        PickToBag<GrenadeItem>();
    }
}
