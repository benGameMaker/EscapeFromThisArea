using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BaseObject : MonoBehaviour
{
    [HideInInspector]
    public float HP;
    public float maxHP = 100f;
    public float atk = 10f;
    [Tooltip("物体转身速度")]
    public float rotationSpeed = 5f;
    public Vector3 floatingDamageOffSet;
    [Tooltip("受伤时会浮现出该物体受到的伤害数值")]
    public bool floatingDamage = false;
    [Tooltip("受伤飘字对象池的默认容量")]
    public int floatingDamagePoolDefaultCapacity = 4;
    [Tooltip("受伤飘字对象池的最大容量")]
    public int floatingDamagePoolMaxSize = 20;

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
    // 受伤飘字对象池，结构与 WeaponControl 的子弹对象池一致
    ObjectPool<GameObject> floatingDamagePool;

    protected virtual void Awake()
    {
        HP = maxHP;
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning(gameObject.name + " animator未获取，请检查Animator组件是否挂载");
        rb=GetComponent<Rigidbody>();
        if (rb == null) Debug.LogWarning(gameObject.name + "未挂载Rigidbody");

        // 从 UIManager 单例获取画布组件
        Transform uiCanvasTrans = UIManager.Instance.canvasTrans;
        canvasRt = uiCanvasTrans.GetComponent<RectTransform>();
        canvas = uiCanvasTrans.GetComponent<Canvas>();
        floatingDamagePre = Resources.Load<GameObject>("Prefabs/UI/FloatingDamage");
        if (floatingDamagePre == null)
        {
            Debug.LogWarning("FloatingDamage 预制体未找到：Resources/Prefabs/UI/FloatingDamage 路径是否正确？");
        }
        else if (floatingDamagePre.GetComponent<FloatingDamage>() == null)
        {
            // 预制体缺少飘字脚本时不允许建池，避免取出后空引用
            Debug.LogWarning("FloatingDamage 预制体未挂载 FloatingDamage 脚本，飘字对象池不会创建。");
            floatingDamagePre = null;
        }
        else
        {
            floatingDamagePool = new ObjectPool<GameObject>(
                CreateFloatingDamage,
                OnFloatingDamageGet,
                OnFloatingDamageRelease,
                OnFloatingDamageDestroy,
                collectionCheck: true,
                floatingDamagePoolDefaultCapacity,
                floatingDamagePoolMaxSize
            );
        }
    }

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
        if(isDead) return;
        HP -= harm;
        if (HP <= 0)
        {
            HP = 0;
            Death();
        }
        if (floatingDamage && floatingDamagePool != null)
        {
            GameObject obj = floatingDamagePool.Get();
            FloatingDamage fd = obj.GetComponent<FloatingDamage>();
            // 生成失败（如初始化失败）立即归还，避免对象一直激活无法回收；飘字会持续跟随本物体的屏幕位置
            if (!fd.CreateFloatingDamageText(harm, transform, transform.position + floatingDamageOffSet, Vector2.up, 100f, 0.5f))
                floatingDamagePool.Release(obj);
        }
    }

    public virtual void Treated(float heal)
    {
        HP += heal;
        if (HP > maxHP) HP = maxHP;
    }
    protected virtual void Move()
    {
        if (isDead || !animator.enabled) return;
    }

    #region 受伤飘字对象池回调（与子弹对象池相同的结构）

    GameObject CreateFloatingDamage()
    {
        GameObject obj = Instantiate(floatingDamagePre, canvasRt, false);
        FloatingDamage fd = obj.GetComponent<FloatingDamage>();
        // 注入画布/相机依赖与回收回调，整个生命周期只需一次
        fd.InjectDependencies(canvas, canvasRt, Camera.main);
        fd.releaseCallback = ReleaseFloatingDamage;
        obj.SetActive(false);
        return obj;
    }

    void OnFloatingDamageGet(GameObject obj)
    {
        obj.SetActive(true);
    }

    void OnFloatingDamageRelease(GameObject obj)
    {
        // 停止可能仍在运行的飘移协程，以防将动画带进对象池
        FloatingDamage fd = obj.GetComponent<FloatingDamage>();
        if (fd != null) fd.StopAllCoroutines();
        obj.SetActive(false);
    }

    void OnFloatingDamageDestroy(GameObject obj)
    {
        Destroy(obj);
    }

    /// <summary>
    /// 供 FloatingDamage 在飘字结束后回收自身到对象池。
    /// 若持有者已销毁，直接销毁该飘字。
    /// </summary>
    public void ReleaseFloatingDamage(GameObject obj)
    {
        if (floatingDamagePool != null)
            floatingDamagePool.Release(obj);
        else if (obj != null)
            Destroy(obj);
    }

    #endregion

    protected virtual void OnDestroy()
    {
        // 销毁池中空闲的飘字实例；仍在播放的飘字在结束时走 ReleaseFloatingDamage 的兜底销毁
        if (floatingDamagePool != null)
        {
            floatingDamagePool.Clear();
            floatingDamagePool = null;
        }
    }

    protected virtual void Death()
    {
        if (isDead) return;   // 防止重复触发死亡逻辑
        isDead = true;
        if (animator != null)
            animator.SetBool("isDead_b", true);
        foreach (Collider part in BodyParts) part.enabled = false;
        rb.isKinematic = true;
        Debug.Log(gameObject.name + "已死亡");
    }
}
