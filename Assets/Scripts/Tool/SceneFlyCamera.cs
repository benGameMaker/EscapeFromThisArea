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
///
/// 飞行中按 L 进入/退出"以玩家为中心的环绕"：
/// - 主相机直接来到玩家身边，以玩家LookAtPoint为圆心自动圆周运动并始终看向它；
///   W/S调半径、Q/E调高度、A/D切换环绕方向、Shift/Ctrl调角速度。
/// - 再次按 L 停留在原地，恢复自由飞行。
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

    [Header("环绕模式(L键，需先进入飞行)")]
    public KeyCode orbitToggleKey = KeyCode.L;
    [Tooltip("进入环绕时相机与目标(玩家LookAtPoint)的水平距离")]
    public float defaultOrbitRadius = 6f;
    [Tooltip("进入环绕时相机相对目标的高度")]
    public float defaultOrbitHeight = 2f;
    [Tooltip("W/S调整环绕半径的速度(单位/秒)")]
    public float orbitRadiusSpeed = 4f;
    [Tooltip("Q/E调整环绕高度的速度(单位/秒)")]
    public float orbitHeightSpeed = 3f;
    [Tooltip("环绕角速度(度/秒)；Shift/Ctrl沿用上面的速度倍率")]
    public float orbitAngularSpeed = 25f;

    // 实际被移动的渲染相机(CinemachineBrain所在物体)
    Transform flyCam;
    CinemachineBrain brain;
    bool isFlying = false;
    float yaw;
    float pitch;

    // 环绕模式状态
    bool orbitMode = false;        // 是否正处于以玩家为中心的环绕
    Transform orbitTarget;         // 环绕目标(玩家身上的LookAtPoint)
    float orbitRadius;
    float orbitHeight;
    float orbitAngle;              // 当前环绕角度(度)
    int orbitDir = 1;              // 环绕方向：1或-1

    // 进入飞行时冻结的"可操作物体"(玩家/车辆)状态，退出时恢复
    readonly List<BagLogicProcess> frozenBlps = new List<BagLogicProcess>();
    readonly List<Behaviour> frozenControllers = new List<Behaviour>();

    // 飞行期间隐藏UI与角色"看相机瞄准点"的组件，退出时恢复
    GameObject uiCanvas;
    bool uiCanvasWasActive;
    RayPointAndShowInteraction playerLook;
    bool playerLookWasEnabled;

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

        orbitRadius = defaultOrbitRadius;
        orbitHeight = defaultOrbitHeight;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isFlying) ExitFlyMode();
            else EnterFlyMode();
            return;
        }

        if (!isFlying) return;

        if (Input.GetKeyDown(orbitToggleKey))
        {
            if (orbitMode) ExitOrbitMode();
            else EnterOrbitMode();
            return;
        }

        if (orbitMode) OrbitUpdate();
        else FlyUpdate();
    }

    // 安全兜底：若飞行期间本物体被销毁/失活(如切场景)，保证所有被改动状态都恢复
    void OnDisable()
    {
        if (isFlying)
        {
            isFlying = false;
            RestoreAll();
        }
    }

    void EnterFlyMode()
    {
        if (flyCam == null)
        {
            Debug.LogWarning("SceneFlyCamera: 找不到渲染相机，无法进入飞行模式");
            return;
        }

        // 用当前相机朝向初始化俯仰/偏航
        InitYawPitchFromCam();
        orbitMode = false;

        // 冻结玩家/车辆的输入与FreeLook视角，使其留在原地不被操作
        FreezeControlledObjects();

        // 隐藏全部UI，并停用玩家"头部一直看向相机瞄准点"的组件
        HideUIAndDisableHeadLook();

        // 关掉Cinemachine接管，飞行期间相机完全由本脚本控制
        if (brain != null) brain.enabled = false;

        isFlying = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExitFlyMode()
    {
        isFlying = false;
        orbitMode = false;
        RestoreAll();

        // 回到游戏的鼠标状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 按当前相机朝向同步内部yaw/pitch，用于进入自由飞行/退出环绕时衔接鼠标视角。
    /// </summary>
    void InitYawPitchFromCam()
    {
        Vector3 euler = flyCam.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
    }

    /// <summary>
    /// 退出/中断飞行时统一恢复：Cinemachine接管、玩家/车辆输入与FreeLook、
    /// UI显示、角色的头部瞄准组件。
    /// </summary>
    void RestoreAll()
    {
        if (brain != null) brain.enabled = true;
        RestoreControlledObjects();
        RestoreUIAndHeadLook();
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

    /// <summary>
    /// 飞行期间隐藏全部UI(直接失活UIManager的主Canvas)，并停用玩家身上
    /// "头部一直看向主相机瞄准点"的RayPointAndShowInteraction组件。
    /// </summary>
    void HideUIAndDisableHeadLook()
    {
        // 1) 隐藏UI：失活UIManager产生的主Canvas(它不会随场景销毁)
        if (uiCanvas == null && UIManager.Instance != null && UIManager.Instance.canvasTrans != null)
            uiCanvas = UIManager.Instance.canvasTrans.gameObject;
        if (uiCanvas != null)
        {
            uiCanvasWasActive = uiCanvas.activeSelf;
            if (uiCanvasWasActive) uiCanvas.SetActive(false);
        }

        // 2) 停用玩家角色头部瞄准组件的旋转驱动(不影响退出后的恢复)
        playerLookWasEnabled = false;
        if (SceneMgr.Instance != null && SceneMgr.Instance.player != null)
        {
            playerLook = SceneMgr.Instance.player.GetComponent<RayPointAndShowInteraction>();
            if (playerLook != null && playerLook.enabled)
            {
                playerLook.enabled = false;
                playerLookWasEnabled = true;
            }
        }
    }

    void RestoreUIAndHeadLook()
    {
        // 恢复UI Canvas的显示
        if (uiCanvas != null && uiCanvasWasActive && !uiCanvas.activeSelf)
            uiCanvas.SetActive(true);

        // 恢复玩家头部瞄准组件的启用状态
        if (playerLook != null && playerLookWasEnabled && !playerLook.enabled)
            playerLook.enabled = true;

        uiCanvas = null;
        playerLook = null;
    }

    /// <summary>
    /// 环绕目标：优先取玩家身上的LookAtPoint，找不到则退回玩家根节点
    /// </summary>
    Transform FindOrbitTarget()
    {
        PlayerController player = SceneMgr.Instance != null ? SceneMgr.Instance.player : null;
        if (player == null) return null;
        Transform look = player.transform.Find("LookAtPoint");
        return look != null ? look : player.transform;
    }

    /// <summary>
    /// 进入环绕：主相机直接来到玩家身边，以其LookAtPoint为中心做圆周运动并始终看向它
    /// </summary>
    void EnterOrbitMode()
    {
        Transform target = FindOrbitTarget();
        if (target == null)
        {
            Debug.LogWarning("SceneFlyCamera: 找不到环绕目标(玩家LookAtPoint)");
            return;
        }

        orbitTarget = target;
        orbitMode = true;

        // 按当前相机相对玩家的方位确定起始环绕角，保证是"平滑地来到玩家身边"
        Vector3 toCam = flyCam.position - target.position;
        toCam.y = 0f;
        orbitAngle = toCam.sqrMagnitude > 0.0001f ? Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg : 0f;
        orbitRadius = defaultOrbitRadius;
        orbitHeight = defaultOrbitHeight;
        orbitDir = 1;
    }

    /// <summary>
    /// 退出环绕：停留在原地，恢复自由飞行(用当前朝向衔接鼠标视角)
    /// </summary>
    void ExitOrbitMode()
    {
        orbitMode = false;
        InitYawPitchFromCam();
    }

    /// <summary>
    /// 环绕更新：
    /// W/S调整半径(W靠近/S拉远)，Q/E调整高度(E升/Q降)，
    /// A/D切换环绕方向，Shift/Ctrl调整角速度。
    /// </summary>
    void OrbitUpdate()
    {
        if (orbitTarget == null)
        {
            orbitMode = false;
            return;
        }

        float angSpeed = orbitAngularSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            angSpeed *= fastMultiplier;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            angSpeed *= slowMultiplier;

        // W/S：半径
        if (Input.GetKey(KeyCode.W)) orbitRadius -= orbitRadiusSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) orbitRadius += orbitRadiusSpeed * Time.deltaTime;
        // Q/E：高度
        if (Input.GetKey(KeyCode.E)) orbitHeight += orbitHeightSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) orbitHeight -= orbitHeightSpeed * Time.deltaTime;
        // A/D：切换环绕方向
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)) orbitDir *= -1;

        orbitRadius = Mathf.Max(0.3f, orbitRadius);
        orbitHeight = Mathf.Clamp(orbitHeight, -200f, 500f);

        // 自动圆周运动：以LookAtPoint为圆心
        orbitAngle += orbitDir * angSpeed * Time.deltaTime;
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 center = orbitTarget.position;
        flyCam.position = new Vector3(
            center.x + Mathf.Sin(rad) * orbitRadius,
            center.y + orbitHeight,
            center.z + Mathf.Cos(rad) * orbitRadius);

        // 主相机始终看向LookAtPoint
        flyCam.rotation = Quaternion.LookRotation(center - flyCam.position, Vector3.up);
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
