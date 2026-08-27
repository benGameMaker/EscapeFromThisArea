using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class WeaponSwitching : MonoBehaviour
{
    public List<Transform> weaponLists;
    public TextCapacityUI capacityUI;

    public GameObject Knife;

    int lastWeaponKeyID;
    int curWeaponKeyID;

    Transform curWeapon;
    Transform LeftHand;
    Transform RightHand;
    Transform weaponHolder;

    PlayerController pc;
    ColliderProcessor cp;
    Rig rigHands;

    void Start()
    {
        weaponHolder = transform.Find("WeaponHolder");
        pc = GetComponentInChildren<PlayerController>();
        cp = GetComponentInChildren<ColliderProcessor>();
        LeftHand = transform.Find("WeaponHolder/LeftHand");
        RightHand = transform.Find("WeaponHolder/RightHand");
        rigHands = transform.Find("Rig_HandsIK").GetComponentInChildren<Rig>();
        if (capacityUI == null) capacityUI = FindObjectOfType<TextCapacityUI>();

        weaponLists.Clear();
        if (weaponHolder != null)
        {
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                weaponLists.Add(weaponHolder.GetChild(i));
            }
        }

        Debug.Log("当前手中武器有：");
        foreach (Transform t in weaponLists) Debug.Log(t.gameObject.name);
    }

    void Update()
    {
        if (curWeapon != null)
        {
            Transform targetRight = curWeapon.Find("RightHandIK");
            Transform targetLeft = curWeapon.Find("LeftHandIK");

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
                    pc.weaponControl = curWeapon.GetComponent<WeaponControl>();
                    if (isHoldWeapon) StartCoroutine(DelayExecute(0.3f, curWeapon, TakeUpWeapon));
                    else TakeUpWeapon(curWeapon);
                }
            }
            else if (lastWeaponKeyID != curWeaponKeyID)
            {
                LayDownWeapon(weaponLists[lastWeaponKeyID - 1]);

                Knife.SetActive(true);
                pc.isAim = false;
                pc.AimingAction();
                curWeapon = null;
            }
        }
    }

    void TakeUpWeapon(Transform weapon)
    {
        Debug.Log("下一把武器是：" + weapon.gameObject.name);

        if (capacityUI != null) capacityUI.GetCapacityDataToUI(weapon.GetComponent<WeaponControl>());

        rigHands.weight = 1f;
        weapon.gameObject.SetActive(true);
        cp.animator.SetInteger("WeaponType_int", weapon.gameObject.name[0] - '0');

        pc.isHoldingWeapon = true;
    }

    void LayDownWeapon(Transform Weapon)
    {
        cp.animator.SetInteger("WeaponType_int", 0);
        rigHands.weight = 0f;
        pc.rigAim.weight = 0f;
        pc.rigGen.weight = 1f;

        pc.isHoldingWeapon = false;

        if (capacityUI != null) capacityUI.HideUI();

        StartCoroutine(DelayExecute(0.3f, Weapon, (Weapon) =>
        {
            Weapon.gameObject.SetActive(false);
        }));
    }

    IEnumerator DelayExecute(float time, Transform transform, Action<Transform> callback)
    {
        yield return new WaitForSeconds(time);
        callback?.Invoke(transform);
    }

}
