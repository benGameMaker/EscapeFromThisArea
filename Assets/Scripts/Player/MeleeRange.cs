using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeRange : MonoBehaviour
{
    public float harmMulitpler = 1f;

    BaseObject executor;
    BoxCollider MeleeRangeBox;
    Coroutine activeCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        executor = GetComponentInParent<BaseObject>();
        MeleeRangeBox = GetComponent<BoxCollider>();

        if (MeleeRangeBox != null)
        {
            MeleeRangeBox.enabled = false;
            MeleeRangeBox.isTrigger = true;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TriggerMeleeAtk(float time)
    {
        if (MeleeRangeBox == null) return;

        // 重新激活前先取消旧的失活协程，避免连续调用导致提前失活
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
        MeleeRangeBox.enabled = true;
        activeCoroutine = StartCoroutine(DisableAfterSeconds(time));
    }

    IEnumerator DisableAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);
        if (MeleeRangeBox != null)
            MeleeRangeBox.enabled = false;
        activeCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseObject obj = other.GetComponentInParent<BaseObject>();
        Debug.Log("攻击到" + other.name);
        if (other.CompareTag(gameObject.tag) || obj == null)
        {
            Debug.Log("刀到了" + gameObject.tag + "||" + gameObject.name);
            return;
        }

        Debug.Log(other.gameObject.name + "被" + gameObject.name + "攻击了");
        obj.Injured(executor, harmMulitpler);
    }
}
