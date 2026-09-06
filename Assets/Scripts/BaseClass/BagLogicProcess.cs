using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 背包开关与输入管理，需要把它挂载到每个“可操作物体”上（即带PlayerInput的物体，如玩家、车辆）。
/// - 所需数据都从本物体自身获取：PlayerInput、CinemachineFreeLook、操作脚本(PlayerController/VehicleController)；
/// - 提供 OpenBag(InputAction.CallbackContext) 方法，把它关联到本物体PlayerInput的OpenBag动作即可开关背包
///   （哪个物体在被控制、哪个PlayerInput处于激活，就只会由它触发）；
/// - 提供 SetActiveInput(bool)，供上车/下车等外部流程调用，用来启用或停用本物体的PlayerInput；
/// - 打开背包时禁用本物体的鼠标视角(CinemachineFreeLook)、开火(Fire)与切枪(SwitchWeapon)，关闭时恢复。
/// </summary>
public class BagLogicProcess : MonoBehaviour
{
    PlayerInput playerInput;          //本物体上的PlayerInput组件
    CinemachineFreeLook freeLook;     //本物体控制的自由视角相机
    BagPanel bagPanel;                //背包面板(懒加载获取)
    Transform discardPoint;           //本可操作物体上的丢弃点(Inspector赋值在Player/VehicleController上)

    //丢弃点：供BagPanel丢弃物品时使用
    public Transform DiscardPoint => discardPoint;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
        //丢弃点挂在可操作物体的控制脚本上(PlayerController/VehicleController)
        PlayerController pc = GetComponent<PlayerController>();
        VehicleController vc = GetComponent<VehicleController>();
        discardPoint = pc != null && pc.DiscardPoint != null ? pc.DiscardPoint
                     : vc != null && vc.DiscardPoint != null ? vc.DiscardPoint : null;
    }

    void Start()
    {
        //初始同步：若本物体的操作脚本默认失活(如未上车的车辆)，则一开始就不监听输入
        MonoBehaviour controller = (MonoBehaviour)GetComponent<PlayerController>();
        if (controller == null) controller = GetComponent<VehicleController>();
        if (controller != null && !controller.enabled)
            SetActiveInput(false);


        freeLook.m_XAxis.m_InputAxisName = "Mouse X";
        freeLook.m_YAxis.m_InputAxisName = "Mouse Y";
    }

    //启用或停用本物体的PlayerInput（由上车/下车等外部流程调用）
    public void SetActiveInput(bool active)
    {
        if (playerInput != null) playerInput.enabled = active;
    }

    //打开/关闭背包：把本方法关联到本物体PlayerInput的OpenBag动作上
    public void OpenBag(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        BagPanel panel = GetBagPanel();
        if (panel == null) return;
        if (panel.isShow) CloseBagPanel(panel);
        else OpenBagPanel(panel);
    }

    //关闭背包并恢复输入：供背包关闭按钮等外部调用（只有打开它的那个物体需要恢复）
    public void CloseBag()
    {
        BagPanel panel = GetBagPanel();
        if (panel == null || !panel.isShow) return;
        CloseBagPanel(panel);
    }

    BagPanel GetBagPanel()
    {
        if (bagPanel == null) bagPanel = UIManager.Instance.GetPanel<BagPanel>();
        return bagPanel;
    }

    void OpenBagPanel(BagPanel panel)
    {
        //记录由本物体打开，关闭按钮据此调用本物体的CloseBag以恢复输入
        panel.bagOpener = this;
        panel.ShowMe();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        //打开背包期间禁止本物体的鼠标视角与开火
        EnableGameplayInput(false);
    }

    void CloseBagPanel(BagPanel panel)
    {
        panel.HideMe(() => { });
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //关闭背包后恢复本物体的鼠标视角与开火
        EnableGameplayInput(true);
    }

    //启用或禁用本物体的鼠标视角、开火与切枪输入
    public void EnableGameplayInput(bool enable)
    {
        //鼠标转动视角：通过清空/恢复CinemachineFreeLook的输入轴名实现
        if (freeLook != null)
        {
            freeLook.m_XAxis.m_InputAxisName = enable ? "Mouse X" : "";
            freeLook.m_YAxis.m_InputAxisName = enable ? "Mouse Y" : "";
        }

        //开火/切枪/瞄准：背包打开时禁用本物体PlayerInput中的Fire、SwitchWeapon与Aim动作，关闭时恢复
        if (playerInput != null && playerInput.actions != null)
        {
            InputAction fire = playerInput.actions.FindAction("Fire");
            if (fire != null)
            {
                if (enable) fire.Enable();
                else fire.Disable();
            }
            //玩家与车辆的切枪动作名都是SwitchWeapon
            InputAction switchWeapon = playerInput.actions.FindAction("SwitchWeapon");
            if (switchWeapon != null)
            {
                if (enable) switchWeapon.Enable();
                else switchWeapon.Disable();
            }
            //持枪时右键瞄准(玩家Aim动作)在背包打开时一并禁用；
            //注意：这里只禁用"玩法输入动作"，背包UI里右键物品的互动(EventSystem/IPointerClickHandler/Input.GetMouseButtonDown)
            //不属于InputAction，不受影响
            InputAction aim = playerInput.actions.FindAction("Aim");
            if (aim != null)
            {
                if (enable) aim.Enable();
                else aim.Disable();
            }
        }
    }
}
