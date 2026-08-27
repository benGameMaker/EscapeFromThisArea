using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManagement : MonoBehaviour
{
    public BaseObject testObject;
    public BaseObject atker;
    void Start()
    {
        
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            testObject.Injured(testObject, 1f);
        }
    }
}
