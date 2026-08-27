using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ColliderProcessor : MonoBehaviour
{
    [HideInInspector]
    public Animator animator;
    public float jumpForce = 5f;


    Rigidbody rb;

    Transform bodyPos;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInParent<Animator>();

        bodyPos = transform.Find("Hips_jnt");

        if (animator == null) Debug.LogError(gameObject.name+"未获取到animator脚本");
    }

    private void FixedUpdate()
    {
    }
    Vector3 origin;
    public Vector3 offset;
    public Vector3 direction;
    public float maxDistance;
    void Update()
    {
        origin = transform.position + offset;
        direction = Vector3.down;
        maxDistance = 1.5f;

        // 绘制射线
        Debug.DrawRay(origin, direction * maxDistance, Color.red);

        // 同时执行物理检测
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            // 命中时在命中点画一个十字标记
            Debug.DrawLine(hit.point - Vector3.right * 0.2f, hit.point + Vector3.right * 0.2f, Color.yellow);
            Debug.DrawLine(hit.point - Vector3.forward * 0.2f, hit.point + Vector3.forward * 0.2f, Color.yellow);
        }
        bool grounded = isGrounded(origin, direction, maxDistance);
        animator.SetBool("Grounded", grounded);
    }

    public void Jump()
    {

        if (!isGrounded(origin, direction, maxDistance)) return;
        animator.SetTrigger("Jump_t");
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    public void Move(float f)
    {
        if (isGrounded(origin, direction, maxDistance))
        {
            animator.SetFloat("Speed_f", f);
        }
    }

    public bool isGrounded(Vector3 origin, Vector2 direction, float maxDistance)
    {
        RaycastHit hit;
        bool f = Physics.Raycast(origin, direction, out hit, maxDistance);
        //if (hit.collider == null) Debug.Log("未接触物体表面");
        //else Debug.Log("踩在" + hit.collider.name + "表面");
        return f;
    }

    private void OnCollisionEnter(Collision collision)
    {
    }

    private void OnCollisionExit(Collision collision)
    {
    }

}
