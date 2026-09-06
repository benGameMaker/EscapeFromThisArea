using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchGameOver : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Vehicle"))
        {
            BagLogicProcess blp = other.GetComponentInParent<BagLogicProcess>();
            blp.EnableGameplayInput(false);
            UIManager.Instance.ShowPanel<OverPanel>();
        }
    }
}
