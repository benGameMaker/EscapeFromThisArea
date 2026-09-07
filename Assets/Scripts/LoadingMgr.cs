using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingMgr : MonoBehaviour
{
    public static LoadingMgr Instance { get; private set; }

    public event Action<float, string> OnProgressUpdate;
    public event Action<string> OnLoadComplete;

    [Header("载入节奏")]
    [Tooltip("LoadPanel显示出来后，延迟多少秒才开始真正异步载入")]
    public float delayBeforeLoad = 1f;
    [Tooltip("场景载入完成后，再延迟多少秒才隐藏LoadPanel")]
    public float delayAfterLoad = 1f;

    private bool isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 带进度条的场景切换：先显示LoadPanel(进度0)→延时delayBeforeLoad秒→异步载入
    /// →载入完成后再停留delayAfterLoad秒→隐藏LoadPanel。
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    /// <param name="displayName">加载文本(如"开始游戏")</param>
    /// <param name="showIntroAfterLoad">载入结束、LoadPanel隐藏后是否立即弹出IntroPanel(主菜单点开始进GameScene用)</param>
    /// <param name="clearAllUI">切换场景是否要清空所有UI面板</param>
    public void LoadSceneAsync(string sceneName, string displayName = null, bool showIntroAfterLoad = false, bool clearAllUI = false)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneCoroutine(sceneName, displayName ?? sceneName, showIntroAfterLoad, clearAllUI));
    }

    IEnumerator LoadSceneCoroutine(string sceneName, string displayName, bool showIntroAfterLoad, bool clearAllUI = false)
    {
        isLoading = true;

        // 1. 先显示LoadPanel，进度初始化为0
        LoadPanel loadPanel = UIManager.Instance.ShowPanel<LoadPanel>();
        loadPanel.transform.SetAsLastSibling();   // 保证盖住其它面板
        loadPanel.SetLoadText(displayName);
        loadPanel.SetProgress(0f);
        OnProgressUpdate?.Invoke(0f, displayName);

        // 2. LoadPanel 显示 delayBeforeLoad 秒后，才开始真正场景载入
        yield return new WaitForSeconds(delayBeforeLoad);

        // 3. 异步载入（allowSceneActivation=false，期间驱动进度条）
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadPanel.SetProgress(progress);
            OnProgressUpdate?.Invoke(progress, displayName);
            yield return null;
        }

        loadPanel.SetProgress(1f);
        OnProgressUpdate?.Invoke(1f, displayName);

        // 4. 载入(实际加载到0.9即完成)后不着急切场景/隐藏，
        //    先停留 delayAfterLoad 秒（进度条保持100%，此时目标场景尚未激活，游戏未开始跑）
        yield return new WaitForSeconds(delayAfterLoad);

        // 5. 再激活场景
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;   // 等待场景真正完成切换

        // 场景激活后刷新环境光照：
        // 异步切换场景时Unity可能沿用上一场景的天空盒/环境光(RenderSettings)或未重新计算环境探针，
        // 导致新场景整体偏暗(Scene窗口也会同步变暗)。这里强制把新场景设为活动场景并重算环境光照。
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && SceneManager.GetActiveScene() != loadedScene)
            SceneManager.SetActiveScene(loadedScene);
        DynamicGI.UpdateEnvironment();

        // 场景激活时GameScene会创建HUD面板，重新把LoadPanel提到最上层盖住它们
        loadPanel.transform.SetAsLastSibling();

        // 6. 隐藏LoadPanel（淡出销毁）
        UIManager.Instance.HidePanel<LoadPanel>();
        if (clearAllUI)
        {
            Debug.Log("清空UI");
            UIManager.Instance.ClearAllUI();
        }

        // 7. LoadPanel 隐藏后立刻显示IntroPanel（仅"开始游戏"进入GameScene流程需要）
        if (showIntroAfterLoad)
        {
            UIManager.Instance.ShowPanel<IntroPanel>();
        }

        // 等LoadPanel淡出销毁完成，避免残留影响下次载入
        while (UIManager.Instance.GetPanel<LoadPanel>() != null) yield return null;

        isLoading = false;
        OnLoadComplete?.Invoke(sceneName);
    }
}
