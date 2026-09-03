using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [Header("生成配置")]
    public GameObject[] lootPrefabs;      // 可生成的资源预制体列表
    public int spawnCount = 10;           // 每次刷新生成的数量
    public LayerMask groundLayerMask;     // 地面层，用于检测地面位置
    public float spawnHeightOffset = 0.1f; // 生成时沿法线方向向上偏移的距离


    private BoxCollider areaCollider;
    private List<Vector3> usedPositions = new List<Vector3>(); // 记录已使用的点位（用于避免重叠）

    void Start()
    {
        areaCollider = GetComponent<BoxCollider>();
        SpawnLoot();
    }

    void SpawnLoot()
    {
        // 边界数据缓存
        Vector3 center = areaCollider.bounds.center;
        Vector3 size = areaCollider.bounds.size;

        int spawnedCount = 0;
        int totalAttempts = 0;

        while (spawnedCount < spawnCount && totalAttempts < spawnCount * 10)
        {
            totalAttempts++;

            // 1. 生成随机点位
            Vector3 randomPoint = GetRandomPointInBox(center, size);

            // 2. 向下发射射线检测地面
            if (Physics.Raycast(randomPoint + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 25f, groundLayerMask))
            {
                // 3. 计算生成位置：沿法线方向向上偏移
                Vector3 spawnPosition = hit.point + hit.normal * spawnHeightOffset;
                // 5. 从列表里随机选一个预制体
                GameObject selectedPrefab = lootPrefabs[Random.Range(0, lootPrefabs.Length)];
                // 6. 生成物体，并随机旋转
                GameObject newLoot = Instantiate(selectedPrefab, spawnPosition, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                spawnedCount++;
            }
        }

        // 打印生成结果日志
        if (spawnedCount < spawnCount)
        {
            Debug.LogWarning($"[LootSpawner] 仅生成了 {spawnedCount}/{spawnCount} 个资源，尝试次数已达上限 {spawnCount * 10}。");
        }
        else
        {
            Debug.Log($"[LootSpawner] 成功生成 {spawnedCount} 个资源。");
        }
    }

    // 在BoxCollider范围内生成随机坐标
    Vector3 GetRandomPointInBox(Vector3 center, Vector3 size)
    {
        float randomX = Random.Range(-size.x / 2f, size.x / 2f);
        float randomZ = Random.Range(-size.z / 2f, size.z / 2f);
        return center + new Vector3(randomX, 0, randomZ);
    }

    // 手动刷新资源（外部调用）
    public void RefreshLoot()
    {
        // 先删除区域内原有的资源（需要标记或查找子物体）
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        usedPositions.Clear();
        SpawnLoot();
    }

    // 在编辑器中可视化区域
    void OnDrawGizmos()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<BoxCollider>();

        if (areaCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(areaCollider.bounds.center, areaCollider.bounds.size);
        }
    }
}