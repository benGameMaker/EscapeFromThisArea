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

        driver = null;
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
        vehicle.enabled = islaunch;

        //同步通过BagLogicProcess启用/停用车辆的PlayerInput：
        //上车开启输入、下车关闭输入，避免不在车上时车辆的输入仍被监听
        BagLogicProcess bagLogic = GetComponent<BagLogicProcess>();
        if (bagLogic != null) bagLogic.SetActiveInput(islaunch);
    }
}
