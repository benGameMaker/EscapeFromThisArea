using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PromptPanel : BasePanel
{
    [Tooltip("显示多久后自行隐藏")]
    public float showTime;
    public TextMeshProUGUI txtPrompt;
    private Coroutine promptCoroutine;

    public override void Init()
    {
        
    }

    //如果调用者需要自定义提示面板的显示时间就传入大于0的custom的值
    public void ShowPromptText(string prompt, float customTime = 0)
    {
        txtPrompt.text = prompt;

        // 停止之前的协程
        if (promptCoroutine != null)
        {
            StopCoroutine(promptCoroutine);
            promptCoroutine = null;
        }

        // 启动新的协程
        promptCoroutine = StartCoroutine(ShowPromptCoroutine(customTime <= 0 ? showTime : customTime));
    }

    private IEnumerator ShowPromptCoroutine(float time)
    {
        ShowMe();
        yield return new WaitForSeconds(time);
        Debug.Log("隐藏提示面板");
        HideMe(() => {
            promptCoroutine = null;
        });
    }
}
