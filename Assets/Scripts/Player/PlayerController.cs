using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PlayerController : BaseObject
{
    [Tooltip("是否禁用动画对物体的位移影响")]
    public bool isStatic = false;

    [Tooltip("举枪耗时")]
    public float gunRaisingTime = 0.5f;
    public float bodyRotationMax_h = 0.5f;
    public float bodyRotationMax_v = 0.4f;
    public Vector3 genOffset = new Vector3(0.38f, -0.15f, -0.84f);
    public Vector3 aimOffset = new Vector3(0.7f, -0.15f, 1f);
    public Transform DiscardPoint;
    [Tooltip("投掷手雷时的初速度大小")]
    public float throwForce = 15f;
    [Tooltip("玩家受伤时播放的音频(不循环)")]
    public AudioClip hurtSound;
    [Tooltip("玩家死亡时播放的音频(不循环)")]
    public AudioClip deathSound;

    [HideInInspector]
    public bool isHoldingWeapon = false;
    [HideInInspector]
    public bool isAim = false;
    [HideInInspector]
    public WeaponControl curWeapon;
    [HideInInspector]
    public Rig rigAim;
    [HideInInspector]
    public Rig rigGen;
    [HideInInspector]
    public VehicleDamage vd;
    [SerializeField]
    Transform Body;
    CinemachineCameraOffset cco;
    bool isAligningAim = false;
    bool isMoving = false;
    bool isHipGunRaised = false;
    Coroutine delayedHolsterCoroutine = null;
    ColliderProcessor cp;
    MeleeRange mr;
    PlayerPanel pp;
    Transform mainCameraPos;

    void Start()
    {
        mainCameraPos = Camera.main.transform;
        mr = GetComponentInChildren<MeleeRange>();
        cp = GetComponentInChildren<ColliderProcessor>();
        curWeapon = GetComponentInChildren<WeaponControl>();
        rigAim = transform.Find("Rig_WeaponAim").GetComponentInChildren<Rig>();
        rigGen = transform.Find("Rig_WeaponPose").GetComponentInChildren<Rig>();
        cco = transform.Find("FreeLook Camera").GetComponent<CinemachineCameraOffset>();
        BodyParts.Add(transform.Find("Hips_jnt").GetComponent<BoxCollider>());
        pp = UIManager.Instance.GetPanel<PlayerPanel>();
        if (pp != null) pp.UpdataHP(HP, maxHP);
        else
        {
            Debug.LogWarning("PlayerPanel获取失败，尝试重试获取");
            UIManager.Instance.ShowPanel<PlayerPanel>();
            pp = UIManager.Instance.GetPanel<PlayerPanel>();
            if (pp != null)
            {   
                Debug.Log("PlayerPanel获取成功");
                pp.UpdataHP(HP, maxHP);
            }
            else Debug.LogWarning("PlayerPanel获取失败");
        }
    }

    protected override void Update()
    {
        animator.SetBool("Static_b", isStatic);

        if (isHoldingWeapon && curWeapon.curCapacity <= 0) animator.SetBool("Shoot_b", false);
        // 计算相机与物体在水平面上的夹角
        Vector3 camForwardFlat = mainCameraPos.forward;
        camForwardFlat.y = 0f;
        camForwardFlat.Normalize();

        Vector3 objForwardFlat = transform.forward;
        objForwardFlat.y = 0f;
        objForwardFlat.Normalize();

        float aimAngle = Vector3.Angle(camForwardFlat, objForwardFlat);

        if (isAim)
        {
            if (aimAngle > 60f)
                isAligningAim = true;

            if (isAligningAim)
            {
                if (aimAngle > 10f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(camForwardFlat, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    isAligningAim = false;
                }
            }
        }
        else
        {
            isAligningAim = false;
        }

        Turn(camForwardFlat, objForwardFlat);
        if (isMoving) Movement(camForwardFlat);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (isDead || !animator.enabled) return;
        moveVector = context.ReadValue<Vector2>();
        if (context.performed && animator.GetFloat("Speed_f") != 1f)
        {
            animator.SetFloat("Speed_f", (moveVector.y >= 0 ? 0.6f : 0.3f));
            isMoving = true;
        }
        else if (context.canceled)
        {
            animator.SetFloat("Speed_f", 0f);
            isMoving = false;
        }
    }
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Jump logic here
            cp.Jump();
        }
    }
    public void RunFast(InputAction.CallbackContext context)
    {
        if (!isMoving || moveVector.y <= 0) return;
        if (context.performed)
        {
            animator.SetFloat("Speed_f", 1f);
        }
        else if (context.canceled)
        {
            animator.SetFloat("Speed_f", (moveVector.y >= 0 ? 0.6f : 0.3f));
        }
    }
    public void Aim(InputAction.CallbackContext context)
    {
        if (!isHoldingWeapon) return;
        if (context.performed)
        {
            // 取消延迟收枪协程，避免与瞄准状态冲突
            if (delayedHolsterCoroutine != null)
            {
                StopCoroutine(delayedHolsterCoroutine);
                delayedHolsterCoroutine = null;
            }

            isAim = !isAim;

            // 进入瞄准：hip 举枪状态交给瞄准接管
            // 退出瞄准：hip 举枪状态重置为未举枪（因为 rigAim.weight 已被瞄准协程降到0）
            if (!isAim)
            {
                isHipGunRaised = false;
                animator.SetBool("Shoot_b", false);
            }

            AimingAction();
        }
    }

    //开火输入分发：持枪走枪械开火；未持枪(持刀/持手雷等道具)交给WeaponSwitching处理道具攻击
    bool isFire = false;   //举枪连发标记，收枪计时使用
    public void Fire(InputAction.CallbackContext context)
    {
        if (!isHoldingWeapon)
        {
            // 未持枪：由WeaponSwitching判断是投掷手雷(10)还是近战攻击(12)
            WeaponSwitching ws = GetComponent<WeaponSwitching>();
            if (ws != null)
            {
                if (context.performed) ws.OnToolAttackStart();
                else if (context.canceled) ws.OnToolAttackEnd();
            }
            return;
        }
        GunFire(context);
    }
    //持枪开火(原Fire中持枪逻辑)：处理空仓换弹、举枪开火与收枪计时
    void GunFire(InputAction.CallbackContext context)
    {
        if (curWeapon == null) return;

        // 换弹中：任何开火输入都忽略（不重置换弹进度、不尝试举枪/开火）
        if (curWeapon.isReloading)
        {
            // 松开时清掉开火/举枪标记，避免换弹完成后残留
            if (context.canceled)
            {
                isFire = false;
                animator.SetBool("Shoot_b", false);
            }
            return;
        }

        // 空仓：自动进入换弹（StartReload内部去重，已在换弹时不会重置换弹进度），
        // 其余流程照旧（举枪/开火姿态），打不出子弹也不会影响换弹
        if (curWeapon.curCapacity <= 0)
        {
            animator.SetBool("Reload_b", true);
            curWeapon.StartReload();
        }

        if (!isAim)
        {
            if (context.performed)
            {
                // 重置收枪时间
                if (delayedHolsterCoroutine != null)
                {
                    StopCoroutine(delayedHolsterCoroutine);
                    delayedHolsterCoroutine = null;
                }

                // 未举枪则先举枪，举枪完毕后开火；已举枪直接开火
                if (!isHipGunRaised)
                {
                    StartCoroutine(RaiseGunThenFireCoroutine());
                }
                else
                {
                    animator.SetBool("Shoot_b", true);
                }
            }

            if (context.canceled)
            {
                isFire = false;
                animator.SetBool("Shoot_b", false);
                // 松开后5秒内未再次按下则收枪
                if (delayedHolsterCoroutine != null)
                    StopCoroutine(delayedHolsterCoroutine);
                delayedHolsterCoroutine = StartCoroutine(DelayedHolsterCoroutine(5f));
            }
        }
        else
        {
            if (context.performed)
            {
                animator.SetBool("Shoot_b", true);
            }

            if (context.canceled)
            {
                animator.SetBool("Shoot_b", false);
            }
        }
    }
    public void Reload(InputAction.CallbackContext context)
    {
        if (!context.canceled) return;
        // 未持枪或已在换弹中：重复按换弹键忽略，不重置换弹进度、不打断换弹
        if (!isHoldingWeapon || curWeapon == null || curWeapon.isReloading) return;

        animator.SetBool("Reload_b", true);
        curWeapon.StartReload();
    }

    // 打断徒步换弹（上车切换归属等场景使用）：复位武器换弹状态、退出换弹动画并清掉准星进度
    public void CancelReload()
    {
        if (curWeapon != null) curWeapon.CancelReload();
        if (animator != null) animator.SetBool("Reload_b", false);
        PlayerPanel panel = UIManager.Instance.GetPanel<PlayerPanel>();
        if (panel != null) panel.EndReloadProgress();
    }

    void Movement(Vector3 camForwardFlat)
    {
        // 将物体在 XZ 平面上匀速转向 moveVector 的方向
        // moveVector 基于相机朝向，忽略相机的 X 轴旋转（投影到世界 XZ 平面）
        Vector3 camRight = mainCameraPos.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDirection = camForwardFlat * (moveVector.y >= 0 ? moveVector.y : -moveVector.y) + camRight * (moveVector.y >= 0 ? moveVector.x : -moveVector.x);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    void Turn(Vector3 camForwardFlat, Vector3 objForwardFlat)
    {
        // Body_Horizontal_f: 水平面有符号夹角，相机在物体负X侧时为负
        float horizontalAngle = Vector3.SignedAngle(objForwardFlat, camForwardFlat, Vector3.up);
        // Body_Vertical_f: 垂直面有符号夹角（俯仰差），相机仰视时为正，俯视时为负
        float camPitch = Mathf.Asin(Mathf.Clamp(mainCameraPos.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        float objPitch = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        float verticalAngle = camPitch - objPitch;

        animator.SetFloat("Body_Horizontal_f", Mathf.Clamp(horizontalAngle * (bodyRotationMax_h / 60f), -bodyRotationMax_h, bodyRotationMax_h));
        animator.SetFloat("Body_Vertical_f", Mathf.Clamp(verticalAngle * (bodyRotationMax_v / 60f), -bodyRotationMax_v, bodyRotationMax_v));
        // 瞄准状态下，若夹角大于60度则开始对齐，持续旋转直到夹角降至10度
    }
    //-----------动画事件触发的方法----------//
    public void TriggerFire()
    {
        curWeapon.Fire();
    }
    //投掷手持道具(手雷)：由投掷动作动画机事件调用。
    //在世界坐标与手中手雷模型相同的位置生成新手雷，取消kinematic、沿摄像机正方向给出初速度，并触发引信
    public void ThrowingObjects()
    {
        WeaponSwitching ws = GetComponent<WeaponSwitching>();
        if (ws == null) return;

        // 库存闸(实时查询)：背包已无手雷时绝不生成/投掷，只做收尾，杜绝"无中生有"的手雷
        BagPanel bag = UIManager.Instance.GetPanel<BagPanel>();
        if (bag == null || bag.itemsNum.GetValueOrDefault("Grenade") <= 0)
        {
            if (ws.Grenade != null && ws.Grenade.activeSelf) ws.OnGrenadeThrown();
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Items/10_Grenade");
        if (prefab == null)
        {
            Debug.LogWarning("未找到手雷预制体 Prefabs/Items/10_Grenade，无法投掷");
            //仍做一次收尾(扣雷/切刀)，防止预制体缺失时按住左键陷入无限空投循环
            ws.OnGrenadeThrown();
            return;
        }

        //在手中手雷模型的同一位置(世界坐标)生成新手雷
        GameObject grenade = Instantiate(prefab, ws.Grenade.transform.position, ws.Grenade.transform.rotation);
        grenade.layer = LayerMask.NameToLayer("Player");
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb == null) rb = grenade.AddComponent<Rigidbody>();
        //投掷出去的实体必须是动态刚体，取消kinematic
        rb.isKinematic = false;
        //方向与摄像机正方向平行
        Transform cam = mainCameraPos != null ? mainCameraPos : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null)
        {
            Debug.LogWarning("找不到主摄像机，无法确定投掷方向");
            Destroy(grenade);
            return;
        }
        rb.velocity = cam.forward * throwForce;

        //拉环，waitTime秒后爆炸
        GrenadeControl gc = grenade.GetComponent<GrenadeControl>();
        if (gc != null) gc.TriggerFuse();
        else Debug.LogWarning("手雷预制体上没有GrenadeControl，无法触发爆炸");

        //扣除背包手雷并隐藏手中模型；若扣减失败(极端竞争：库存在这一帧被耗尽)则回收刚生成的雷
        if (!ws.OnGrenadeThrown())
        {
            Destroy(grenade);
        }
    }
    //投掷动画末尾事件(在投掷动画最后插入事件帧调用)：
    //背包中还有手雷则把手雷模型放回手中(可连续投掷)，没有则切换为持刀状态
    public void ThrowingOver()
    {
        WeaponSwitching ws = GetComponent<WeaponSwitching>();
        if (ws != null) ws.OnThrowOver();
    }
    public void TriggerMeleeAtk(float time)
    {
        mr.TriggerMeleeAtk(time);
    }
    public void ReloadCompleted()
    {
        animator.SetBool("Reload_b", false);
        curWeapon.Reload();
    }
    //--------------------------------------//

    public void AimingAction()
    {
        // 举枪
        StartCoroutine(GunRaisingGesture(rigAim.weight, (isAim ? 1 : 0), gunRaisingTime));
        // 瞄准视角
        StartCoroutine(ViewpointSwitching(cco.m_Offset, (isAim ? aimOffset : genOffset), gunRaisingTime));
    }
    //隐藏玩家模型，在视觉上实现隐身
    public void Setvisiblity(bool isVisible)
    {
        if (Body == null)
        {
            Debug.Log("Body未必赋值");
            return;
        }
        Body.gameObject.SetActive(isVisible);
    }
    //禁用动画自带的位移
    public void SetAnimationMove(bool isUsing)
    {
        animator.enabled = isUsing;
    }

    IEnumerator RaiseGunThenFireCoroutine()
    {
        isFire = true;
        rigGen.weight = 0f;
        isHipGunRaised = true;
        yield return StartCoroutine(GunRaisingGesture(rigAim.weight, 1f, gunRaisingTime));
        animator.SetBool("Shoot_b", true);
        if (!isFire) animator.SetBool("Shoot_b", false);
    }
    IEnumerator DelayedHolsterCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        rigGen.weight = 1f;
        isHipGunRaised = false;
        yield return StartCoroutine(GunRaisingGesture(rigAim.weight, 0f, gunRaisingTime));
        delayedHolsterCoroutine = null;
    }
    IEnumerator GunRaisingGesture(float from, float to, float time)
    {
        if (isAim || isHipGunRaised) rigGen.weight = 0f;
        else rigGen.weight = 1f;

        float timer = 0;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / time); // 0~1的进度
            rigAim.weight = Mathf.Lerp(from, to, progress);
            yield return null;
        }
        rigAim.weight = to; // 最后一帧强制等于目标，避免精度误差

    }
    IEnumerator ViewpointSwitching(Vector3 start, Vector3 end, float totalTime)
    {
        float timer = 0;
        cco.m_Offset = start;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            // t：0~1
            float t = Mathf.Clamp01(timer / totalTime);
            // 匀速线性插值
            cco.m_Offset = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }
        // 确保最后等于目标值，避免浮点误差
        cco.m_Offset = end;
    }

    protected override void Move()
    {
        base.Move();
    }
    protected override void Death()
    {
        if (isDead) return;   // 防止死亡流程重复触发导致重复播放死亡音
        //玩家死亡音(不循环)
        if (deathSound != null) GameDataMgr.Instance.PlaySound(deathSound);
        base.Death();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = false;
    }
    public override void Injured(BaseObject attacker, float extraHarm)
    {
        base.Injured(attacker, extraHarm);
        //玩家受伤音(不循环；致死的那次由Death播放死亡音，此处不重复播)
        if (!isDead && hurtSound != null) GameDataMgr.Instance.PlaySound(hurtSound, 0.7f);
        pp.UpdataHP(HP, maxHP);
    }
}