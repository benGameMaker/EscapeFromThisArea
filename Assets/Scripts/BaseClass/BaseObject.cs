using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseObject : MonoBehaviour
{
    public float hp = 100;
    public float atk = 10;
    [Tooltip("基于动画自带的速度改变物体移速的倍率")]
    public float moveSpeedMultiplier = 1;
    [Tooltip("物体转身速度")]
    public float rotationSpeed = 5f;
    [Tooltip("受伤时会浮现出该物体受到的伤害数值")]
    public bool floatingDamage = false;
    public Vector3 floatingDamageOffSet;

    [HideInInspector]
    public Vector2 moveVector;
    [HideInInspector]
    public Animator animator;

    public bool isDead = false;
    protected BoxCollider myBox;
    protected Rigidbody rb;

    Canvas canvas;
    RectTransform canvasRt;
    GameObject floatingDamagePre;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError(gameObject.name + " animator未获取，请检查Animator组件是否挂载");

        myBox = GetComponent<BoxCollider>();
        if (myBox == null) Debug.Log(gameObject.name + "未挂载BoxCollider");
        rb=GetComponent<Rigidbody>();
        if (rb == null) Debug.Log(gameObject.name + "未挂载Rigidbody");

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

    public virtual void Injured(BaseObject attacker,float extraHarm)
    {
        hp -= attacker.atk * extraHarm;
        Debug.Log(gameObject.name + "受到了" + attacker.gameObject.name);
        if (hp <= 0)
        {
            hp = 0;
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
            fd.CreateFloatingDamageText(attacker.atk * extraHarm, transform.position + floatingDamageOffSet, Vector2.up, 100f, 0.5f);
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
        myBox.enabled = false;
        rb.isKinematic = true;
        Debug.Log(gameObject.name + "已死亡");
    }
}
