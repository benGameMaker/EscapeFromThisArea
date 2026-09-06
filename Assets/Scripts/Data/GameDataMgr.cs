using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameDataMgr
{
    static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;

    public MusicData musicData;
    public List<CellData> cellDatas;
    public List<ZombieInfo> ZombieInfos;

    GameDataMgr()
    {
        musicData = JsonMgr.Instance.LoadData<MusicData>("MusicData");
        cellDatas = JsonMgr.Instance.LoadData<List<CellData>>("CellData");
        ZombieInfos = JsonMgr.Instance.LoadData<List<ZombieInfo>>("ZombieInfo");
        //Debug.Log(ZombieInfos.Count);
        //foreach (var zomb in ZombieInfos) Debug.Log($"ZombieInfo: {zomb.id}, Health: {zomb.HP}, Speed: {zomb.speed}");
    }

    public void SaveMusicData()
    {
        JsonMgr.Instance.SaveData(musicData, "MusicData");
    }
    public void SaveCellData()
    {
        JsonMgr.Instance.SaveData(cellDatas, "CellData");
    }

    public void PlaySound(string resName)
    {
        GameObject musicObj = new GameObject();
        AudioSource a = musicObj.AddComponent<AudioSource>();
        a.clip = Resources.Load<AudioClip>(resName);
        a.volume = musicData.soundValue;
        a.mute = musicData.soundOpen;

        a.Play();

        GameObject.Destroy(musicObj, 1f);
    }
}
