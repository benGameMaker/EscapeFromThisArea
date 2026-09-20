using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneMgr : MonoBehaviour
{
    static SceneMgr instance;
    public static SceneMgr Instance => instance;
    public GameObject testObject;
    public BaseItemObjData Itme1;
    public BaseItemObjData Itme2;
    public BaseItemObjData Itme3;
    public BaseItemObjData Itme4;
    public BaseItemObjData Itme5;
    public PlayerController player;
    public BagLogicProcess pblp;
    public BagLogicProcess vblp;
    
    private void Awake()
    {
        instance = this;
        UIManager.Instance.ShowPanel<PlayerPanel>();
        UIManager.Instance.ShowPanel<BagPanel>(true);
        UIManager.Instance.ShowPanel<PromptPanel>(true);
        Debug.Log("初始化UI");
    }
    void Start()
    {
        if (player == null) Debug.Log("玩家物体未初始化");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // 场景经由异步加载进入时，环境光可能未被重新计算导致画面整体偏暗，这里主动刷新一次
        DynamicGI.UpdateEnvironment();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            player.Injured(player, 1f);
            //testObject.GetComponent<VehicleDamage>().DamageVehicle(20f);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            testObject.GetComponent<VehicleDamage>().RepairVehicle(10);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (UIManager.Instance.GetPanel<GameSettingPanel>() != null) UIManager.Instance.HidePanel<GameSettingPanel>();
            else UIManager.Instance.ShowPanel<GameSettingPanel>();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(Itme1);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(Itme2);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(Itme3);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(Itme4);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            UIManager.Instance.GetPanel<BagPanel>().AddItem(Itme5);
        }
    }
}
