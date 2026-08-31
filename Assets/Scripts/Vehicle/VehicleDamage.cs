using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleDamage : MonoBehaviour
{
    public float curIntegrity;
    public float maxIntegrity = 100f;
    public bool isBroke = false;

    [HideInInspector]
    public float curVelocity;
    [HideInInspector]
    public VehiclePanel vp;
    [HideInInspector]
    public VehicleController vc;
    Rigidbody rb;

    private void Start()
    {
        curIntegrity = maxIntegrity;
        rb = GetComponent<Rigidbody>();
        vc = GetComponent<VehicleController>();
    }

    private void FixedUpdate()
    {
        curVelocity = transform.InverseTransformDirection(rb.velocity).z;
        if (vp != null && vp.isShow) vp.UpdataCurVelocity(curVelocity);
        else Debug.Log("未获取到VehiclePanel面板或者该面板已经隐藏");
    }

    public void RepairVehicle(int num)
    {
        curIntegrity += num * Random.Range(0.2f, 2f);
        Debug.Log("车辆恢复了" + curIntegrity + "完整度");
        if (curIntegrity > maxIntegrity) curIntegrity = maxIntegrity;
        vp.UpdataCurIntegrity(curIntegrity, maxIntegrity);
        if (curIntegrity >= 80f && isBroke)
        {
            isBroke = false;
            vc.SetVehicleState(!isBroke);
            vp.ShowOrHideNotice(isBroke);
            Debug.Log("车辆车辆已基本修复");

        }
    }
    public void DamageVehicle(float value)
    {
        curIntegrity -= value;
        Debug.Log("这次撞击损失了" + value + "车辆完整度");
        vp.UpdataCurIntegrity((curIntegrity <= 0 ? 0 : curIntegrity), maxIntegrity);
        if (curIntegrity <= 0)
        {
            curIntegrity = 0;
            isBroke = true;
            vc.SetVehicleState(!isBroke);
            vp.ShowOrHideNotice(isBroke);
            Debug.Log("车辆已损坏");
            return;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;
        Debug.Log("车辆碰撞了" + collision.gameObject.name + "，此时速度为：" + curVelocity);
        float damageCoefficientt;
        if (collision.gameObject.CompareTag("Zombie")) damageCoefficientt = 2f;
        else damageCoefficientt = 5f;
        if (Mathf.Abs(curVelocity) > 20f) DamageVehicle(damageCoefficientt * 0.8f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 15f) DamageVehicle(damageCoefficientt * 0.5f * curVelocity);
        else if (Mathf.Abs(curVelocity) > 5f) DamageVehicle(damageCoefficientt * 0.3f * curVelocity);
    }
}
