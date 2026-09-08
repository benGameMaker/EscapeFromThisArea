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
        togSound.isOn = data.soundOpen;

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
            GameDataMgr.Instance.musicData.soundOpen = !v;
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
            string currentScene = SceneManager.GetActiveScene().name;
            UIManager.Instance.GetPanel<PlayerPanel>().gameObject.SetActive(true);
            LoadingMgr.Instance.LoadSceneAsync(currentScene, "重新开始布置场景中...", false, true);
        });
        btnReturn.onClick.AddListener(() =>
        {
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
        base.HideMe(callback);
        if (SceneMgr.Instance.pblp != null) SceneMgr.Instance.pblp.EnableGameplayInput(true);
        else SceneMgr.Instance.vblp.EnableGameplayInput(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
