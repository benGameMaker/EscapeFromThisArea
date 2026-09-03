using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairInteraction : BaseInteractivity
{
    public override void InteractionBehavior(PlayerController obj)
    {
        PickToBag<RepairItem>();
    }
}
