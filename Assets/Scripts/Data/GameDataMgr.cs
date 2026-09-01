using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataMgr
{
    static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;

    public MusicData musicData;
    public List<CellData> cellDatas;

    GameDataMgr()
    {
        musicData = JsonMgr.Instance.LoadData<MusicData>("MusicData");
        cellDatas = JsonMgr.Instance.LoadData<List<CellData>>("CellData");
        Debug.Log("GameDataMgr initialized. MusicData and CellData loaded.");
    }

    public void SaveMusicData()
    {
        JsonMgr.Instance.SaveData(musicData, "MusicData");
    }
    public void SaveCellData()
    {
        JsonMgr.Instance.SaveData(cellDatas, "CellData");
    }
}
