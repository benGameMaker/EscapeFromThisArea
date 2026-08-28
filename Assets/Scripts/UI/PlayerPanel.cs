using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : BasePanel
{
    public TextMeshProUGUI HPText;
    public Image HPBack;
    public Image HPLine;

    public Image WeaponIcon;
    public Image Separator;
    public GameObject CapacityData;
    public TextMeshProUGUI CurCapacity;
    public TextMeshProUGUI MaxCapacity;

    float maxHPwidth;
    
    public override void Init()
    {
        maxHPwidth = (HPBack.transform as RectTransform).rect.width;
        Debug.Log(maxHPwidth);
        //初始隐藏弹药数据，等WeaponSwitching切换武器后再显示（避免依赖面板创建时机）
        if (CapacityData != null) CapacityData.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
    }

    public void UpdataHP(float HP,float maxHP)
    {
        Debug.Log("跟新玩家HP值");
        HPText.text = HP.ToString();
        Debug.Log("HP: " + HP + " / maxHP: " + maxHP + "|| maxHPwidth: " + maxHPwidth);
        (HPLine.transform as RectTransform).sizeDelta = new Vector2((float)HP / maxHP * maxHPwidth, 50f);
    }
    public void UpdataCurCapacity(int cur)
    {

        MaxCapacity.text = cur.ToString();
    }
    public void SetCapacityDataToUI(WeaponControl weaponControl)
    {
        CapacityData.SetActive(true);
        MaxCapacity.text = weaponControl.capacity.ToString();
        CurCapacity.text = weaponControl.curCapacity.ToString();
        WeaponIcon.sprite = weaponControl.WeaponIcon;
        Separator.sprite = weaponControl.BulletIcon;
    }
    public void HideCapacityDataUI()
    {
        CapacityData.SetActive(false);
    }
}
