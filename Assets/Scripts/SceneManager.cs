using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public BaseObject testObject;
    public BaseObject atker;
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
