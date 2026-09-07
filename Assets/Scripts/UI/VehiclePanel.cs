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
    public GameObject VehicleDamagedNotice;

    public Image WeaponIcon;
    public Image Separator;
    public TextMeshProUGUI CurCapacity;
    public TextMeshProUGUI MaxCapacity;
    public GameObject CapacityData;
    public Image SightBead;

    float maxIntegritywidth;

    // 当前绑定的武器，用于事件退订
    WeaponControl curWeapon;
    // 订阅的背包面板，用于OnDestroy退订
    BagPanel bagRef;

    // 换弹进度协程
    Coroutine reloadProgressCoroutine;

    public override void Init()
    {
        //初始隐藏弹药数据，等上车切换武器后再显示
        if (CapacityData != null) CapacityData.SetActive(true);
        //maxIntegritywidth = IntegrityBack.rectTransform.rect.width;
        //订阅背包数据变化，背包物品数量变化(如拾取/消耗子弹)时实时刷新HUD的余量
        bagRef = UIManager.Instance.GetPanel<BagPanel>();
        if (bagRef != null) bagRef.OnBagItemsChanged += OnBagItemsChanged;
    }
    void OnDestroy()
    {
        if (bagRef != null) bagRef.OnBagItemsChanged -= OnBagItemsChanged;
    }
    //背包数据变化：刷新当前武器子弹的余量显示
    void OnBagItemsChanged()
    {
        RefreshMaxCapacity();
    }
    bool isInitialized;
    public void UpdataCurIntegrity(float curIntegrity, float maxIntegrity)
    {
        if (!isInitialized) maxIntegritywidth = IntegrityBack.rectTransform.rect.width;
        //Debug.Log("更新车辆curIntegrity值");
        IntegrityText.text = curIntegrity.ToString("F1");
        //Debug.Log("curIntegrity: " + curIntegrity + " / maxIntegrity: " + maxIntegrity + "|| maxIntegritywidth: " + maxIntegritywidth);
        (IntegrityLine.transform as RectTransform).sizeDelta = new Vector2((float)curIntegrity / maxIntegrity * maxIntegritywidth, 50f);
    }
    public void UpdataCurCapacity(int cur)
    {
        CurCapacity.text = cur.ToString();
        //开火/换弹等弹药数量变化后，同步刷新背包中该武器子弹的余量(MaxCapacity)
        RefreshMaxCapacity();
        // 弹药数量恢复（换弹完成），结束换弹进度显示
        EndReloadProgress();
    }
    public void UpdataCurVelocity(float cur)
    {
        VelocityText.text = cur.ToString("F1");
    }

    public void ShowCapacityDataToUI(WeaponControl weaponControl)
    {
        //确保已订阅背包数据变化事件(面板可能比背包更早初始化)
        if (bagRef == null)
        {
            bagRef = UIManager.Instance.GetPanel<BagPanel>();
            if (bagRef != null) bagRef.OnBagItemsChanged += OnBagItemsChanged;
        }
        //退订上一把武器，订阅当前武器的事件，实现实时刷新
        if (curWeapon != null) curWeapon.OnCapacityChanged -= UpdataCurCapacity;
        if (curWeapon != null) curWeapon.OnReloadStart -= StartReloadProgress;
        curWeapon = weaponControl;
        weaponControl.OnCapacityChanged += UpdataCurCapacity;
        weaponControl.OnReloadStart += StartReloadProgress;

        CapacityData.SetActive(true);
        //CurCapacity=弹匣现有数量，MaxCapacity=该子弹在背包中的总数
        CurCapacity.text = weaponControl.curCapacity.ToString();
        MaxCapacity.text = GetBagAmmoCount(weaponControl).ToString();
        WeaponIcon.sprite = weaponControl.WeaponIcon;
        Separator.sprite = weaponControl.BulletIcon;
    }
    //背包中指定武器所用子弹的总数量
    int GetBagAmmoCount(WeaponControl wc)
    {
        if (wc == null) return 0;
        BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
        return bag != null ? bag.itemsNum.GetValueOrDefault(wc.bulletTypeName) : 0;
    }
    //用当前绑定武器的子弹类型刷新MaxCapacity(背包余量)
    void RefreshMaxCapacity()
    {
        if (curWeapon == null) return;
        MaxCapacity.text = GetBagAmmoCount(curWeapon).ToString();
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
    //通知面板通知玩家已经车辆损毁，需要维修
    public void ShowOrHideNotice(bool IsOrNo)
    {

        if (IsOrNo) VehicleDamagedNotice.SetActive(true);
        else VehicleDamagedNotice.SetActive(false);
        //Debug.Log("通知面板：" + VehicleDamagedNotice.activeSelf);
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

    // 换弹完成：停止进度并清空准星填充（public：切枪打断换弹时由VehicleController调用）
    public void EndReloadProgress()
    {
        if (reloadProgressCoroutine != null)
        {
            StopCoroutine(reloadProgressCoroutine);
            reloadProgressCoroutine = null;
        }
        if (SightBead != null) SightBead.fillAmount = 0f;
    }
}
