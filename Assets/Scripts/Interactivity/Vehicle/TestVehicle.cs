using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVehicle : BaseInteractivity
{
    Transform DrivingPos;
    Transform DropOffPos;
    TestCar car;

    PlayerController pc;
    Rigidbody rbPc;
    Collider colPc;
    Coroutine snapCo;
    private void Start()
    {
        DrivingPos = transform.Find("DrivingPos");
        DropOffPos = transform.Find("DropOffPos");
        car = GetComponent<TestCar>();
        if (DrivingPos == null) Debug.LogWarning("DrivingPos获取失败");
        if (DropOffPos == null) Debug.LogWarning("DropOffPos获取失败");
    }
    private void Update()
    {
        
    }
    public override void InteractionBehavior(GameObject obj)
    {
        pc = obj.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning(name + "获取PlayerController失败");
            return;
        }
        rbPc = pc.GetComponent<Rigidbody>();
        colPc = pc.GetComponent<Collider>();
        Debug.Log(pc.name + "进入了" + name);

        SetPlayerBehaviour(obj, false);
        SetVehicleBehaviour(true);
        
    }
    //禁用玩家位移上的行动
    public void SetPlayerBehaviour(GameObject obj, bool isCanAction)
    {
        if (snapCo != null) StopCoroutine(snapCo);

        if (isCanAction)
        {
            // 下车：先脱离车辆放到落点，再恢复动画位移与物理
            pc.transform.SetParent(null);
            pc.transform.position = DropOffPos.position;
            pc.SetAnimationMove(true);
            pc.Setvisiblity(true);
            rbPc.velocity = Vector3.zero;
            rbPc.angularVelocity = Vector3.zero;
            rbPc.isKinematic = false;
            colPc.enabled = true;
        }
        else
        {
            // 进车：先把玩家调整为"不受动画/物理影响"的状态（此刻仍是世界坐标）。
            // 注意：运行时切换applyRootMotion会触发Animator重新初始化，把玩家transform强制拉回Animator记录的根位置基准（即按E前的世界坐标），
            // 所以要趁玩家仍是世界坐标时先关闭RootMotion（被拉回也停留在原地，无影响），
            // 再到下一帧吸附到DrivingPos，避开当帧Animator更新阶段重新初始化对位置的覆盖。
            pc.SetAnimationMove(false);
            pc.Setvisiblity(false);
            rbPc.velocity = Vector3.zero;
            rbPc.angularVelocity = Vector3.zero;
            rbPc.isKinematic = true;
            colPc.enabled = false;
            snapCo = StartCoroutine(SnapToDrivingPos());
        }
    }

    // 下一帧吸附到DrivingPos：等Animator完成"运行时切换applyRootMotion"触发的重新初始化后再设置父子关系与位置
    IEnumerator SnapToDrivingPos()
    {
        yield return null;
        if (pc == null || DrivingPos == null) yield break;
        pc.transform.SetParent(DrivingPos);
        pc.transform.localPosition = Vector3.zero;
        pc.transform.localRotation = Quaternion.identity;
    }
    //激活车辆物体上的驾驶车辆的脚本
    public void SetVehicleBehaviour(bool islaunch)
    {
        car.enabled = islaunch;
    }
}
