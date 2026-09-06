using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    public GameObject prefab;   // 资源预制体
    [Range(0, 100)]
    public int weight = 10;     // 权重值，范围0~100，方便在编辑器里调整
}
