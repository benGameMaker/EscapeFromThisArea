using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    static SceneManager instance;
    public static SceneManager Instance => instance;
    public BaseObject testObject;
    public BaseObject atker;
    private void Awake()
    {
        instance = this;
        UIManager.Instance.ShowPanel<PlayerPanel>();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            testObject.Injured(testObject, 1f);
        }
    }
}
