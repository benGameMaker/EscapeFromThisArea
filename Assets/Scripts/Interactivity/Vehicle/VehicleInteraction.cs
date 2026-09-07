using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleInteraction : BaseInteractivity
{
    [Tooltip("驾驶车辆时，刚体要忽略的层级")]
    public LayerMask ignorelLayer;
    
    LayerMask originalExcludeLayer;
    Transform DropOffPos;
    Rigidbody rb;
    VehicleController vehicle;
    PlayerController driver;

    [Tooltip("上车后DetectionZombies相对DropOffPos的局部偏移（沿+X方向微调让射线能瞄准到车身，y/z一般保持0）")]
    public Vector3 vehicleDetectorOffset = new Vector3(0.5f, 0f, 0f);
    DetectionZombies rideDetector;         // 上车后随车的检测器引用（下车时挂回玩家）
    Vector3 rideDetectorLocalOffset;       // 上车前检测器相对玩家根节点的局部偏移（下车时恢复）
    private void Start()
    {
        DropOffPos = transform.Find("DropOffPos");
        if (DropOffPos == null) Debug.LogWarning("DropOffPos获取失败");
        vehicle = GetComponent<VehicleController>();
        rb = GetComponent<Rigidbody>();
        originalExcludeLayer = rb.excludeLayers;
    }
    private void Update()
    {
        
    }
    //玩家在外部使用这个函数来于车辆进行互动
    public override void InteractionBehavior(PlayerController player)
    {
        if (player == null) return;

        //记录上车玩家，供下车时恢复
        driver = player;

        //将玩家设置为DropOffPos的子物体，然后失活隐藏
        driver.transform.SetParent(DropOffPos);
        driver.transform.localPosition = Vector3.zero;
        driver.transform.localRotation = Quaternion.identity;

        driver.isAim = false;
        driver.rigAim.weight = 0f;
        driver.rigGen.weight = 1f;
        driver.vd = GetComponent<VehicleDamage>();
        // 上车会隐藏玩家并切换到车载武器：打断仍在进行中的徒步换弹，避免换弹状态残留
        driver.CancelReload();

        // 检测器随车挂载：下面玩家整体会被SetActive(false)，
        // 先把DetectionZombies从玩家改挂到驾驶位并保持激活，保证车载武器开火时也能吸引僵尸。
        // 注意：不再保留玩家身上(0,1.58,0)的站立偏移，否则检测点会高出车顶导致射线瞄不到车身，
        // 而是放在DropOffPos并沿其+X方向做小幅偏移，让僵尸锥形视野的射线能命中车身。
        rideDetector = driver.GetComponentInChildren<DetectionZombies>(true);
        if (rideDetector != null)
        {
            rideDetectorLocalOffset = rideDetector.transform.localPosition; // 记录徒步时的局部偏移，下车时恢复
            MoveDetector(rideDetector, DropOffPos != null ? DropOffPos : transform, vehicleDetectorOffset);
        }

        driver.gameObject.SetActive(false);

        //将玩家信息传给车辆上的武器（子弹伤害归属玩家）
        if (vehicle != null)
        {
            vehicle.SetWeaponsOwner(driver);
            //if (obj.GetComponent<BaseObject>().name != null) Debug.Log("VehicleInteraction传入玩家信息" + obj.GetComponent<BaseObject>().name);
            //else Debug.Log("VehicleInteraction传入玩家信息" + obj.GetComponent<BaseObject>().name+"------失败！！！！！！！！！！");
        }
        else Debug.Log("vehicle不存在");

        SetVehicleBehaviour(true);

        //上车后显示车辆UI面板并展示当前武器数据
        if (vehicle != null) vehicle.ShowVehicleUI();

        HideInteractionPanel();
    }

    public void DropOff()
    {
        if (driver == null)
        {
            Debug.LogWarning("当前没有玩家在车上，无法下车");
            return;
        }

        SetVehicleBehaviour(false);

        //下车后隐藏车辆UI面板
        if (vehicle != null) vehicle.HideVehicleUI();

        driver.gameObject.SetActive(true);
        rb.excludeLayers = originalExcludeLayer;

        //解除父子关系回到场景根节点，位置在世界坐标下与DropOffPos相同
        driver.transform.SetParent(null);
        driver.transform.position = DropOffPos.position;
        driver.transform.rotation = DropOffPos.rotation;
        driver.vd = null;

        // 下车：把DetectionZombies挂回玩家根节点并恢复徒步时的局部偏移
        if (rideDetector != null)
            MoveDetector(rideDetector, driver.transform, rideDetectorLocalOffset);
        rideDetector = null;

        driver = null;
    }

    // 把DetectionZombies检测器整体改挂到targetParent下，并设置局部偏移/姿态、保持激活，
    // 使检测器在徒步（挂玩家根节点）与驾驶（挂车辆驾驶位）时都能正常运行
    void MoveDetector(DetectionZombies detector, Transform targetParent, Vector3 localOffset)
    {
        if (detector == null || targetParent == null) return;
        detector.transform.SetParent(targetParent, false);
        detector.transform.localPosition = localOffset;
        detector.transform.localRotation = Quaternion.identity;
        detector.gameObject.SetActive(true);
    }
    //设置车辆物体上的驾驶车辆脚本的激活状态
    public void SetVehicleBehaviour(bool islaunch)
    {
        if (vehicle == null)
        {
            Debug.LogWarning("VehicleController未挂载在车辆上，无法" + (islaunch ? "启动" : "停止") + "驾驶");
            return;
        }
        rb.excludeLayers = ignorelLayer;
        // 下车时打断仍在进行中的换弹（停协程/复位武器状态/清车辆UI进度），避免下次上车残留换弹状态
        if (!islaunch) vehicle.CancelReload();
        vehicle.enabled = islaunch;

        //上车播放"上车提示音(不循环)+驾驶循环音"，下车停止驾驶循环音
        if (islaunch) vehicle.PlayDrivingAudio();
        else vehicle.StopDrivingAudio();

        //同步通过BagLogicProcess启用/停用车辆的PlayerInput：
        //上车开启输入、下车关闭输入，避免不在车上时车辆的输入仍被监听
        BagLogicProcess bagLogic = GetComponent<BagLogicProcess>();
        if (bagLogic != null) bagLogic.SetActiveInput(islaunch);
    }
}
