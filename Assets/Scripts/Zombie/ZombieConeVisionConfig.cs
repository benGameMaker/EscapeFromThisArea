using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.AI;


public class ZombieConeVisionConfig : MonoBehaviour
{
    [Tooltip("最大检测半径")]
    public float detectionRadius = 10f;
    [Tooltip("最大追踪半径")]
    public float traceRadius = 15f;
    [Tooltip("检测时的锥形半角角度（度），总角度=半角*2）")]
    [Range(0, 180)]
    public float detectionHalfAngle = 45f;
    [Tooltip("追踪时的锥形半角角度（度），总角度=半角*2）")]
    [Range(0, 180)]
    public float traceHalfAngle = 90f;
    [Tooltip("视野是否可以被障碍物遮挡")]
    public bool checkObstacle = true;
    [Tooltip("要追踪的目标")]
    public LayerMask TrackTargets;
    [Tooltip("忽略的障碍物层级")]
    public LayerMask ignoreObstacleLayer;

    public bool detectionVisualFieldVisualization = false;
    public bool traceVisualFieldVisualization = false;
    public Transform DetectionPoint;

    [HideInInspector]
    public float distance = -1f;
    [HideInInspector]

    NavMeshAgent _agent;
    public NavMeshAgent agent
    {
        get
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();
            return _agent;
        }
    }
    Transform TracekedTarget = null;

    // 是否暂停追逐/导航（僵尸已接触到车辆、原地攻击时由ZombieConrtol置位），
    // 暂停期间不刷新追踪目标，也不响应外部的NavigationToTargetPoint指令（如枪声吸引）
    public bool isNavigationPaused = false;

    private void Start()
    {
    }

    private void FixedUpdate()
    {
        if (!isNavigationPaused && TracekedTarget != null) ExecutionTraceTarget(TracekedTarget);
    }

    public void VisibilityDetection(Transform target)
    {
        // 避免重复计入同一个目标
        if (TracekedTarget != null) return;
        //Debug.Log("开始追踪" + target.name);
        if (checkObstacle)
        {
            // 发射射线检测是否有障碍物遮挡
            if (RaycastTag(target, detectionRadius))
            {
                TracekedTarget = target;
            }
            else
            {
                //Debug.Log("有遮挡，" + name + "没能看见" + target.name + "||" + target.tag);
            }
        }
        else
        {
            TracekedTarget = target;
        }
    }

    public void NavigationToTargetPoint(Vector3 targetPoint)
    {
        // 已暂停（如正在攻击车辆）时不接受新的导航目标
        if (isNavigationPaused) return;
        agent.isStopped = false;
        agent.SetDestination(targetPoint);
    }

    // 暂停追逐/导航（由ZombieConrtol在僵尸接触车辆开始攻击时调用）
    public void PauseChasing()
    {
        isNavigationPaused = true;
        if (agent != null) agent.isStopped = true;
    }

    // 恢复追逐/导航（僵尸离开车辆后由ZombieConrtol调用）
    public void ResumeChasing()
    {
        isNavigationPaused = false;
        if (agent != null) agent.isStopped = false;
    }

    // 外部强制切换持续追踪目标（车辆球形触发器吸引 / 上下车切换玩家与车辆时使用）：
    // 直接覆盖当前追踪目标，无需经过锥形视野判定；传 null 表示停止追踪。
    // TracekedTarget 约定指向"被控制实体的一个子物体（检测器DetectionZombies）"，
    // FixedUpdate 里会导航到 target.parent.position，从而自动跟随玩家当前控制的实体。
    public void ForceTraceTarget(Transform target)
    {
        // 解除贴车暂停，恢复导航（否则即使设了目标也不会停下来追击）
        isNavigationPaused = false;
        if (agent != null) agent.isStopped = false;

        TracekedTarget = target;
        if (target == null) distance = -1f;
    }
    //持续追踪目标，直至目标超出追踪范围或有障碍物遮挡视野
    public void ExecutionTraceTarget(Transform target)
    {
        Vector3 ZombieToTarget = target.position - DetectionPoint.position;
        distance = ZombieToTarget.magnitude;

        float dot = Vector3.Dot(DetectionPoint.forward, ZombieToTarget.normalized);
        float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

        if (ZombieToTarget.magnitude > traceRadius || angle > traceHalfAngle || !RaycastTag(target, traceRadius))
        {
            TracekedTarget = null;
            distance = -1f;
            //Debug.Log(gameObject.name + "跟丢玩家");
        }
        //Debug.Log(gameObject.name + "正在追踪玩家");
        NavigationToTargetPoint(target.parent.position);
    }

    bool RaycastTag(Transform target, float length)
    {
        RaycastHit hit;
        bool f =Physics.Raycast(
            DetectionPoint.position,
            target.position - DetectionPoint.position,
            out hit,
            length,
            TrackTargets
        );
        //Debug.Log("射线检测结果：" + f + "，碰撞物体：" + (hit.collider != null ? hit.collider.name : "无") + "，目标物体：" + target.name);
        return f;
    }

    #region 调试可视化（在Scene视图中绘制锥形）

    [Tooltip("检测锥体颜色")]
    public Color detectionGizmoColor = Color.green;
    [Tooltip("追踪锥体颜色")]
    public Color traceGizmoColor = Color.yellow;

    void OnDrawGizmosSelected()
    {
        // 以 DetectionPoint 为锥体中心，未指定时回退到物体自身位置
        Vector3 origin = DetectionPoint != null ? DetectionPoint.position : transform.position;
        Vector3 forward = DetectionPoint != null ? DetectionPoint.forward : transform.forward;

        if (detectionVisualFieldVisualization)
        {
            Gizmos.color = detectionGizmoColor;
            DrawCone(origin, forward, detectionRadius, detectionHalfAngle);
        }

        if (traceVisualFieldVisualization)
        {
            Gizmos.color = traceGizmoColor;
            DrawCone(origin, forward, traceRadius, traceHalfAngle);
        }
    }

    /// <summary>
    /// 绘制锥体辅助图形
    /// </summary>
    void DrawCone(Vector3 origin, Vector3 direction, float radius, float halfAngleDeg)
    {
        float halfAngleRad = halfAngleDeg * Mathf.Deg2Rad;
        float coneLength = radius;
        float coneRadius = Mathf.Tan(halfAngleRad) * coneLength;

        Vector3 forward = direction.normalized * coneLength;
        Vector3 up = Vector3.up;

        // 计算与forward垂直的轴
        Vector3 right = Vector3.Cross(direction, up).normalized;
        if (right == Vector3.zero)
        {
            right = Vector3.right;
        }
        up = Vector3.Cross(right, direction).normalized;

        // 绘制锥体线框
        int segments = 16;
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 2 * Mathf.PI;
            Vector3 circlePoint = origin + forward;
            circlePoint += (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * coneRadius;
            points[i] = circlePoint;
        }

        // 绘制底部圆环
        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }

        // 绘制从原点到底部圆的连线
        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(origin, points[i]);
        }

        // 绘制中心线
        Gizmos.DrawLine(origin, origin + forward);
    }

    #endregion
}
