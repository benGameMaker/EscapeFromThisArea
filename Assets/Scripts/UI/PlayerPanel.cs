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
    // 订阅的背包面板，用于OnDestroy退订
    BagPanel bagRef;
    // 换弹进度协程
    Coroutine reloadProgressCoroutine;
    
    public override void Init()
    {

        maxHPwidth = HPBack.rectTransform.rect.width;
        //初始隐藏弹药数据，等WeaponSwitching切换武器后再显示
        if (CapacityData != null) CapacityData.SetActive(false);
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
        //开火/换弹等弹药数量变化后，同步刷新背包中该武器子弹的余量(MaxCapacity)
        RefreshMaxCapacity();
        // 弹药数量恢复（换弹完成），结束换弹进度显示
        EndReloadProgress();
    }
    public void ShowCapacityDataToUI(WeaponControl wc)
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
        curWeapon = wc;
        wc.OnCapacityChanged += UpdataCurCapacity;
        wc.OnReloadStart += StartReloadProgress;

        HideToolIcon();
        CapacityData.SetActive(true);
        //CurCapacity=弹匣现有数量，MaxCapacity=该子弹在背包中的总数
        CurCapacity.text = wc.curCapacity.ToString();
        MaxCapacity.text = GetBagAmmoCount(wc).ToString();
        WeaponIcon.sprite = wc.WeaponIcon;
        Separator.sprite = wc.BulletIcon;
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
    //显示手持道具(刀/手雷等)的图标
    public void ShowToolIcon(Sprite icon)
    {
        if (icon == null) Debug.Log("道具图标丢失");
        HideCapacityDataUI();
        KnifeIcon.sprite = icon;
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
    public void HideToolIcon()
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
