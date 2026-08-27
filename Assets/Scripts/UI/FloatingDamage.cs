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

    void Awake()
    {
        if (!isInitialized) Debug.Log("飘动伤害初始化失败");
    }

    /// <summary>
    /// 由调用方（如 BaseObject）在实例化后、生成文本前显式注入依赖，
    /// 避免内部重复查找 GameObject.Find("Canvas")，也保证 Awake 中未就绪时可被补齐。
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


    public void CreateFloatingDamageText(float damageValue, Vector3 worldPos, Vector2 movingDirection, float distance, float time)
    {
        if (!isInitialized)
        {
            Debug.LogError("FloatingDamage 初始化失败：缺少 TextMeshProUGUI 或 Canvas 依赖，放弃生成飘字。");
            return;
        }
        if (stochasticVariation)
        {
            float newX = worldPos.x + Random.Range(-axisOffset, axisOffset);
            float newY = worldPos.y + Random.Range(-axisOffset, axisOffset);
            float angle = Random.Range(-angleOffset, angleOffset);
            worldPos = new Vector3(newX, newY, worldPos.z);

            movingDirection = Quaternion.Euler(0, 0, angle) * movingDirection;
        }

        // 1. 世界坐标转屏幕像素
        Vector2 screenPoint = renderCamera.WorldToScreenPoint(worldPos);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                screenPoint,
                canvas.worldCamera,
                out Vector2 uiAnchoredPos
            ))
        {
            return;
        }

        DamageText.text = damageValue.ToString("F1");
        DamageTextRt.anchoredPosition = uiAnchoredPos;

        StartCoroutine(RectTransformMove(movingDirection, distance, time));
    }

    IEnumerator RectTransformMove(Vector2 direction, float distance, float time)
    {
        if (time <= 0 || DamageTextRt == null) yield break;

        Vector2 dir = direction.normalized;
        Vector2 startPos = DamageTextRt.anchoredPosition;
        Vector2 endPos = startPos + dir * distance;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            DamageTextRt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        DamageTextRt.anchoredPosition = endPos;
        Destroy(gameObject, existanceTime);
    }
}
