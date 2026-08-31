using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleDamage : MonoBehaviour
{
    public float curIntegrity;
    public float maxIntegrity = 100f;
    public bool isBroke = false;

    float curVelocity;
    [HideInInspector]
    public VehiclePanel vp;
    Rigidbody rb;

    private void Start()
    {
        curIntegrity = maxIntegrity;
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (vp != null && vp.isShow) vp.UpdataCurVelocity(transform.InverseTransformDirection(rb.velocity).z);
        else Debug.Log("未获取到VehiclePanel面板或者该面板已经隐藏");
    }

    public void RepairVehicle(int num)
    {
        curIntegrity += num * Random.Range(0.2f, 2f);
    }
    public void DamageVehicle(float value)
    {
        curVelocity -= value;
        vp.UpdataCurIntegrity(curIntegrity, maxIntegrity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        float damageCoefficientt;
        if (collision.gameObject.CompareTag("Zombie")) damageCoefficientt = 2f;
        else damageCoefficientt = 5f;
        if (curVelocity > 50) DamageVehicle(damageCoefficientt * 2f);
        else if (curVelocity > 30f) DamageVehicle(damageCoefficientt * 1f);
        else if (curVelocity > 20f) DamageVehicle(damageCoefficientt * 0.6f);
    }
}
