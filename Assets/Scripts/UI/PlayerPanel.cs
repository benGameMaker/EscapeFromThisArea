using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : BasePanel
{
    public TextMeshProUGUI HPText;
    public Image HPBack;
    public Image HPLine;

    public Image WeaponIcon;
    public Image KnifeIcon;
    public Image Separator;
    public TextMeshProUGUI CurCapacity;
    public TextMeshProUGUI MaxCapacity;

    public GameObject CapacityData;
    public Image SightBead;

    float maxHPwidth;

    // 当前绑定的武器，用于事件退订
    WeaponControl curWeapon;

    // 换弹进度协程
    Coroutine reloadProgressCoroutine;
    
    public override void Init()
    {

        maxHPwidth = HPBack.rectTransform.rect.width;
        //初始隐藏弹药数据，等WeaponSwitching切换武器后再显示
        if (CapacityData != null) CapacityData.SetActive(false);
    }
    public void UpdataHP(float HP,float maxHP)
    {
        //Debug.Log("跟新玩家HP值");
        HPText.text = HP.ToString("F1");
        //Debug.Log("HP: " + HP + " / maxHP: " + maxHP + "|| maxHPwidth: " + maxHPwidth);
        (HPLine.transform as RectTransform).sizeDelta = new Vector2((float)HP / maxHP * maxHPwidth, 50f);
    }
    public void UpdataCurCapacity(int cur)
    {

        CurCapacity.text = cur.ToString();
        // 弹药数量恢复（换弹完成），结束换弹进度显示
        EndReloadProgress();
    }
    public void ShowCapacityDataToUI(WeaponControl weaponControl)
    {
        //退订上一把武器，订阅当前武器的事件，实现实时刷新
        if (curWeapon != null) curWeapon.OnCapacityChanged -= UpdataCurCapacity;
        if (curWeapon != null) curWeapon.OnReloadStart -= StartReloadProgress;
        curWeapon = weaponControl;
        weaponControl.OnCapacityChanged += UpdataCurCapacity;
        weaponControl.OnReloadStart += StartReloadProgress;

        HideKnifeIcon();
        CapacityData.SetActive(true);
        MaxCapacity.text = weaponControl.capacity.ToString();
        CurCapacity.text = weaponControl.curCapacity.ToString();
        WeaponIcon.sprite = weaponControl.WeaponIcon;
        Separator.sprite = weaponControl.BulletIcon;
    }
    public void ShowKnifeIcon(Sprite knifeIcon)
    {
        if (knifeIcon == null) Debug.Log("刀图片丢失");
        HideCapacityDataUI();
        KnifeIcon.sprite = knifeIcon;
        KnifeIcon.gameObject.SetActive(true);
    }
    public void HideCapacityDataUI()
    {
        //退订当前武器，避免事件继续回调
        if (curWeapon != null)
        {
            curWeapon.OnCapacityChanged -= UpdataCurCapacity;
            curWeapon.OnReloadStart -= StartReloadProgress;
            curWeapon = null;
        }
        EndReloadProgress();
        CapacityData.SetActive(false);
    }
    public void HideKnifeIcon()
    {
        KnifeIcon.gameObject.SetActive(false);
    }

    // 换弹开始事件回调：按武器ReloadTime匀速将准星填充从0补到1
    void StartReloadProgress(float duration)
    {
        if (reloadProgressCoroutine != null) StopCoroutine(reloadProgressCoroutine);
        reloadProgressCoroutine = StartCoroutine(ReloadProgressCoroutine(Mathf.Max(0.01f, duration)));
    }

    IEnumerator ReloadProgressCoroutine(float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (SightBead != null) SightBead.fillAmount = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        SightBead.fillAmount = 1f;
        reloadProgressCoroutine = null;
    }

    // 换弹完成：停止进度并清空准星填充
    void EndReloadProgress()
    {
        if (reloadProgressCoroutine != null)
        {
            StopCoroutine(reloadProgressCoroutine);
            reloadProgressCoroutine = null;
        }
        if (SightBead != null) SightBead.fillAmount = 0f;
    }
}
