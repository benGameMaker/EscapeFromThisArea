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
        ItemDataLoadCell();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
    void ItemDataLoadCell()
    {
        GameObject itemUnitObj;
        ItemUnit itemUnit;
        foreach (CellData cell in GameDataMgr.Instance.cellDatas)
        {
            itemUnitObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/ItemUnit"));
            if (cell.cellIndex < itemCells.Count)
            {
                itemUnitObj.transform.SetParent(itemCells[cell.cellIndex], false);
            }
            else Debug.LogError(cell.itemName + "的单元格索引超出范围: " + cell.cellIndex);

            itemUnit = itemUnitObj.GetComponent<ItemUnit>();

            itemUnit.itemIcon.sprite = Resources.Load<Sprite>("Sprites/" + cell.itemIconName);
            itemUnit.itemName.text = cell.itemName;
            itemUnit.curNum.text = cell.curNum.ToString();
            itemUnit.cellIndex = cell.cellIndex;
        }
    }
    void CollectCells()
    {
        for(int i = 0; i < Cells.childCount; i++)
        {
            itemCells.Add(Cells.GetChild(i));
        }
    }
}
