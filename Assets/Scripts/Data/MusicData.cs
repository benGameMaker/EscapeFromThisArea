using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicData
{
    public bool musicOpen = true;
    // 音效是否静音(true=静音)。注：旧存档字段名为soundOpen且语义混乱(实为"静音")，
    // 改名soundMuted后旧字段读不到，回退到默认false(不静音)，符合"默认有声音"的预期。
    public bool soundMuted = false;
    public float musicValue = 0.2f;
    public float soundValue = 0.2f;
}
