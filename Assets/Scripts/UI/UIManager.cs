using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    static UIManager instance = new UIManager();
    public static UIManager Instance => instance;
    public Transform canvasTrans;
    //面板容器
    Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();
    UIManager()
    {
        GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Canvas"));
        canvasTrans = canvas.transform;
        GameObject.DontDestroyOnLoad(canvas);
    }

    public T ShowPanel<T>() where T : BasePanel
    {
        //在容器中面板的下标于面板名保持一致
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName)) return panelDic[panelName] as T;

        GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/UI/" + panelName));
        panelObj.transform.SetParent(canvasTrans, false);

        T panel = panelObj.GetComponent<T>();
        panelDic.Add(panelName, panel);
        panel.ShowMe();

        return panel;
    }
      
    //参数的作用是是否要面板在显示和隐藏时有淡入淡出的效果
    public void HidePanel<T>(bool isFade = true) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName))
        {
            if (isFade)
            {
                panelDic[panelName].HideMe(() =>
                {
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                GameObject.Destroy(panelDic[panelName].gameObject);
                panelDic.Remove(panelName);

            }
        }

    }

    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName)) return panelDic[panelName] as T;

        return null;
    }
}
