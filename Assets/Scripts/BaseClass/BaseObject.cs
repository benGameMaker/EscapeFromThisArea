using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseObject : MonoBehaviour
{
    [HideInInspector]
    public float HP;
    public float maxHP = 100f;
    public float atk = 10f;
    [Tooltip("基于动画自带的速度改变物体移速的倍率")]
    public float moveSpeedMultiplier = 1;
    [Tooltip("物体转身速度")]
    public float rotationSpeed = 5f;
    public Vector3 floatingDamageOffSet;
    [Tooltip("受伤时会浮现出该物体受到的伤害数值")]
    public bool floatingDamage = false;

    [HideInInspector]
    public Vector2 moveVector;
    [HideInInspector]
    public Animator animator;

    public bool isDead = false;
    protected List<Collider> BodyParts = new List<Collider>();
    protected Rigidbody rb;

    Canvas canvas;
    RectTransform canvasRt;
    GameObject floatingDamagePre;

    protected virtual void Awake()
    {
        HP = maxHP;
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning(gameObject.name + " animator未获取，请检查Animator组件是否挂载");
        rb=GetComponent<Rigidbody>();
        if (rb == null) Debug.LogWarning(gameObject.name + "未挂载Rigidbody");

        // 场景中不再常驻 Canvas：改为运行时通过 UIManager 单例获取
        //（首次访问 UIManager 时若场景没有 Canvas 会自动创建并 DontDestroyOnLoad）
        Transform uiCanvasTrans = UIManager.Instance.canvasTrans;
        canvasRt = uiCanvasTrans.GetComponent<RectTransform>();
        canvas = uiCanvasTrans.GetComponent<Canvas>();
        floatingDamagePre = Resources.Load<GameObject>("Prefabs/UI/FloatingDamage");
        if (floatingDamagePre == null)
            Debug.LogWarning("FloatingDamage 预制体未找到：Resources/Prefabs/UI/FloatingDamage 路径是否正确？");
    }

    void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {

    }

    public virtual void Injured(BaseObject attacker, float extraHarm)
    {
        if (attacker == null)
        {
            Debug.Log("攻击对象为空");
            return;
        }
        Debug.Log(gameObject.name + "受到了" + attacker.gameObject.name);
        Injured(attacker.atk * extraHarm);
    }

    // 只传入伤害数值的受伤方法，直接扣除 harm 点 HP
    public virtual void Injured(float harm)
    {
        HP -= harm;
        if (HP <= 0)
        {
            HP = 0;
            Death();
        }
        if (floatingDamage)
        {
            GameObject obj = Instantiate(floatingDamagePre, canvasRt, false);
            FloatingDamage fd = obj.GetComponent<FloatingDamage>();
            if (fd == null)
            {
                Debug.LogWarning("FloatingDamage 预制体未挂载 FloatingDamage 脚本，跳过飘字生成。");
                Destroy(obj);
                return;
            }
            fd.InjectDependencies(canvas, canvasRt, Camera.main);
            fd.CreateFloatingDamageText(harm, transform.position + floatingDamageOffSet, Vector2.up, 100f, 0.5f);
        }
    }

    protected virtual void Move()
    {
        if (isDead || !animator.enabled) return;
    }
    protected virtual void Death()
    {
        if (animator != null)
            animator.SetBool("isDead_b", true);
        foreach (Collider part in BodyParts) part.enabled = false;
        rb.isKinematic = true;
        Debug.Log(gameObject.name + "已死亡");
    }
}
