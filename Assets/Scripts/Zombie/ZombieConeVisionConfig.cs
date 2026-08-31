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

    public bool detectionVisualFieldVisualization = false;
    public bool traceVisualFieldVisualization = false;
    public Transform DetectionPoint;

    [HideInInspector]
    public float distance = -1f;
    [HideInInspector]
    public NavMeshAgent agent;
    Transform TracekedTarget = null;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void FixedUpdate()
    {
        if (TracekedTarget != null) ExecutionTraceTarget(TracekedTarget);
    }

    public void VisibilityDetection(Transform target)
    {
        // 避免重复计入同一个目标
        if (TracekedTarget != null) return;
        Debug.Log("开始追踪" + target.name);
        if (checkObstacle)
        {
            // 发射射线检测是否有障碍物遮挡
            if (RaycastTag(target, detectionRadius))
            {
                TracekedTarget = target;
            }
            else
            {
                Debug.Log("有遮挡，" + name + "没能看见" + target.name + "||" + target.tag);
            }
        }
        else
        {
            TracekedTarget = target;
        }
    }

    public void NavigationToTargetPoint(Vector3 targetPoint)
    {
        agent.isStopped = false;
        agent.SetDestination(targetPoint);
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
            Debug.Log(gameObject.name + "跟丢玩家");
        }
        Debug.Log(gameObject.name + "正在追踪玩家");
        NavigationToTargetPoint(target.parent.position);
    }

    bool RaycastTag(Transform target, float length)
    {
        RaycastHit hit;
        return Physics.Raycast(
            DetectionPoint.position,
            target.position - DetectionPoint.position,
            out hit,
            length,
            TrackTargets
        );
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
