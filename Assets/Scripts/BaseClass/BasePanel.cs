using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class BasePanel : MonoBehaviour
{
    public bool isShow = false;

    float alphaSpeed = 10.0f;
    protected CanvasGroup canvasGroup;

    protected Camera cam;
    protected Canvas canvas;
    protected RectTransform loaclRt;
    protected UnityAction hideCallBack;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = UIManager.Instance.canvasTrans.GetComponent<Canvas>();
        cam = Camera.main.transform.GetComponent<Camera>();
        loaclRt = GetComponent<RectTransform>();
        //没有就通过代码自己获取
        if (canvasGroup == null) canvasGroup = gameObject.GetComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {
        Init();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //实现淡入淡出的效果
        if (isShow && canvasGroup.alpha != 1f)
        {
            canvasGroup.alpha += alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha >= 1f) canvasGroup.alpha = 1f;
        }else if (!isShow)
        {
            canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha <= 0)
            {
                canvasGroup.alpha = 0;
                hideCallBack?.Invoke();
            }
        }
    }
    public abstract void Init();
    public virtual void ShowMe()
    {
        isShow= true;
        canvasGroup.alpha = 0;
    }
    public virtual void HideMe(UnityAction callback)
    {
        isShow = false;
        canvasGroup.alpha = 1f;
        hideCallBack = callback;
    }
}
