using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 场景浏览飞行模式（类似Scene窗口的Fly模式）。
/// 挂载到真正参与渲染的相机（带有 CinemachineBrain 的 Main Camera）上即可。
///
/// 按下 P 键进入/退出飞行：
/// - 飞行时直接控制相机的位置与朝向，玩家/车辆不再响应输入；
///   它们的 PlayerInput（移动/行为）与 FreeLook Camera（鼠标转动视角）都会被冻结，
///   使其留在原地不被操作。
/// - WASD 分别沿相机本地坐标 +Z / -X / -Z / +X 平移，Q/E 为 -Y / +Y；
///   鼠标移动控制相机看向的方向；Shift 加速、Ctrl 减速。
/// - 再次按 P 恢复之前被操控物体的输入，相机交还给 Cinemachine。
/// </summary>
public class SceneFlyCamera : MonoBehaviour
{
    [Header("开关")]
    public KeyCode toggleKey = KeyCode.P;

    [Header("飞行参数")]
    [Tooltip("基础飞行速度(单位/秒)")]
    public float flySpeed = 8f;
    [Tooltip("按住Shift的速度倍率(对应Scene窗口加速)")]
    public float fastMultiplier = 4f;
    [Tooltip("按住Ctrl的速度倍率(对应Scene窗口减速)")]
    public float slowMultiplier = 0.15f;
    [Tooltip("鼠标灵敏度(度/像素当量)")]
    public float mouseSensitivity = 2f;
    [Tooltip("是否反转鼠标Y轴")]
    public bool invertMouseY = false;

    // 实际被移动的渲染相机(CinemachineBrain所在物体)
    Transform flyCam;
    CinemachineBrain brain;
    bool isFlying = false;
    float yaw;
    float pitch;

    // 进入飞行时冻结的"可操作物体"(玩家/车辆)状态，退出时恢复
    readonly List<BagLogicProcess> frozenBlps = new List<BagLogicProcess>();
    readonly List<Behaviour> frozenControllers = new List<Behaviour>();

    void Start()
    {
        Camera camComp = GetComponent<Camera>();
        if (camComp == null) camComp = Camera.main;
        if (camComp == null) camComp = FindObjectOfType<Camera>();
        if (camComp != null)
        {
            flyCam = camComp.transform;
            brain = camComp.GetComponent<CinemachineBrain>();
        }
        if (brain == null) brain = FindObjectOfType<CinemachineBrain>();
        if (flyCam == null && brain != null) flyCam = brain.transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isFlying) ExitFlyMode();
            else EnterFlyMode();
            return;
        }

        if (isFlying) FlyUpdate();
    }

    void EnterFlyMode()
    {
        if (flyCam == null)
        {
            Debug.LogWarning("SceneFlyCamera: 找不到渲染相机，无法进入飞行模式");
            return;
        }

        // 用当前相机朝向初始化俯仰/偏航
        Vector3 euler = flyCam.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // 冻结玩家/车辆的输入与FreeLook视角，使其留在原地不被操作
        FreezeControlledObjects();

        // 关掉Cinemachine接管，飞行期间相机完全由本脚本控制
        if (brain != null) brain.enabled = false;

        isFlying = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExitFlyMode()
    {
        isFlying = false;

        // 恢复Cinemachine对相机的接管
        if (brain != null) brain.enabled = true;

        // 恢复之前被冻结的玩家/车辆输入与FreeLook
        RestoreControlledObjects();

        // 回到游戏的鼠标状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 找出当前处于"可操作"状态的玩家/车辆(带PlayerController或VehicleController的BagLogicProcess)，
    /// 禁用它们的 PlayerInput、FreeLook 鼠标视角与控制脚本，并清除刚体速度。
    /// </summary>
    void FreezeControlledObjects()
    {
        frozenBlps.Clear();
        frozenControllers.Clear();

        foreach (BagLogicProcess blp in FindObjectsOfType<BagLogicProcess>())
        {
            PlayerController pc = blp.GetComponent<PlayerController>();
            VehicleController vc = blp.GetComponent<VehicleController>();
            if (pc == null && vc == null) continue;   // 只处理玩家/车辆这类操控物体

            PlayerInput pi = blp.GetComponent<PlayerInput>();
            bool controllerEnabled = (pc != null && pc.enabled) || (vc != null && vc.enabled);
            bool inputEnabled = pi != null && pi.enabled;
            if (!controllerEnabled && !inputEnabled) continue;  // 本身就没在被操控

            // 停用自由视角鼠标输入(并禁用Fire/Switch/Aim) + 停用整个PlayerInput(移动/行为)
            blp.EnableGameplayInput(false);
            blp.SetActiveInput(false);
            frozenBlps.Add(blp);   // 记录，退出飞行时据此恢复PlayerInput与FreeLook轴

            // 停用控制脚本，防止残留输入/状态继续驱动物体(如车辆惯性、持续开火)
            if (pc != null && pc.enabled)
            {
                pc.enabled = false;
                frozenControllers.Add(pc);
            }
            else if (vc != null && vc.enabled)
            {
                vc.enabled = false;
                frozenControllers.Add(vc);
            }

            // 让正在行驶的车辆当场刹停，保持"呆在原地"
            Rigidbody rb = blp.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void RestoreControlledObjects()
    {
        foreach (BagLogicProcess blp in frozenBlps)
        {
            if (blp == null) continue;
            blp.SetActiveInput(true);
            blp.EnableGameplayInput(true);
        }
        foreach (Behaviour controller in frozenControllers)
        {
            if (controller != null) controller.enabled = true;
        }
        frozenBlps.Clear();
        frozenControllers.Clear();
    }

    void FlyUpdate()
    {
        // 1. 平移：W/S/A/D = 相机本地 +Z/-Z/-X/+X，Q/E = -Y/+Y（与Scene窗口一致）
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += flyCam.forward;
        if (Input.GetKey(KeyCode.S)) move -= flyCam.forward;
        if (Input.GetKey(KeyCode.D)) move += flyCam.right;
        if (Input.GetKey(KeyCode.A)) move -= flyCam.right;
        if (Input.GetKey(KeyCode.E)) move += flyCam.up;
        if (Input.GetKey(KeyCode.Q)) move -= flyCam.up;

        float speed = flySpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            speed *= slowMultiplier;

        flyCam.position += move.normalized * speed * Time.deltaTime;

        // 2. 鼠标转动视角（光标锁定，用增量转向）
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y") * (invertMouseY ? -1f : 1f);
        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        flyCam.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
