using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class WeaponSwitching : MonoBehaviour
{
    public List<WeaponControl> weaponLists;

    public GameObject Knife;
    public Sprite KnifeIcon;

    int lastWeaponKeyID;
    int curWeaponKeyID;

    WeaponControl curWeapon;
    Transform LeftHand;
    Transform RightHand;
    Transform weaponHolder;

    PlayerController pc;
    ColliderProcessor cp;
    Rig rigHands;
    PlayerPanel pp;

    void Start()
    {
        weaponHolder = transform.Find("WeaponHolder");
        pc = SceneManager.Instance.player;
        cp = GetComponentInChildren<ColliderProcessor>();
        LeftHand = transform.Find("WeaponHolder/LeftHand");
        RightHand = transform.Find("WeaponHolder/RightHand");
        rigHands = transform.Find("Rig_HandsIK").GetComponentInChildren<Rig>();
        pp = UIManager.Instance.GetPanel<PlayerPanel>();
        if (pp == null) Debug.LogWarning("WeaponSwitching获取面板失败");
        else
        {
            pp.HideCapacityDataUI();
            pp.ShowKnifeIcon(KnifeIcon);
            //Debug.Log("初始化武器数据到UI");
        }

        weaponLists.Clear();
        if (weaponHolder != null)
        {
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                WeaponControl wc = weaponHolder.GetChild(i).GetComponent<WeaponControl>();
                // 武器物体初始可能为inactive，Awake不会执行，需手动初始化弹匣
                wc.InitCapacity();
                weaponLists.Add(wc);
                Debug.Log(wc.name + "现有的子弹数为：" + wc.curCapacity);
            }
        }
    }

    void Update()
    {
        if (curWeapon != null)
        {
            Transform targetRight = curWeapon.transform.Find("RightHandIK");
            Transform targetLeft = curWeapon.transform.Find("LeftHandIK");

            if (targetRight != null && RightHand != null)
            {
                RightHand.SetPositionAndRotation(targetRight.position, targetRight.rotation);
            }
            if (targetLeft != null && LeftHand != null)
            {
                LeftHand.SetPositionAndRotation(targetLeft.position, targetLeft.rotation);
            }
        }
    }

    public void SwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            cp.animator.SetBool("Reload_b", false);
            if (cp == null || cp.animator == null || rigHands == null)
            {
                Debug.LogError(gameObject.name + " 组件未就绪，跳过切枪");
                return;
            }
            lastWeaponKeyID = curWeaponKeyID;

            if (int.TryParse(context.control.name, out curWeaponKeyID) && lastWeaponKeyID != curWeaponKeyID)
            {
                if (curWeaponKeyID >= 1 && curWeaponKeyID <= weaponLists.Count)
                {
                    bool isHoldWeapon = false;
                    if (curWeapon != null)
                    {
                        LayDownWeapon(curWeapon);
                        isHoldWeapon = true;
                    }

                    Knife.SetActive(false);
                    curWeapon = weaponLists[curWeaponKeyID - 1];
                    pc.weaponControl = curWeapon;
                    if (isHoldWeapon) StartCoroutine(DelayExecute(0.3f, curWeapon, TakeUpWeapon));
                    else TakeUpWeapon(curWeapon);
                }
            }
            else if (lastWeaponKeyID != curWeaponKeyID)
            {
                LayDownWeapon(weaponLists[lastWeaponKeyID - 1]);

                Knife.SetActive(true);
                pp.ShowKnifeIcon(KnifeIcon);
                pc.isAim = false;
                pc.AimingAction();
                curWeapon = null;
            }
        }
    }

    void TakeUpWeapon(WeaponControl weapon)
    {
        Debug.Log("下一把武器是：" + weapon.gameObject.name);

        pp.ShowCapacityDataToUI(weapon);

        rigHands.weight = 1f;
        weapon.gameObject.SetActive(true);
        cp.animator.SetInteger("WeaponType_int", weapon.gameObject.name[0] - '0');

        pc.isHoldingWeapon = true;

        // 切枪前处于瞄准状态时，拿起新武器后从普通持枪动作过渡回瞄准动作
        if (pc.isAim) pc.AimingAction();
    }

    void LayDownWeapon(WeaponControl Weapon)
    {
        cp.animator.SetInteger("WeaponType_int", 0);
        rigHands.weight = 0f;
        // 切枪时先回到普通持枪动作（rigAim=0/rigGen=1），作为瞄准切枪的过渡动作
        pc.rigAim.weight = 0f;
        pc.rigGen.weight = 1f;

        pc.isHoldingWeapon = false;

        pp.HideCapacityDataUI();

        StartCoroutine(DelayExecute(0.3f, Weapon, (Weapon) =>
        {
            Weapon.gameObject.SetActive(false);
        }));
    }

    IEnumerator DelayExecute(float time, WeaponControl weaponControl, Action<WeaponControl> callback)
    {
        yield return new WaitForSeconds(time);
        callback?.Invoke(weaponControl);
    }
}
