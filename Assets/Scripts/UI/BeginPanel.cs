using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    public Button btnStart;
    public Button btnSetting;
    public Button btnQuit;
    public override void Init()
    {
        btnStart.onClick.AddListener(() =>
        {
            // 载入完成后显示IntroPanel（开场文本交互）
            UIManager.Instance.HidePanel<BeginPanel>();
            LoadingMgr.Instance.LoadSceneAsync("GameScene", "开始游戏", true);
            if (UIManager.Instance.GetPanel<SettingPanel>() != null) UIManager.Instance.HidePanel<SettingPanel>();
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
