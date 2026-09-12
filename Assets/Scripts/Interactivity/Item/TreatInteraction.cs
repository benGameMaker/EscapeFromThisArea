using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreatInteraction : BaseInteractivity
{
    public override void InteractionBehavior(PlayerController obj)
    {
        GameDataMgr.Instance.PlaySound(audio);
        PickToBag<TreatItem>();
    }
}
