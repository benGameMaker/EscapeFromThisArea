using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class IntroPanel : BasePanel
{
    [Header("UI 组件")]
    public TMP_Text storyText;      // 剧情文本组件
    public Button clickButton;      // 全屏点击按钮

    [Header("配置参数")]
    public string[] storyLines;     // 剧情数组
    public float typingSpeed = 0.1f;  // 打字速度（秒/字）
    public float autoDelay = 1.0f;    // 每句话打完后的停顿时间(秒)

    // 内部状态
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine delayCoroutine;
    private string currentFullText = "";

    // 开场期间冻结游戏，交互完才恢复
    private bool inputFrozen = false;
    private readonly List<ZombieSpawner> frozenSpawners = new List<ZombieSpawner>();

    public override void Init()
    {
        if (storyLines == null || storyLines.Length == 0)
        {
            Debug.LogError("IntroPanel: 剧情数组为空！");
            // 避免面板黑屏卡住，直接结束开场
            FinishStoryAndExit();
            return;
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnScreenClick);

        // 开场需要鼠标点击交互：释放并显示鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 开场期间冻结游戏（玩家输入/刷怪），交互结束后才正式开始游戏
        FreezeGame();

        ShowNextLine();
    }

    /// <summary>
    /// 屏幕点击处理逻辑
    /// </summary>
    private void OnScreenClick()
    {
        // 情况1：如果正在打字，点击则瞬间显示全文字，并进入等待期
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            storyText.text = currentFullText;
            isTyping = false;

            // 手动跳完打字后，判断是否为最后一句
            if (currentLineIndex < storyLines.Length - 1)
            {
                StartAutoDelay();
            }
            return;
        }

        // 情况2：如果没在打字（说明正在 1s 等待期），点击则跳过等待，直接下一句
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }

        ProceedToNext();
    }

    /// <summary>
    /// 推进剧情逻辑
    /// </summary>
    private void ProceedToNext()
    {
        currentLineIndex++;

        if (currentLineIndex < storyLines.Length)
        {
            ShowNextLine();
        }
        else
        {
            FinishStoryAndExit();
        }
    }

    /// <summary>
    /// 显示下一行文字并开始打字
    /// </summary>
    private void ShowNextLine()
    {
        currentFullText = storyLines[currentLineIndex];
        storyText.text = "";
        isTyping = true;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeWriterEffect());
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private IEnumerator TypeWriterEffect()
    {
        for (int i = 0; i <= currentFullText.Length; i++)
        {
            storyText.text = currentFullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;

        // 如果不是最后一句，才启动自动等待
        if (currentLineIndex < storyLines.Length - 1)
        {
            StartAutoDelay();
        }
        // 如果是最后一句，则什么都不做，静静等待玩家点击
    }

    /// <summary>
    /// 开启自动等待协程
    /// </summary>
    private void StartAutoDelay()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(WaitForAutoNext());
    }

    private IEnumerator WaitForAutoNext()
    {
        yield return new WaitForSecondsRealtime(autoDelay);
        delayCoroutine = null;
        ProceedToNext();
    }

    /// <summary>
    /// 开场结束：不再切换场景。在GameScene中直接恢复游戏、隐藏本面板，
    /// 锁定鼠标后即正式开始游玩。
    /// </summary>
    private void FinishStoryAndExit()
    {
        clickButton.interactable = false;

        UnfreezeGame();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UIManager.Instance.HidePanel<IntroPanel>();
    }

    /// <summary>
    /// 冻结游戏：禁用玩家输入、暂停已开启的刷怪器
    /// </summary>
    private void FreezeGame()
    {
        inputFrozen = false;
        frozenSpawners.Clear();

        PlayerController player = SceneMgr.Instance != null ? SceneMgr.Instance.player : null;
        if (player != null)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
                inputFrozen = true;
            }
        }

        // 若场景中已有启用的刷怪器则暂停（GameScene中刷怪器默认关闭）
        ZombieSpawner[] spawners = FindObjectsOfType<ZombieSpawner>();
        foreach (ZombieSpawner spawner in spawners)
        {
            if (spawner != null && spawner.enableSpawn)
            {
                spawner.DisableSpawn();
                frozenSpawners.Add(spawner);
            }
        }
    }

    /// <summary>
    /// 恢复游戏：重新启用玩家输入、恢复被暂停的刷怪器
    /// </summary>
    private void UnfreezeGame()
    {
        PlayerController player = SceneMgr.Instance != null ? SceneMgr.Instance.player : null;
        if (player != null)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null && inputFrozen)
            {
                playerInput.enabled = true;
                inputFrozen = false;
            }
        }

        foreach (ZombieSpawner spawner in frozenSpawners)
        {
            if (spawner != null) spawner.EnableSpawn();
        }
        frozenSpawners.Clear();
    }
}
