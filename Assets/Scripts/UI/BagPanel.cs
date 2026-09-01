using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagPanel : BasePanel
{
    public Button btnExit;
    public Transform Cells;

    public PlayerController pc;
    public List<Transform> itemCells;
    public override void Init()
    {
        btnExit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<BagPanel>();
            //Cursor.lockState = CursorLockMode.Locked;
            //pc.enableMouseInput(true);
            //Cursor.visible = false;
        });
        CollectCells();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    void CollectCells()
    {
        for(int i = 0; i < Cells.childCount; i++)
        {
            itemCells.Add(Cells.GetChild(i));
        }
    }
}
