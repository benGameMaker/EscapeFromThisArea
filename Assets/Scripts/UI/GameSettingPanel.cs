using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettingPanel : BasePanel
{
    public Button btnClose;
    public Toggle togMusic;
    public Toggle togSound;
    public Slider sliderMusic;
    public Slider sliderSound;
    public Button btnRestart;
    public Button btnReturn;

    public override void Init()
    {
        MusicData data = GameDataMgr.Instance.musicData;
        togMusic.isOn = data.musicOpen; 
        togSound.isOn = !data.soundMuted;

        sliderMusic.value = data.musicValue; sliderSound.value = data.soundValue;

        btnClose.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.SaveMusicData();
            UIManager.Instance.HidePanel<GameSettingPanel>();
        });
        togMusic.onValueChanged.AddListener((v) =>
        {
            BKMusic.Instance.SetIsOpen(v);
            GameDataMgr.Instance.musicData.musicOpen = v;
        });
        togSound.onValueChanged.AddListener((v) =>
        {
            // 勾选(v=true)=开启音效，对应soundMuted=false；取反后存储
            GameDataMgr.Instance.musicData.soundMuted = !v;
        });
        sliderMusic.onValueChanged.AddListener((v) =>
        {
            BKMusic.Instance.ChangeValue(v);
            GameDataMgr.Instance.musicData.musicValue = v;
        });
        sliderSound.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.musicData.soundValue = v;
        });
        btnRestart.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.SaveMusicData();   // 重开前先保存设置
            string currentScene = SceneManager.GetActiveScene().name;
            UIManager.Instance.GetPanel<PlayerPanel>().gameObject.SetActive(true);
            LoadingMgr.Instance.LoadSceneAsync(currentScene, "重新开始布置场景中...", false, true);
        });
        btnReturn.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.SaveMusicData();   // 返回主菜单前先保存设置
            LoadingMgr.Instance.LoadSceneAsync("BeginScene", "正在返回主菜单...", false, true);
        });
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void ShowMe()
    {
        base.ShowMe();
        if (SceneMgr.Instance.pblp != null) SceneMgr.Instance.pblp.EnableGameplayInput(false);
        else SceneMgr.Instance.vblp.EnableGameplayInput(false);

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }
    public override void HideMe(UnityAction callback)
    {
        // 无论用关闭按钮还是按T键/切场景隐藏，都先保存音频设置，避免改动丢失
        GameDataMgr.Instance.SaveMusicData();
        base.HideMe(callback);
        if (SceneMgr.Instance.pblp != null) SceneMgr.Instance.pblp.EnableGameplayInput(true);
        else SceneMgr.Instance.vblp.EnableGameplayInput(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
