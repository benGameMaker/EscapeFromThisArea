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
    public TextCapacityUI capacityUI;

    Transform mainCameraPos;
    [SerializeField]
    Transform Body;

    [HideInInspector]
    public bool isHoldingWeapon = false;
    [HideInInspector]
    public bool isAim = false;
    bool isAligningAim = false;
    bool isMoving = false;
    bool isHipGunRaised = false;
    Coroutine delayedHolsterCoroutine = null;
    ColliderProcessor cp;
    MeleeRange mr;
    [HideInInspector]
    public WeaponControl weaponControl;
    CinemachineCameraOffset cco;
    [HideInInspector]
    public Rig rigAim;
    [HideInInspector]
    public Rig rigGen;

    void Start()
    {
        mainCameraPos = Camera.main.transform;
        mr = GetComponentInChildren<MeleeRange>();
        cp = GetComponentInChildren<ColliderProcessor>();
        weaponControl = GetComponentInChildren<WeaponControl>();
        rigAim = transform.Find("Rig_WeaponAim").GetComponentInChildren<Rig>();
        rigGen = transform.Find("Rig_WeaponPose").GetComponentInChildren<Rig>();
        cco = transform.Find("FreeLook Camera").GetComponentInChildren<CinemachineCameraOffset>();
    }

    protected override void Update()
    {
        animator.SetBool("Static_b", isStatic);

        if (isHoldingWeapon && weaponControl.curCapacity <= 0) animator.SetBool("Shoot_b", false);
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

    bool isFire = false;
    public void Fire(InputAction.CallbackContext context)
    {
        if (!isHoldingWeapon)
        {
            if(context.performed)
            {
                animator.SetInteger("WeaponType_int", 12);
            }
            if (context.canceled)
            {
                animator.SetInteger("WeaponType_int", 0);
            }
            return;
        }
        if (weaponControl.curCapacity <= 0)
        {
            animator.SetBool("Reload_b", true);
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
        animator.SetBool("Reload_b", true);
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
    public void TriggerFire()
    {
        weaponControl.Fire();
    }
    public void TriggerMeleeAtk(float time)
    {
        mr.TriggerMeleeAtk(time);
    }
    public void ReloadCompleted()
    {
        animator.SetBool("Reload_b", false);
        weaponControl.curCapacity = weaponControl.capacity;
    }
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
        base.Death();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = false;
    }
}