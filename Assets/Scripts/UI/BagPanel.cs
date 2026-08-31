using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagPanel : BasePanel
{
    public Button btnExit;
    public PlayerController pc;
    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<BagPanel>();
            //Cursor.lockState = CursorLockMode.Locked;
            //pc.enableMouseInput(true);
            //Cursor.visible = false;
        });
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
}
