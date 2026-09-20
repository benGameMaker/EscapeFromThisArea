using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreatItem : BaseItemObjData
{
    [Tooltip("治疗动画持续时长(秒)")]
    public float treatTime = 4f;
    [Tooltip("每次治疗回复的生命值")]
    public float treatValue = 20f;
    [Tooltip("收起武器进入徒手待机后，到开始播放治疗动画的过渡时间(秒)")]
    public float poseTransitionTime = 0.3f;

    // 全局只允许同时进行一次治疗（玩家可再次使用治疗物品时需要等上一次结束）
    static bool isTreating;

    public override void UseBahaivor(int Integer = 0)
    {
        PlayerController pc = SceneMgr.Instance.player;
        if (pc == null) return;

        // 前置拦截：这几类情况不开始治疗、不消耗物品
        if (pc.isDead)
        {
            ShowPrompt("玩家已死亡，无法治疗");
            return;
        }
        if (pc.vd != null)
        {
            ShowPrompt("驾驶中无法治疗");
            return;
        }
        if (pc.HP >= pc.maxHP - 0.01f)
        {
            ShowPrompt("生命值已满，无需治疗");
            return;
        }
        if (isTreating)
        {
            ShowPrompt("正在治疗中");
            return;
        }

        WeaponSwitching ws = pc.GetComponent<WeaponSwitching>();
        if (ws == null || pc.animator == null) return;

        isTreating = true;
        // 协程挂在玩家身上运行，背包开关与否都不影响它执行
        pc.StartCoroutine(TreatCoroutine(pc, ws, Integer));
    }

    IEnumerator TreatCoroutine(PlayerController pc, WeaponSwitching ws, int useCellIndex)
    {
        // 记录治疗前的持械状态，治疗完成后按原状态恢复
        int prevGunIndex = ws.CurGunIndex;       // >0 表示之前持枪
        bool prevGrenade = ws.IsHoldingGrenade;  // 之前手持手雷

        // 1. 收起枪/刀/雷，进入徒手待机（普通待机状态）
        ws.HolsterAll();

        // 2. 等待收枪/待机动作完成，期间若切枪/切刀/切雷则直接打断
        float wait = Mathf.Max(0f, poseTransitionTime);
        while (wait > 0f)
        {
            if (pc.isDead || ws.IsHoldingWeaponTool)
            {
                CancelTreat();
                yield break;
            }
            wait -= Time.deltaTime;
            yield return null;
        }

        PlayerPanel pp = UIManager.Instance.GetPanel<PlayerPanel>();

        // 3. 播放治疗动画，用换弹转圈进度显示同样的治疗时长
        pc.animator.SetBool("Treat_bandage_b", true);
        if (pp != null) pp.StartReloadProgress(treatTime);

        // 4. 等待治疗完成；期间(关闭背包后)切枪/切刀/切雷会打断治疗
        float timer = 0f;
        bool interrupted = false;
        while (timer < treatTime)
        {
            timer += Time.deltaTime;
            if (pc.isDead || ws.IsHoldingWeaponTool)
            {
                interrupted = true;
                break;
            }
            yield return null;
        }

        // 复位治疗动画与转圈进度（成功或被打断都需要复位）
        pc.animator.SetBool("Treat_bandage_b", false);
        if (pp != null) pp.EndReloadProgress();
        CancelTreat();

        // 被打断：不治疗、不消耗；切枪/切刀/切雷行为已接管玩家当前状态
        if (interrupted || pc.isDead)
            yield break;

        // 5. 治疗玩家并刷新血条
        pc.Treated(treatValue);
        if (pp != null) pp.UpdataHP(pc.HP, pc.maxHP);

        // 6. 消耗一个治疗道具
        BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
        if (bag != null) bag.ConsumeItem(itemName, 1, useCellIndex);

        // 7. 恢复到治疗前的手持状态
        if (prevGunIndex > 0)
        {
            ws.SwitchToGun(prevGunIndex);
        }
        else if (prevGrenade && bag != null && bag.itemsNum.GetValueOrDefault("Grenade") > 0)
        {
            ws.EquipGrenade();
        }
        else
        {
            ws.EquipKnife();
        }
    }

    void CancelTreat()
    {
        isTreating = false;
    }

    void ShowPrompt(string text)
    {
        UIManager.Instance.GetPanel<PromptPanel>().ShowPromptText(text);
    }
}
