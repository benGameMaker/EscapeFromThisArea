using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadPanel : BasePanel
{
    public Image LoadIcon;
    public Image imgProgressBar;
    public TextMeshProUGUI LoadText;

    [Header("旋转速度(度/秒)")]
    public float rotateSpeed = 90f;

    public override void Init()
    {
        // 进度条满宽与旋转均在运行时计算，无需在Init中预存
    }

    /// <summary>
    /// 刷新载入进度（0~1），通过改变进度条Image的宽度表现进度。
    /// 进度条锚定在轨道左侧(pivot.x=0)，宽度从0增长到轨道可用宽度。
    /// </summary>
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        RectTransform barRect = imgProgressBar.rectTransform;
        RectTransform trackRect = barRect.parent as RectTransform;

        // 满宽=轨道宽度扣除左右边距（左右各留同进度条原起始X偏移的间隙）
        float fullWidth = trackRect != null
            ? Mathf.Max(0f, trackRect.rect.width - barRect.anchoredPosition.x * 2f)
            : barRect.rect.width;

        Vector2 sizeDelta = barRect.sizeDelta;
        sizeDelta.x = fullWidth * progress;
        barRect.sizeDelta = sizeDelta;
    }

    protected override void Update()
    {
        base.Update();
        // 载入图标绕自身轴心(锚点/中心)匀速旋转
        if (isShow && LoadIcon != null)
        {
            LoadIcon.rectTransform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }
    }
}
