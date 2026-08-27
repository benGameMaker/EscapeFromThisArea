using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteraction : MonoBehaviour
{
    public float distance;
    public float interactionDistance;
    public LayerMask layers;


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
    }

    void Update()
    {
        if (mainCameraPos == null) return;

        Ray ray = new Ray(mainCameraPos.position, mainCameraPos.forward);

        // ---- 调试绘制 ----
        // 先绘制一条到最大距离的黄色虚线（表示最大范围）
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.yellow);

        if (Physics.Raycast(ray, out RaycastHit hit, distance, ~layers))
        {
            // 命中：绘制绿色实线到命中点
            Debug.DrawLine(ray.origin, hit.point, Color.blue);
            // 在命中点绘制一个红色小十字（方便观察精确位置）
            Debug.DrawLine(hit.point - Vector3.up * 0.2f, hit.point + Vector3.up * 0.2f, Color.red);
            Debug.DrawLine(hit.point - Vector3.right * 0.2f, hit.point + Vector3.right * 0.2f, Color.red);

            RayPointAndShowInteraction(hit.collider.gameObject);
        }
        else
        {
            // 未命中：绘制红色实线到最大距离
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);

            if (bi != null)
            {
                bi.HideInteractionPanel();
                bi = null;
            }
        }
    }

    void RayPointAndShowInteraction(GameObject item)
    {
        if (item.CompareTag("Item"))
        {
            if (bi != null)
            {
                float dist = Vector3.Distance(bi.transform.position, item.transform.position);
                if (bi.name != item.name || dist > interactionDistance)
                {
                    bi.HideInteractionPanel();
                }
            }
            bi = item.GetComponent<BaseInteractivity>();
            if(bi != null)
            {
                bi.ShowInteractionPanel();
            }
            else
            {
                Debug.Log("检测到的物体(" + item.name + ") 没有面板");
            }
        }
        else
        {
            if(bi != null)
            {
                bi.HideInteractionPanel();
                Debug.Log("检测到的物体(" + item.name + ")不再是Item，它是: " + item.tag + "，隐藏面板");
            }
            Debug.Log("检测到的物体(" + item.name + ")不是Item，它是: " + item.tag);
        }
    }


}
