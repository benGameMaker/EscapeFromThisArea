using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RayPointAndShowInteraction : MonoBehaviour
{
    public float debugGreenLineOffset;
    public float rayDistance;
    public float interactionDistance;
    public LayerMask ignoreLayers;

    public bool drawDebugRay;
    public bool lookAtPosFixedInEndOfRay;

    public Transform lookAtPos;


    BaseInteractivity bi;

    Transform mainCameraPos;
    void Awake()
    {
        UIManager.Instance.ShowPanel<InteractionPanel>();
        if (UIManager.Instance.GetPanel<InteractionPanel>() == null) Debug.LogWarning("InteractionPanel初始化失败！！！");
    }

    void Start()
    {
        mainCameraPos = Camera.main.GetComponent<Transform>();
        if (lookAtPos == null) Debug.LogError("lookAtPos未被赋值");
    }

    void Update()
    {
        if (mainCameraPos == null) return;

        Ray ray = new Ray(mainCameraPos.position, mainCameraPos.forward);

        // ---- 调试绘制 ----
        // 绘制一条到最大距离的黄色线（表示最大范围）
        // 绘制一条绿色线表示可交互最大距离
        if (drawDebugRay)
        {
            Debug.DrawRay(ray.origin + Vector3.up * debugGreenLineOffset, ray.direction * interactionDistance, Color.green);
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.yellow);
        }

        // 本帧检测到的可交互目标(未检测到则为null)
        BaseInteractivity detected = null;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~ignoreLayers, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay)
            {
                // 命中：绘制绿色实线到命中点
                Debug.DrawLine(ray.origin, hit.point, Color.blue);
                // 在命中点绘制一个红色小十字（方便观察精确位置）
                Debug.DrawLine(hit.point - Vector3.up * 0.2f, hit.point + Vector3.up * 0.2f, Color.red);
                Debug.DrawLine(hit.point - Vector3.right * 0.2f, hit.point + Vector3.right * 0.2f, Color.red);
            }
            if (lookAtPosFixedInEndOfRay) lookAtPos.position = hit.point;

            // 命中点与玩家的距离在交互范围内才算可交互目标
            if (Vector3.Distance(hit.point, transform.position) <= interactionDistance)
                detected = hit.collider.GetComponentInParent<BaseInteractivity>();
        }
        else
        {
            // 未命中：绘制红色实线到最大距离
            if(drawDebugRay) Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);
            if (lookAtPosFixedInEndOfRay) lookAtPos.position = mainCameraPos.position + mainCameraPos.forward * rayDistance;
        }

        RefreshInteractionTarget(detected);
    }

    // 统一刷新当前交互目标与交互面板：
    // 目标发生变化(含被拾取销毁)时，无论旧组件是否还有效，都必须先把面板隐藏，
    // 否则物品被Destroy后 bi 变成"已销毁引用"，bi!=null 为false会导致面板残留在屏幕上
    void RefreshInteractionTarget(BaseInteractivity current)
    {
        // 同一个仍然有效的目标：保持不变
        if (bi != null && bi == current) return;

        if (bi != null)
        {
            bi.HideInteractionPanel();
        }
        else
        {
            // 旧目标已被销毁(拾取)或不存在时，直接通过UIManager隐藏面板
            InteractionPanel panel = UIManager.Instance.GetPanel<InteractionPanel>();
            if (panel != null && panel.isShow) panel.HideMe(() => { });
        }

        bi = current;
        if (bi != null) bi.ShowInteractionPanel();
    }
    //"( •ᴥ• )"
    public void Interaction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (bi == null)
            {
                Debug.Log("没有可以交互的物体");
                return;
            }

            bi.InteractionBehavior(GetComponent<PlayerController>());

            // 交互后物品可能已被拾取销毁(InteractionPanel停止跟随)，立即隐藏面板并清空引用，
            // 避免面板停留在拾取瞬间的位置直到下一次检测到新目标
            InteractionPanel panel = UIManager.Instance.GetPanel<InteractionPanel>();
            if (panel != null && panel.isShow) panel.HideMe(() => { });
            bi = null;

            if (gameObject.CompareTag("Bullet"))
            {
                UIManager.Instance.GetPanel<PlayerPanel>().ShowCapacityDataToUI(SceneMgr.Instance.player.curWeapon);
            }
        }
    }
}
