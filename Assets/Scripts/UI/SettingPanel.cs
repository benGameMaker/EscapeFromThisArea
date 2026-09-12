using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Button btnClose;
    public Toggle togMusic;
    public Toggle togSound;
    public Slider sliderMusic;
    public Slider sliderSound;

    public override void Init()
    {
        MusicData data = GameDataMgr.Instance.musicData;
        togMusic.isOn = data.musicOpen; togSound.isOn = !data.soundMuted;

        sliderMusic.value=data.musicValue; sliderSound.value = data.soundValue;

        btnClose.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.SaveMusicData();
            UIManager.Instance.HidePanel<SettingPanel>();
            Debug.Log("点击了设置面板的关闭按钮");
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
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void HideMe(UnityAction callback)
    {
        // 无论用关闭按钮、点"开始"进游戏还是切场景隐藏设置面板，都保存音频设置，避免改动丢失
        GameDataMgr.Instance.SaveMusicData();
        base.HideMe(callback);
    }
}
