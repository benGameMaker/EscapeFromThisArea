using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextCapacityUI : MonoBehaviour
{
    public Text CurCapacity;
    public Text Separator;
    public Text MaxCapacity;

    WeaponControl wc;
    // Start is called before the first frame update
    void Start()
    {
        if (CurCapacity == null)
        {
            Transform t = transform.Find("CurCapacity");
            if (t != null) CurCapacity = t.GetComponent<Text>();
        }
        if (Separator == null)
        {
            Transform t = transform.Find("Separator");
            if (t != null) Separator = t.GetComponent<Text>();
        }
        if (MaxCapacity == null)
        {
            Transform t = transform.Find("MaxCapacity");
            if (t != null) MaxCapacity = t.GetComponent<Text>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (wc != null && CurCapacity != null)
        {
            CurCapacity.text = wc.curCapacity.ToString();
        }
    }
    public void GetCapacityDataToUI(WeaponControl weaponControl)
    {
        wc = weaponControl;
        if (MaxCapacity != null) MaxCapacity.text = wc.capacity.ToString();
        if (CurCapacity != null) CurCapacity.text = wc.curCapacity.ToString();
    }
    public void HideUI()
    {
        if (MaxCapacity != null) MaxCapacity.text = "0";
        if (CurCapacity != null) CurCapacity.text = "0";
    }

}
