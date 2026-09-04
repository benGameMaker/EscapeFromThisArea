using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    static SceneManager instance;
    public static SceneManager Instance => instance;
    public GameObject testObject;
    public BaseObject atker;
    public PlayerController player;
    private void Awake()
    {
        instance = this;
        UIManager.Instance.ShowPanel<PlayerPanel>();
        UIManager.Instance.ShowPanel<BagPanel>(true);
        UIManager.Instance.ShowPanel<PromptPanel>(true);
    }
    void Start()
    {
        if (player == null) Debug.Log("玩家物体未初始化");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            //testObject.Injured(testObject, 1f);
            testObject.GetComponent<VehicleDamage>().DamageVehicle(20f);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            testObject.GetComponent<VehicleDamage>().RepairVehicle(10);
        }
    }
}
