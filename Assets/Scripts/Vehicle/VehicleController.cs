using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleController : MonoBehaviour
{
    public WheelCollider flWheel;
    public WheelCollider frWheel;
    public WheelCollider rlWheel;
    public WheelCollider rrWheel;

    public Transform flWheelModel;
    public Transform frWheelModel;
    public Transform rlWheelModel;
    public Transform rrWheelModel;

    public float motorTorque;
    public float brakeTorque;
    public float maxSteeringAngle;

    [Tooltip("摄像机瞄准射线的最大距离，超过该距离武器指向射线末端")]
    public float aimRayDistance = 100f;

    [Tooltip("反向行驶刹车时，车速（本地Z轴速度）降到该值以下才恢复驱动力")]
    public float brakeStopSpeed = 1f;

    float fireRate = 10f;

    Vector2 inputAxis;
    Transform mainCameraPos;
    Transform WeaponPos;

    //WeaponPos下收集到的武器列表，切换方向为下标从小到大轮换
    List<WeaponControl> weaponLists = new List<WeaponControl>();
    int curWeaponIndex = -1;
    WeaponControl curWeapon;
    VehicleDamage vd;
    [HideInInspector]
    bool isFiring = false;
    float nextFireTime = 0f;
    Coroutine reloadCoroutine;
    CinemachineFreeLook cfl;
    bool isBrakeHeld = false;
    bool isCanDrive = true;
    [HideInInspector]
    public BagPanel bp;

    void Awake()
    {
        WeaponPos = transform.Find("WeaponPos");
        if (WeaponPos == null) Debug.LogWarning("WeaponPos获取失败");
        vd = GetComponent<VehicleDamage>();
        //组件可能默认禁用（上车才激活），Start不会执行，Awake不受禁用影响，确保武器列表提前就绪
        CollectWeapons();
    }

    void Start()
    {
        mainCameraPos = Camera.main.transform;
        cfl = transform.Find("CameraPos/FreeLook Camera").GetComponent<CinemachineFreeLook>();
        if (cfl == null) Debug.Log("获取cfl失败");
        //兜底：若Awake时WeaponPos尚未就绪则在此补齐
        CollectWeapons();
    }

    //收集WeaponPos下所有挂载WeaponControl的武器，默认全部隐藏，切到时再显示
    void CollectWeapons()
    {
        if (weaponLists.Count > 0) return; //已收集，避免重复
        if (WeaponPos == null) return;

        for (int i = 0; i < WeaponPos.childCount; i++)
        {
            Transform weapon = WeaponPos.GetChild(i);
            WeaponControl wc = weapon.GetComponent<WeaponControl>();
            if (wc != null)
            {
                wc.enableExclusionLayer = true;
                weaponLists.Add(wc);
            }
        }
        if (weaponLists.Count > 0) SwitchToWeapon(0);
        else Debug.LogWarning("WeaponPos下没有找到带WeaponControl的武器");
    }

    void Update()
    {
        UpdataWheelModelsRot();
        UpdateWeaponAim();
        if (isFiring) TryFire();
    }
    private void FixedUpdate()
    {
        Move();
        Turn();
    }

    public void Drive(InputAction.CallbackContext context)
    {
        if (isCanDrive) inputAxis = context.ReadValue<Vector2>();
    }
    public void Fire(InputAction.CallbackContext context)
    {
        if (context.performed) isFiring = true;
        else if (context.canceled) isFiring = false;
    }
    public void SwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (weaponLists.Count <= 1) return;
            SwitchToWeapon(curWeaponIndex + 1);
        }
    }
    public void Reload(InputAction.CallbackContext context)
    {
        if (context.performed) DoReload();
    }
    public void Brake(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isBrakeHeld = true;
            ApplyBrake(brakeTorque);
        }
        else if (context.canceled)
        {
            isBrakeHeld = false;
            ApplyBrake(0);
        }
    }
    public void DropOff(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            VehicleInteraction vehicleInteraction = GetComponent<VehicleInteraction>();
            if (vehicleInteraction == null)
            {
                Debug.LogWarning("车辆上没有VehicleInteraction，无法下车");
                return;
            }
            vehicleInteraction.DropOff();
        }
    }
    public void OpenBag(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("汽车的控制信号");
            if(bp==null) bp = UIManager.Instance.GetPanel<BagPanel>();
            if (bp.isShow)// 关闭背包
            {
                bp.HideMe(() => { });
                Cursor.lockState = CursorLockMode.Locked;
            }
            else// 打开背包
            {
                bp.ShowMe();
                Cursor.lockState = CursorLockMode.Confined;
            }
            Cursor.visible = bp.isShow;
            enableMouseInput(!bp.isShow);
        }
    }

    void TryFire()
    {
        if (curWeapon == null) return;

        if (curWeapon.curCapacity <= 0)
        {
            DoReload();
            return;
        }
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        curWeapon.Fire();
    }
    void DoReload()
    {
        if (curWeapon == null || reloadCoroutine != null) return; //换弹中不重复触发

        //通知面板播放换弹进度，ReloadTime秒后完成补弹
        curWeapon.StartReload();
        reloadCoroutine = StartCoroutine(ReloadAfterDelay(curWeapon));
    }

    void SwitchToWeapon(int index)
    {
        if (weaponLists.Count == 0) return;


        curWeaponIndex = ((index % weaponLists.Count) + weaponLists.Count) % weaponLists.Count;
        curWeapon = weaponLists[curWeaponIndex];
        fireRate = curWeapon.fireRate;

        //切枪后刷新车辆UI面板的武器数据（面板未显示时GetPanel返回null，不处理）
        VehiclePanel vp = UIManager.Instance.GetPanel<VehiclePanel>();
        if (vp != null) vp.ShowCapacityDataToUI(curWeapon);
    }

    //上车时显示车辆UI面板并展示当前武器数据（由VehicleInteraction调用）
    public void ShowVehicleUI()
    {
        //玩家HUD面板常驻不销毁（WeaponSwitching/PlayerController缓存其引用，销毁会导致切枪等失效），仅隐藏显示
        PlayerPanel pp = UIManager.Instance.GetPanel<PlayerPanel>();
        if (pp != null) pp.gameObject.SetActive(false);

        VehiclePanel vp = UIManager.Instance.ShowPanel<VehiclePanel>();
        if (curWeapon != null) vp.ShowCapacityDataToUI(curWeapon);

        if (vd.isBroke) vp.ShowOrHideNotice(true);
        else vp.ShowOrHideNotice(false);
        vp.UpdataCurIntegrity(vd.curIntegrity, vd.maxIntegrity);
        vd.vp = UIManager.Instance.GetPanel<VehiclePanel>();
    }

    //下车时隐藏车辆UI面板、重新显示玩家面板（由VehicleInteraction调用）
    public void HideVehicleUI()
    {
        UIManager.Instance.HidePanel<VehiclePanel>();

        PlayerPanel pp = UIManager.Instance.GetPanel<PlayerPanel>();
        if (pp != null) pp.gameObject.SetActive(true);
    }
    //为所有武器设置归属者（上车时由VehicleInteraction传入玩家，使子弹伤害归属玩家）
    public void SetWeaponsOwner(BaseObject owner)
    {
        CollectWeapons(); //兜底：确保武器列表已收集（组件被禁用时Start未执行）
        //Debug.Log(name + "拥有" + weaponLists.Count + "把武器");
        for (int i = 0; i < weaponLists.Count; i++)
        {
            Debug.Log("正在给" + weaponLists[i].name + "传入拥有者");
            if (weaponLists[i] != null)
            {
                weaponLists[i].SetOwner(owner);
                //if (owner != null) Debug.Log("向" + weaponLists[i].name + "传入" + owner.name + "的信息");
                //else Debug.Log("向" + weaponLists[i].name + "传入" + owner.name + "的信息---失败!!!!!!!!!!!");
            }
        }
    }
    public void SetVehicleState(bool state)
    {
        isCanDrive = state;
        Debug.Log("车辆驾驶状态设置为：" + isCanDrive);
    }
    IEnumerator ReloadAfterDelay(WeaponControl weapon)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, weapon.ReloadTime * 2f));
        weapon.Reload();
        reloadCoroutine = null;
    }
    void UpdateWeaponAim()
    {
        if (WeaponPos == null || mainCameraPos == null) return;

        Ray ray = new Ray(mainCameraPos.position, mainCameraPos.forward);
        //默认目标为射线末端
        Vector3 targetPos = mainCameraPos.position + mainCameraPos.forward * aimRayDistance;

        //排除车辆所在层（Vehicle），避免瞄准射线打到车辆自身
        LayerMask ignoreVehicle = ~LayerMask.GetMask("Vehicle");
        if (Physics.Raycast(ray, out RaycastHit hit, aimRayDistance, ignoreVehicle))
        {
                targetPos = hit.point;
        }

        Vector3 dir = targetPos - WeaponPos.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            WeaponPos.rotation = Quaternion.LookRotation(dir);
        }
    }

    //启用或关闭鼠标移动带到视角变化以及开火
    public void enableMouseInput(bool enable)
    {
        string x = (enable ? "Mouse X" : "");
        string y = (enable ? "Mouse Y" : "");
        //玩家输入由PlayerInput组件驱动，必须禁用组件内部的Fire action；
        //手动new的PlayerInputActions实例是另一套独立输入，禁用它不会影响组件
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            InputAction fireAction = playerInput.actions.FindAction("Fire");
            if (fireAction != null)
            {
                if (enable) fireAction.Enable();
                else fireAction.Disable();
                Debug.Log(enable ? "启用鼠标控制开火" : "禁用鼠标控制开火");
            }
            else Debug.LogWarning("未找到Fire action");
        }
        cfl.m_XAxis.m_InputAxisName = x;
        cfl.m_YAxis.m_InputAxisName = y;
    }


    //移动：当输入方向与当前车速方向相反时先刹车制动，减速到brakeStopSpeed以内再恢复驱动力
    void Move()
    {
        float inputY = inputAxis.y;
        float speed = vd != null ? vd.curVelocity : 0f;

        //反向意图：想前进但车在较快倒车，或想倒车但车在较快前进
        bool reverseIntent = (inputY > 0.01f && speed < -brakeStopSpeed)
                          || (inputY < -0.01f && speed > brakeStopSpeed);

        if (reverseIntent)
        {
            //先刹车制动，暂不施加驱动力
            ApplyBrake(brakeTorque);
            rlWheel.motorTorque = 0;
            rrWheel.motorTorque = 0;
            return;
        }

        //车速已降到阈值范围内（或方向一致），恢复驱动力
        //若反向刹车结束且未按Space，需清掉之前施加的刹车
        if (!isBrakeHeld) ApplyBrake(0);

        rlWheel.motorTorque = motorTorque * inputY;
        rrWheel.motorTorque = motorTorque * inputY;
    }
    void ApplyBrake(float torque)
    {
        flWheel.brakeTorque = torque;
        frWheel.brakeTorque = torque;
        rlWheel.brakeTorque = torque;
        rrWheel.brakeTorque = torque;
    }
    void Turn()
    {
        flWheel.steerAngle = maxSteeringAngle * inputAxis.x;
        frWheel.steerAngle = maxSteeringAngle * inputAxis.x;
    }
    void UpdataWheelModelsRot()
    {
        UpdataWheelModelRot(flWheelModel, flWheel);
        UpdataWheelModelRot(frWheelModel, frWheel);
        UpdataWheelModelRot(rlWheelModel, rlWheel);
        UpdataWheelModelRot(rrWheelModel, rrWheel);
    }
    void UpdataWheelModelRot(Transform wheelModel, WheelCollider wheelCollider)
    {
        if (wheelModel == null || wheelCollider == null) return;
        wheelCollider.GetWorldPose(out var pos, out var rot);
        wheelModel.transform.position = pos;
        wheelModel.transform.rotation = rot;
    }
}
