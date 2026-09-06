using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 僵尸刷新管理器 —— 以玩家为中心，在特定范围内刷新僵尸
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [Header("=== 核心开关 ===")]
    public bool enableSpawn = true;          // 运行时开启/关闭刷新功能

    [Header("=== 刷新参数 ===")]
    public float spawnInterval = 5f;         // 刷新间隔（秒）
    public float spawnMinRange = 8f;         // 最小刷新距离（避免刷在脸上）
    public float spawnMaxRange = 25f;        // 最大刷新距离
    public int maxAliveZombies = 10;         // 最多存活的僵尸数量（由本脚本刷新）

    [Header("=== 视野规避参数 ===")]
    public float playerViewAngle = 60f;      // 玩家视野角度（度），用于判断是否在视野内
    public LayerMask wallLayerMask;          // 墙壁层，用于检测视线遮挡

    [Header("=== 生成位置偏移 ===")]
    public float groundCheckHeight = 20f;    // 地面检测的起始高度
    public float spawnHeightOffset = 0.1f;   // 生成时沿法线向上偏移

    [Header("=== 调试与可视化 ===")]
    public bool showDebugGizmos = true;      // 是否在Scene视图中显示调试信息
    public Color gizmoColor = Color.red;     // Gizmos颜色

    // ---- 内部状态 ----
    private List<GameObject> spawnedZombies = new List<GameObject>(); // 本脚本刷新的僵尸列表
    private Coroutine spawnCoroutine;
    private Transform playerTransform;

    // ---- 数据缓存 ----
    private List<ZombieInfo> zombieInfoList;

    void Start()
    {
        // 从 GameDataMgr 获取僵尸数据
        zombieInfoList = GameDataMgr.Instance.ZombieInfos;
        if (zombieInfoList == null || zombieInfoList.Count == 0)
        {
            Debug.LogError("[ZombieSpawner] 未找到僵尸数据！请检查 GameDataMgr 中的 ZombieInfos 是否已加载。");
            return;
        }

        // 获取玩家引用
        if (SceneMgr.Instance.player == null)
        {
            Debug.LogError("[ZombieSpawner] 玩家引用为空！请确保 SceneMgr 中的 player 已赋值。");
            return;
        }
        playerTransform = SceneMgr.Instance.player.transform;

        // 启动刷新协程
        if (enableSpawn)
        {
            StartSpawning();
        }
    }

    void Update()
    {
        // 如果玩家引用丢失，尝试重新获取
        if (playerTransform == null && SceneMgr.Instance.player != null)
        {
            playerTransform = SceneMgr.Instance.player.transform;
        }

        // 清理已被销毁的僵尸引用（防止列表堆积）
        CleanupDestroyedZombies();
    }

    #region === 公开控制方法 ===

    /// <summary>
    /// 启用刷新
    /// </summary>
    public void EnableSpawn()
    {
        if (enableSpawn) return;
        enableSpawn = true;
        StartSpawning();
    }

    /// <summary>
    /// 禁用刷新（会停止生成新僵尸，但已有僵尸不受影响）
    /// </summary>
    public void DisableSpawn()
    {
        if (!enableSpawn) return;
        enableSpawn = false;
        StopSpawning();
    }

    /// <summary>
    /// 立即清除本脚本刷新的所有僵尸
    /// </summary>
    public void ClearAllSpawnedZombies()
    {
        foreach (GameObject zombie in spawnedZombies)
        {
            if (zombie != null)
                Destroy(zombie);
        }
        spawnedZombies.Clear();
    }

    /// <summary>
    /// 获取当前本脚本刷新的活着的僵尸数量
    /// </summary>
    public int GetAliveZombieCount()
    {
        CleanupDestroyedZombies();
        return spawnedZombies.Count;
    }

    #endregion

    #region === 内部逻辑 ===

    void StartSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (enableSpawn)
        {
            // 如果玩家丢失，等待下一帧
            if (playerTransform == null)
            {
                yield return new WaitForSeconds(spawnInterval);
                continue;
            }

            // 检查当前存活数量
            int aliveCount = GetAliveZombieCount();
            if (aliveCount < maxAliveZombies)
            {
                // 尝试生成一只僵尸
                bool spawned = TrySpawnZombie();
                if (spawned)
                {
                    Debug.Log($"[ZombieSpawner] 成功生成僵尸，当前存活: {GetAliveZombieCount()}/{maxAliveZombies}");
                }
                else
                {
                    Debug.LogWarning("[ZombieSpawner] 本次尝试未生成僵尸，可能是找不到合适位置或数据不足。");
                }
            }

            // 等待下一个刷新间隔
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    bool TrySpawnZombie()
    {
        if (zombieInfoList == null || zombieInfoList.Count == 0)
        {
            Debug.LogError("[ZombieSpawner] 僵尸数据列表为空，无法生成僵尸！");
            return false;
        }

        // 1. 寻找合适的生成位置
        Vector3 spawnPosition;
        if (!FindValidSpawnPosition(out spawnPosition))
        {
            Debug.LogWarning("[ZombieSpawner] 未找到合适的生成位置，跳过本次生成。");
            return false;
        }

        // 2. 从僵尸数据中随机选一个 
        ZombieInfo selectedInfo = zombieInfoList[Random.Range(0, zombieInfoList.Count)];
        if (selectedInfo == null)
        {
            Debug.LogWarning("[ZombieSpawner] 随机选中的僵尸数据为空！");
            return false;
        }

        // 3. 加载预制体（从 Resources 加载）
        // 注意：selectedInfo.res 应该是 Resources 文件夹下的相对路径，不带扩展名
        GameObject prefab = Resources.Load<GameObject>(selectedInfo.res);
        if (prefab == null)
        {
            Debug.LogWarning($"[ZombieSpawner] 加载预制体失败: {selectedInfo.res}");
            return false;
        }

        // 4. 生成僵尸
        GameObject zombie = Instantiate(prefab, spawnPosition, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

        // 5. 初始化僵尸数据
        ZombieConrtol controller = zombie.GetComponent<ZombieConrtol>();
        if (controller != null)
        {
            controller.Inti(selectedInfo);
        }
        else
        {
            Debug.LogWarning($"[ZombieSpawner] 僵尸预制体 {selectedInfo.res} 缺少 ZombieConrtol 组件！");
        }

        // 6. 添加到管理列表
        spawnedZombies.Add(zombie);

        return true;
    }

    bool FindValidSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;
        if (playerTransform == null)
            return false;

        Vector3 playerPos = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;

        // 最多尝试 30 次
        for (int attempt = 0; attempt < 30; attempt++)
        {
            // 1. 在球壳范围内随机生成一个候选点
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Clamp(randomDir.y, -0.3f, 0.3f);
            randomDir.Normalize();

            float distance = Random.Range(spawnMinRange, spawnMaxRange);
            Vector3 candidate = playerPos + randomDir * distance;

            // 2. 检查是否在玩家视野内
            Vector3 dirToCandidate = (candidate - playerPos).normalized;
            float angle = Vector3.Angle(playerForward, dirToCandidate);
            if (angle < playerViewAngle / 2f)
                continue;

            // 3. 检查是否有墙壁遮挡
            Vector3 eyePos = playerPos + Vector3.up * 1.5f;
            Vector3 dirToTarget = (candidate - eyePos).normalized;
            float distanceToTarget = Vector3.Distance(eyePos, candidate);
            if (!Physics.Raycast(eyePos, dirToTarget, out RaycastHit hit, distanceToTarget, wallLayerMask))
                continue; // 没有墙遮挡，不安全

            // 4. 向下检测地面
            Vector3 checkPos = candidate + Vector3.up * groundCheckHeight;
            if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit groundHit, groundCheckHeight + 5f))
            {
                Vector3 potentialPos = groundHit.point + groundHit.normal * spawnHeightOffset;

                // ⭐ 5. 新增：验证该位置是否在 NavMesh 上
                if (IsPositionOnNavMesh(potentialPos, out Vector3 validPos))
                {
                    spawnPos = validPos;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查一个位置是否在 NavMesh 上，如果是则返回最近的有效点
    /// </summary>
    bool IsPositionOnNavMesh(Vector3 position, out Vector3 validPosition)
    {
        validPosition = Vector3.zero;
        NavMeshHit hit;

        // SamplePosition 会在指定半径内查找最近的 NavMesh 点
        // 半径设大一些，以便在略微偏离 NavMesh 的位置也能找到有效点
        float maxDistance = 2f;
        if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
        {
            validPosition = hit.position;
            return true;
        }

        return false;
    }

    void CleanupDestroyedZombies()
    {
        // 移除所有已被销毁的僵尸引用
        spawnedZombies.RemoveAll(z => z == null);
    }

    #endregion

    #region === 编辑器可视化 ===

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || playerTransform == null)
            return;

        // 绘制刷新范围（两个球体）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, spawnMinRange);
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(playerTransform.position, spawnMaxRange);

        // 绘制视野锥（简易表示）
        Vector3 forward = playerTransform.forward;
        float halfAngle = playerViewAngle / 2f;
        Gizmos.color = Color.yellow;

        // 绘制视野左右边界（水平方向）
        Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawLine(playerTransform.position + Vector3.up * 1.5f,
                        playerTransform.position + Vector3.up * 1.5f + leftDir * spawnMaxRange);
        Gizmos.DrawLine(playerTransform.position + Vector3.up * 1.5f,
                        playerTransform.position + Vector3.up * 1.5f + rightDir * spawnMaxRange);

        // 绘制所有已生成僵尸的位置（如果有）
        Gizmos.color = Color.cyan;
        foreach (GameObject zombie in spawnedZombies)
        {
            if (zombie != null)
                Gizmos.DrawSphere(zombie.transform.position, 0.3f);
        }
    }

    #endregion
}