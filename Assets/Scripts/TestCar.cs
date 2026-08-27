using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCar : MonoBehaviour
{
    public WheelCollider flWheel;
    public WheelCollider frWheel;
    public WheelCollider rlWheel;
    public WheelCollider rrWheel;

    public Transform flWheelModel;
    public Transform frWheelModel;
    public Transform rlWheelModel;
    public Transform rrWheelModel;

    public float motorTorque;
    public float brakeTorque;
    public float maxSteeringAngle;

    Vector2 inputAxis;

    void Start()
    {
        
    }

    void Update()
    {
        HandAxisInput();
        UpdataWheelModelsRot();
    }
    private void FixedUpdate()
    {
        Move();
        Brake();
        Turn();
    }



    void Move()
    {
        rlWheel.motorTorque = motorTorque * inputAxis.y;
        rrWheel.motorTorque = motorTorque * inputAxis.y;
    }
    void HandAxisInput()
    {
        inputAxis.x = Input.GetAxis("Horizontal");
        inputAxis.y = Input.GetAxis("Vertical");
    }
    void Brake()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            flWheel.brakeTorque = brakeTorque;
            frWheel.brakeTorque = brakeTorque;
            rlWheel.brakeTorque = brakeTorque;
            rrWheel.brakeTorque = brakeTorque;
        }
        else
        {
            flWheel.brakeTorque = 0;
            frWheel.brakeTorque = 0;
            rlWheel.brakeTorque = 0;
            rrWheel.brakeTorque = 0;
        }
    }
    void Turn()
    {
        flWheel.steerAngle = maxSteeringAngle * inputAxis.x;
        frWheel.steerAngle = maxSteeringAngle * inputAxis.x;
    }
    void UpdataWheelModelsRot()
    {
        UpdataWheelModelRot(flWheelModel, flWheel);
        UpdataWheelModelRot(frWheelModel, frWheel);
        UpdataWheelModelRot(rlWheelModel, rlWheel);
        UpdataWheelModelRot(rrWheelModel, rrWheel);
    }
    void UpdataWheelModelRot(Transform wheelModel,WheelCollider wheelCollider)
    {
        wheelCollider.GetWorldPose(out var pos, out var rot);
        wheelModel.transform.position = pos;
        wheelModel.transform.rotation = rot;
    }
}
