using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    // 移动速度
    public float speed = 3f;
    // 鼠标灵敏度
    public float xScensitivity = 10f;
    public float yScensitivity = 10f;

    public float jumpForce = 5f;

    [HideInInspector][SerializeField]
    public bool isHighSpeed = false;
    [HideInInspector][SerializeField]
    public bool isAiming = false;

    bool isJump;
    float xRotation = 0f;
    Rigidbody rb;
    Animator ani;
    Vector3 velocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ani = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Aim();
        Mouse();
        Move();
        HighSpeed();
        Jump();
    }

    void Aim()
    {
        float aim=ani.GetFloat("Aiming");
        if (Input.GetMouseButton(1))
        {
            isAiming = true;
            ani.SetBool("Aim", true);
            ani.SetFloat("Aiming", Mathf.Lerp(aim, 1f,0.1f));
        }
        else
        {
            isAiming = false;
            ani.SetBool("Aim", false);
            ani.SetFloat("Aiming", Mathf.Lerp(aim, 0f,0.1f));
        }
    }

    void Mouse()
    {
        // 获取输入
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
       // Debug.Log("Mouse X: " + x + ", Mouse Y: " + y);
        // 上下旋转
        xRotation += y * yScensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        ani.transform.localRotation = Quaternion.Euler(-xRotation, 0f, 0f);
        // 左右旋转
        transform.Rotate(Vector3.up * x * xScensitivity);

    }

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 dir =(transform.right * horizontal + transform.forward * vertical).normalized;
        velocity = dir * speed;
        velocity.y = rb.velocity.y;

        ani.SetFloat("Movement", dir.magnitude);
    }
    
    void HighSpeed()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isGrounded()) 
        {
            isHighSpeed = true;
            speed = 6f;
            ani.SetBool("Holstered", true);
        }
        else
        {
            isHighSpeed = false;
            speed = 3f;
            ani.SetBool("Holstered", false);
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded())
        {
            isJump = true;
        }
    } 

    public bool isGrounded()
    {
        RaycastHit hit;
        return Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down,
                out hit, 0.4f,LayerMask.GetMask("Ground"));
    }

    private void FixedUpdate()
    {
        if (isJump)
        {
            isJump = false;
            velocity.y = jumpForce;
        }
        rb.velocity = velocity;
    }
}
