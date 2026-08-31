using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class VehiclePanel : BasePanel
{
    public Image IntegrityBack;
    public Image IntegrityLine;
    public TextMeshProUGUI IntegrityText;
    public TextMeshProUGUI VelocityText;

    public Image WeaponIcon;
    public Image Separator;
    public TextMeshProUGUI CurCapacity;
    public TextMeshProUGUI MaxCapacity;
    public GameObject CapacityData;
    public Image SightBead;

    float maxIntegritywidth;

    // 当前绑定的武器，用于事件退订
    WeaponControl curWeapon;

    // 换弹进度协程
    Coroutine reloadProgressCoroutine;

    public override void Init()
    {
        //初始隐藏弹药数据，等上车切换武器后再显示
        if (CapacityData != null) CapacityData.SetActive(true);
        //maxIntegritywidth = IntegrityBack.rectTransform.rect.width;
    }
    bool isInitialized;
    public void UpdataCurIntegrity(float curIntegrity, float maxIntegrity)
    {
        if (!isInitialized) maxIntegritywidth = IntegrityBack.rectTransform.rect.width;
        Debug.Log("更新车辆curIntegrity值");
        IntegrityText.text = curIntegrity.ToString();
        Debug.Log("curIntegrity: " + curIntegrity + " / maxIntegrity: " + maxIntegrity + "|| maxIntegritywidth: " + maxIntegritywidth);
        (IntegrityLine.transform as RectTransform).sizeDelta = new Vector2((float)curIntegrity / maxIntegrity * maxIntegritywidth, 50f);
    }
    public void UpdataCurCapacity(int cur)
    {
        CurCapacity.text = cur.ToString();
        // 弹药数量恢复（换弹完成），结束换弹进度显示
        EndReloadProgress();
    }
    public void UpdataCurVelocity(float cur)
    {
        VelocityText.text = cur.ToString("F1");
    }

    public void ShowCapacityDataToUI(WeaponControl weaponControl)
    {
        //退订上一把武器，订阅当前武器的事件，实现实时刷新
        if (curWeapon != null) curWeapon.OnCapacityChanged -= UpdataCurCapacity;
        if (curWeapon != null) curWeapon.OnReloadStart -= StartReloadProgress;
        curWeapon = weaponControl;
        weaponControl.OnCapacityChanged += UpdataCurCapacity;
        weaponControl.OnReloadStart += StartReloadProgress;

        CapacityData.SetActive(true);
        MaxCapacity.text = weaponControl.capacity.ToString();
        CurCapacity.text = weaponControl.curCapacity.ToString();
        WeaponIcon.sprite = weaponControl.WeaponIcon;
        Separator.sprite = weaponControl.BulletIcon;
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
        if (CapacityData != null) CapacityData.SetActive(false);
    }

    // 换弹开始事件回调：按武器ReloadTime匀速将准星填充从0补到1
    void StartReloadProgress(float duration)
    {
        if (reloadProgressCoroutine != null) StopCoroutine(reloadProgressCoroutine);
        reloadProgressCoroutine = StartCoroutine(ReloadProgressCoroutine(Mathf.Max(0.01f, duration)));
    }

    IEnumerator ReloadProgressCoroutine(float duration)
    {
        duration *= 2f;
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (SightBead != null) SightBead.fillAmount = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        if (SightBead != null) SightBead.fillAmount = 1f;
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
