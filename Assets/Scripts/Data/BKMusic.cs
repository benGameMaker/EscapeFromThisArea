using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BKMusic: MonoBehaviour
{
    static BKMusic instance;
    public static BKMusic Instance => instance;

    AudioSource bkSource;

    private void Awake()
    {
        instance = this;
        bkSource = GetComponent<AudioSource>();
        MusicData data = GameDataMgr.Instance.musicData;

        SetIsOpen(data.musicOpen);
        ChangeValue(data.musicValue);
    }

    public void SetIsOpen(bool isOpen)
    {
        bkSource.mute = !isOpen;
    }

    public void ChangeValue(float v)
    {
        bkSource.volume = v;
    }
}
