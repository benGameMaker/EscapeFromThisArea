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
    Vector3 hitPoint;

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

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~ignoreLayers))
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

            detectionInteraction(hit.collider.gameObject);
        }
        else
        {
            // 未命中：绘制红色实线到最大距离
            if(drawDebugRay) Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);
            if (lookAtPosFixedInEndOfRay) lookAtPos.position = mainCameraPos.position + mainCameraPos.forward * rayDistance;

            if (bi != null)
            {
                bi.HideInteractionPanel();
                bi = null;
            }
        }
        hitPoint = hit.point;
    }
    //"( •ᴥ• )"
    public void Interaction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (bi == null) Debug.Log("没有可以交互的物体");
            else bi.InteractionBehavior(GetComponent<PlayerController>());
            if (gameObject.CompareTag("Bullet"))
            {
                UIManager.Instance.GetPanel<PlayerPanel>().ShowCapacityDataToUI(SceneMgr.Instance.player.curWeapon);
            }
        }
    }
    void detectionInteraction(GameObject item)
    {
        if (hitPoint == Vector3.zero || Vector3.Distance(hitPoint, transform.position) > interactionDistance)
        {
            if (bi != null)
            {
                bi.HideInteractionPanel();
                bi = null;
            }
            return;
        }
        bi = item.GetComponent<BaseInteractivity>();
        if (bi != null)
        {
            bi.ShowInteractionPanel();
        }
        else
        {
            UIManager.Instance.GetPanel<InteractionPanel>().HideMe(() => { });
            //Debug.Log("检测到的物体(" + item.name + ") 没有面板");
        }
    }
}
