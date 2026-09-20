using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    public float T;
    public float D;
    public float existanceTime;
    [Tooltip("在起始位置进行小幅度偏离和偏向")]
    public bool stochasticVariation = true;
    public float axisOffset;
    public float angleOffset;

    TextMeshProUGUI DamageText;
    RectTransform DamageTextRt;

    Canvas canvas;
    RectTransform canvasRt;
    Camera renderCamera;

    bool isInitialized;
    // 是否正处于对象池中（防止重复回收）
    bool isInPool = true;
    /// <summary>
    /// 飘字生命周期结束后的回收回调，由对象池持有方（BaseObject）注入，替代直接Destroy
    /// </summary>
    [HideInInspector] public Action<GameObject> releaseCallback;

    // 跟随相关：飘字锚点 = 发出者位置 + baseWorldOffset + 一次性随机偏离 randomWorldOffset
    Transform followTarget;
    Vector3 baseWorldOffset;
    Vector3 randomWorldOffset;
    // 发出者被销毁时冻结在最后一个世界锚点继续播放
    Vector3 fixedAnchor;
    // 上浮（偏向）方向与距离、上浮时长
    Vector2 driftDirection;
    float driftDistance;
    float driftTime;

    /// <summary>
    /// 由调用方在实例化后、生成文本前显式注入画布位置参数依赖。
    /// </summary>
    public void InjectDependencies(Canvas targetCanvas, RectTransform targetCanvasRt, Camera camera = null)
    {
        canvas = targetCanvas;
        canvasRt = targetCanvasRt;
        renderCamera = camera;

        // TextMeshProUGUI 与 RectTransform 基于自身组件获取
        if (DamageText == null)
            DamageText = GetComponent<TextMeshProUGUI>();
        if (DamageTextRt == null && DamageText != null)
            DamageTextRt = DamageText.rectTransform;

        isInitialized = DamageText != null && DamageTextRt != null && canvas != null && canvasRt != null;
    }

    /// <summary>
    /// 生成飘字并持续跟随发出者的屏幕位置。返回false表示初始化失败，调用方应立即将其归还对象池。
    /// </summary>
    /// <param name="emitter">飘字跟随的发出者（受伤物体）；为空时固定在 worldPos 不再跟随</param>
    /// <param name="worldPos">初始世界锚点（通常为 发出者位置 + 头顶偏移）</param>
    public bool CreateFloatingDamageText(float damageValue, Transform emitter, Vector3 worldPos, Vector2 movingDirection, float distance, float time)
    {
        if (!isInitialized)
        {
            Debug.LogError("FloatingDamage 初始化失败：缺少 TextMeshProUGUI 或 Canvas 依赖，放弃生成飘字。");
            return false;
        }

        followTarget = emitter;
        driftDistance = distance;
        driftTime = time;

        // 一次性随机偏离与偏向：偏离作为相对发出者固定的世界偏移，偏向只改变上浮方向
        if (stochasticVariation)
        {
            randomWorldOffset = new Vector3(
                UnityEngine.Random.Range(-axisOffset, axisOffset),
                UnityEngine.Random.Range(-axisOffset, axisOffset),
                0f);
            float angle = UnityEngine.Random.Range(-angleOffset, angleOffset);
            movingDirection = Quaternion.Euler(0, 0, angle) * movingDirection;
        }
        else
        {
            randomWorldOffset = Vector3.zero;
        }
        driftDirection = movingDirection.normalized;

        if (followTarget != null)
        {
            // worldPos 已包含调用方给的固定头顶偏移，将其换算为相对发出者的偏移
            baseWorldOffset = worldPos - followTarget.position;
        }
        else
        {
            fixedAnchor = worldPos + randomWorldOffset;
        }

        DamageText.text = damageValue.ToString("F1");
        DamageText.enabled = true;

        isInPool = false;
        StartCoroutine(FloatingRoutine());
        return true;
    }

    // 飘字动画与停留结束后归还对象池，替代原来的Destroy
    void ReleaseToPool()
    {
        if (isInPool) return;
        isInPool = true;
        releaseCallback?.Invoke(gameObject);
    }

    IEnumerator FloatingRoutine()
    {
        // 总生命周期 = 上浮时间 + 停留时间，期间持续跟随发出者的屏幕位置
        float totalLifetime = driftTime + existanceTime;
        float elapsed = 0f;
        while (elapsed < totalLifetime)
        {
            elapsed += Time.deltaTime;
            float driftProgress = driftTime > 0f ? Mathf.Clamp01(elapsed / driftTime) : 1f;
            RefreshPosition(driftProgress);
            yield return null;
        }

        ReleaseToPool();
    }

    // 每帧根据发出者当前世界位置重算屏幕坐标，叠加随时间的上浮偏移；出屏或在相机背后时隐藏
    void RefreshPosition(float driftProgress)
    {
        Vector3 worldAnchor;
        if (followTarget != null)
        {
            worldAnchor = followTarget.position + baseWorldOffset + randomWorldOffset;
            // 缓存最后锚点：发出者中途被销毁时冻结在该位置继续播完
            fixedAnchor = worldAnchor;
        }
        else
        {
            worldAnchor = fixedAnchor;
        }

        Vector3 screenPoint = renderCamera.WorldToScreenPoint(worldAnchor);
        // z<=0 表示在相机背后；超出屏幕像素矩形表示不在视口内，两种情况都隐藏飘字
        bool inView = screenPoint.z > 0f
            && screenPoint.x >= 0f && screenPoint.x <= Screen.width
            && screenPoint.y >= 0f && screenPoint.y <= Screen.height;

        if (DamageText.enabled != inView)
            DamageText.enabled = inView;

        if (inView && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                screenPoint,
                canvas.worldCamera,
                out Vector2 uiAnchoredPos))
        {
            // 锚点跟随发出者，上浮偏移保持原有动画
            DamageTextRt.anchoredPosition = uiAnchoredPos + driftDirection * driftDistance * driftProgress;
        }
    }
}
