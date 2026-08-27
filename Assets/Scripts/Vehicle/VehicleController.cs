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

    float fireRate = 10f;

    Vector2 inputAxis;
    Transform mainCameraPos;
    Transform WeaponPos;

    //WeaponPos下收集到的武器列表，切换方向为下标从小到大轮换
    List<WeaponControl> weaponLists = new List<WeaponControl>();
    int curWeaponIndex = -1;
    WeaponControl curWeapon;
    [HideInInspector]

    bool isFiring = false;
    float nextFireTime = 0f;

    void Awake()
    {
        WeaponPos = transform.Find("WeaponPos");
        if (WeaponPos == null) Debug.LogWarning("WeaponPos获取失败");

        //组件可能默认禁用（上车才激活），Start不会执行，Awake不受禁用影响，确保武器列表提前就绪
        CollectWeapons();
    }

    void Start()
    {
        mainCameraPos = Camera.main.transform;
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
        inputAxis = context.ReadValue<Vector2>();
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
            flWheel.brakeTorque = brakeTorque;
            frWheel.brakeTorque = brakeTorque;
            rlWheel.brakeTorque = brakeTorque;
            rrWheel.brakeTorque = brakeTorque;
        }
        else if (context.canceled)
        {
            flWheel.brakeTorque = 0;
            frWheel.brakeTorque = 0;
            rlWheel.brakeTorque = 0;
            rrWheel.brakeTorque = 0;
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
        if (curWeapon == null) return;
        curWeapon.curCapacity = curWeapon.capacity;
    }
    void SwitchToWeapon(int index)
    {
        if (weaponLists.Count == 0) return;


        curWeaponIndex = ((index % weaponLists.Count) + weaponLists.Count) % weaponLists.Count;
        curWeapon = weaponLists[curWeaponIndex];
        fireRate = curWeapon.fireRate;
        
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

    void Move()
    {
        rlWheel.motorTorque = motorTorque * inputAxis.y;
        rrWheel.motorTorque = motorTorque * inputAxis.y;
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
