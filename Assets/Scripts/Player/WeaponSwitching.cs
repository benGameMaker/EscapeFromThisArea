using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class WeaponSwitching : MonoBehaviour
{
    public List<WeaponControl> weaponLists;

    public GameObject Knife;      //刀模型
    public GameObject Grenade;    //手雷模型
    public Sprite KnifeIcon;      //刀图标
    public Sprite GrenadeIcon;    //手雷图标

    WeaponControl curWeapon;      //当前手持的枪，为null表示手持道具(刀/手雷)
    GameObject curTool;           //当前手持的非枪道具

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
        pc = SceneMgr.Instance.player;
        cp = GetComponentInChildren<ColliderProcessor>();
        LeftHand = transform.Find("WeaponHolder/LeftHand");
        RightHand = transform.Find("WeaponHolder/RightHand");
        rigHands = transform.Find("Rig_HandsIK").GetComponentInChildren<Rig>();
        pp = UIManager.Instance.GetPanel<PlayerPanel>();
        if (pp == null) Debug.LogWarning("PlayerPanel获取面板失败");
        weaponLists.Clear();
        if (weaponHolder != null)
        {
            for (int i = 0; i < weaponHolder.childCount; i++)
            {
                WeaponControl wc = weaponHolder.GetChild(i).GetComponent<WeaponControl>();
                // 武器物体初始可能为inactive，Awake不会执行，需手动初始化弹匣
                if (wc != null)
                {
                    wc.InitCapacity();
                    weaponLists.Add(wc);
                    //Debug.Log(wc.name + "现有的子弹数为：" + wc.curCapacity);
                }
            }
        }

        // 初始默认持刀
        curTool = Knife;
        Knife.SetActive(true);
        Grenade.SetActive(false);
        if (pp != null)
        {
            pp.HideCapacityDataUI();
            pp.ShowToolIcon(KnifeIcon);
        }
    }

    void Update()
    {
        if (curWeapon == null) return;

        Transform targetRight = curWeapon.transform.Find("RightHandIK");
        Transform targetLeft = curWeapon.transform.Find("LeftHandIK");
        if (targetRight != null && RightHand != null)
            RightHand.SetPositionAndRotation(targetRight.position, targetRight.rotation);
        if (targetLeft != null && LeftHand != null)
            LeftHand.SetPositionAndRotation(targetLeft.position, targetLeft.rotation);
    }

    // 输入分发：数字键切枪，非数字键按按键名切换到对应道具
    public void SwitchWeapon(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (cp == null || cp.animator == null || rigHands == null)
        {
            Debug.LogError(gameObject.name + " 组件未就绪，跳过切枪");
            return;
        }
        // 切换武器是唯一允许打断换弹的行为：复位武器换弹状态并清掉准星换弹进度
        if (curWeapon != null && curWeapon.isReloading)
        {
            curWeapon.CancelReload();
            if (pp != null) pp.EndReloadProgress();
        }
        cp.animator.SetBool("Reload_b", false);

        if (int.TryParse(context.control.name, out int gunID))
            SwitchToGun(gunID);
        else
            SwitchToolByKey(context.control.name);
    }

    // 通用：非数字键按按键名分发到对应道具(Q=刀、G=手雷等)，后续新增道具在此加case即可
    void SwitchToolByKey(string controlName)
    {
        switch (controlName.ToLower())
        {
            case "q":
                EquipTool(Knife, KnifeIcon);   // 刀
                break;
            case "g":
                // 手雷：背包中必须有名为"Grenade"的物品才切换，否则无反应
                BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
                if (bag != null && bag.itemsNum.GetValueOrDefault("Grenade") > 0)
                    EquipTool(Grenade, GrenadeIcon);
                break;
        }
    }

    // 切换到编号为gunID(1~N)的枪械（public：治疗完成后恢复之前持枪状态用）
    public void SwitchToGun(int gunID)
    {
        if (gunID < 1 || gunID > weaponLists.Count) return;
        WeaponControl target = weaponLists[gunID - 1];
        if (target == curWeapon) return;

        bool wasHoldingGun = curWeapon != null;
        if (wasHoldingGun)
        {
            // 放下旧枪，延迟0.3s(等收枪)再拿起新枪
            LayDownWeapon(curWeapon);
        }
        else
        {
            // 当前持刀/手雷，先收起道具
            HideTool();
        }

        curWeapon = target;
        pc.curWeapon = target;

        if (wasHoldingGun)
            StartCoroutine(DelayExecute(0.3f, target, TakeUpWeapon));
        else
            TakeUpWeapon(target);
    }

    // 通用：手持指定的非枪道具(刀/手雷)
    void EquipTool(GameObject toolModel, Sprite toolIcon)
    {
        if (curTool == toolModel) return; // 已是该道具，无需重复切换

        if (curWeapon != null)
            LayDownWeapon(curWeapon);

        curWeapon = null;
        curTool = toolModel;

        // 手持道具(刀/手雷)不使用双手IK，始终为0。
        // 治疗动画是手K、需要rigHands=1，治疗结束/被打断后切回道具时必须归零（否则残留1）
        if (rigHands != null) rigHands.weight = 0f;

        // 道具身上若有刚体(如手雷)，拿在手上时必须保持kinematic，否则会因重力掉落
        SetModelKinematic(Knife, Knife == toolModel);
        SetModelKinematic(Grenade, Grenade == toolModel);

        // 只显示目标道具
        Knife.SetActive(Knife == toolModel);
        Grenade.SetActive(Grenade == toolModel);

        pp.ShowToolIcon(toolIcon);
        pc.isAim = false;
        pc.AimingAction();
    }

    // 设置模型及其子物体上所有刚体的kinematic状态(拿在手上的模型应冻结物理，避免受重力掉落)
    void SetModelKinematic(GameObject model, bool kinematic)
    {
        if (model == null) return;
        Rigidbody[] rbs = model.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++) rbs[i].isKinematic = kinematic;
    }

    // 收起当前手持道具(切枪/投掷后调用)
    public void HideTool()
    {
        Knife.SetActive(false);
        Grenade.SetActive(false);
        curTool = null;
    }

    //---------------------- 治疗/道具使用流程使用的公开接口 ----------------------

    // 当前是否正拿着枪或道具（治疗流程用它检测"玩家是否切枪/切刀/切雷而打断"）
    public bool IsHoldingWeaponTool => curWeapon != null || curTool != null;

    // 当前手持枪在weaponLists中的序号(1~N)，未持枪时为0
    public int CurGunIndex => curWeapon == null ? 0 : weaponLists.IndexOf(curWeapon) + 1;

    // 立刻收起当前枪/道具，进入徒手待机（模型立即隐藏不走0.3s延迟，避免与治疗动画冲突）
    public void HolsterAll()
    {
        if (curWeapon != null)
        {
            curWeapon.CancelReload();   // 收枪同样打断换弹，避免换弹状态残留
            curWeapon.gameObject.SetActive(false);
            pp.HideCapacityDataUI();
            //因为是手k的动画，所以收枪后rigHands要立即恢复为1，手才会动起来
            rigHands.weight = 1f;
            pc.rigAim.weight = 0f;
            pc.rigGen.weight = 1f;
            pc.isHoldingWeapon = false;
            pc.curWeapon = null;
            curWeapon = null;
        }
        else
        {
            HideTool();
            rigHands.weight = 1f;
        }
        pc.isAim = false;
        if (pc.animator != null) pc.animator.SetInteger("WeaponType_int", 0);
    }

    // 恢复为手持刀（治疗完成后默认切回）
    public void EquipKnife()
    {
        EquipTool(Knife, KnifeIcon);
    }

    // 恢复为手持手雷（治疗完成时若此前持雷且背包仍有雷）
    public void EquipGrenade()
    {
        EquipTool(Grenade, GrenadeIcon);
    }

    //---------------------- 手雷投掷会话与道具攻击输入 ----------------------

    //是否手持手雷(当前道具为手雷，用于区分投掷与近战)
    public bool IsHoldingGrenade => curTool == Grenade;

    //背包中是否还有手雷(实时获取背包，避免缓存引用失效导致误判)
    bool HasGrenadeInBag()
    {
        BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
        return bag != null && bag.itemsNum.GetValueOrDefault("Grenade") > 0;
    }

    //道具攻击按下(未持枪时Fire.performed)：手持手雷播放投掷动画(10)，否则播放近战攻击(12)
    public void OnToolAttackStart()
    {
        cp.animator.SetInteger("WeaponType_int", IsHoldingGrenade ? 10 : 12);
    }

    //道具攻击松开(未持枪时Fire.canceled)：回到待机(0)
    public void OnToolAttackEnd()
    {
        cp.animator.SetInteger("WeaponType_int", 0);
    }

    //手雷已投出(由动画事件ThrowingObjects生成完投掷物后调用)：
    //扣除背包一颗手雷并返回是否扣减成功(失败=背包已空)。成功后才允许本轮"投掷动画"发生，
    //模型显隐统一交给ThrowingOver收尾(有雷装回下一颗/无雷切刀)，避免中途状态与动画错位
    public bool OnGrenadeThrown()
    {
        BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
        bool deducted = bag != null && bag.ConsumeItem("Grenade", 1);
        //先隐藏手中手雷(无论是否还有，等ThrowingOver决定装填还是切刀)
        if (Grenade != null) Grenade.SetActive(false);
        return deducted;
    }

    //投掷动画末尾(由动画事件ThrowingOver调用)：
    //仍处手雷会话时：背包有雷->把手雷模型放回手中(可继续连投)；没有->切回持刀并强制隐藏手雷。
    //若鼠标左键仍按住，立即发起下一次攻击(续投或近战12)，保证衔接流畅
    public void OnThrowOver()
    {
        if (IsHoldingGrenade)
        {
            if (HasGrenadeInBag())
            {
                //装回手中的手雷，为下一次投掷做准备
                if (Grenade != null) Grenade.SetActive(true);
            }
            else
            {
                //手雷投完：强制隐藏手雷并切回持刀状态(双保险，杜绝模型残留)
                if (Grenade != null) Grenade.SetActive(false);
                EquipTool(Knife, KnifeIcon);
            }
        }

        //按住左键不松且仍处于道具模式(未持枪)：紧接下一次攻击
        if (curWeapon == null && Input.GetMouseButton(0))
        {
            if (IsHoldingGrenade && HasGrenadeInBag()) cp.animator.SetInteger("WeaponType_int", 10);   //继续投掷
            else if (!IsHoldingGrenade) cp.animator.SetInteger("WeaponType_int", 12);                  //近战攻击
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
        if (Weapon != null && Weapon.isReloading) Weapon.CancelReload();  // 放下旧枪打断其换弹
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
