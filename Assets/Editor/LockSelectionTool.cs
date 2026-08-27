using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class LockSelectionTool
{
    //你要固定保持选中的物体
    private static GameObject _lockTarget;

    static LockSelectionTool()
    {
        //订阅选择变化回调
        Selection.selectionChanged += OnSelectionChanged;
    }

    public static void SetLockTarget(GameObject go)
    {
        _lockTarget = go;
    }

    public static void ClearLock()
    {
        _lockTarget = null;
    }

    private static void OnSelectionChanged()
    {
        if (_lockTarget == null) return;

        //如果当前选中集合不包含目标物体，强制改回来
        if (!Selection.Contains(_lockTarget))
        {
            //强制选中目标
            Selection.activeGameObject = _lockTarget;
        }
    }

    //测试菜单：Tools下面快速设置锁定
    [MenuItem("Tools/锁定当前选中物体")]
    static void LockCurrentSelect()
    {
        if (Selection.activeGameObject != null)
        {
            SetLockTarget(Selection.activeGameObject);
            Debug.Log($"锁定物体：{_lockTarget.name}");
        }
    }

    [MenuItem("Tools/取消锁定选择")]
    static void UnLock()
    {
        ClearLock();
        Debug.Log("取消锁定");
    }
}