using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        UIManager.Instance.ShowPanel<OverPanel>();
    }
}
