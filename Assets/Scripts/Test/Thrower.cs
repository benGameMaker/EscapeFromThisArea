using UnityEngine;

public class Thrower : MonoBehaviour
{

    /// <summary>
    /// 执行投掷操作
    /// </summary>
    /// <param name="power">投掷力的大小</param>
    /// <param name="pitchOffset">绕X轴的偏转角度（上下偏转），单位：度</param>
    /// <param name="yawOffset">绕Y轴的偏转角度（左右偏转），单位：度</param>
    public void Throw(float power, float pitchOffset, float yawOffset)
    {
        // 1. 从 Resources 文件夹加载预制体（假设预制体名为 "ThrowableObject"）
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Wep_Grenade");
        if (prefab == null)
        {
            Debug.LogError("未能在 Resources 文件夹中找到名为 'Wep_Grenade' 的预制体！");
            return;
        }

        // 2. 实例化预制体（位置和旋转与当前物体保持一致）
        GameObject projectile = Instantiate(prefab, transform.position, transform.rotation);

        // 3. 获取 Rigidbody 组件（如果预制体没有，则添加一个）
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }

        // 4. 计算投掷方向
        // 获取当前物体的局部 Z 轴方向（即 transform.forward）
        Vector3 baseDirection = transform.forward;

        // 应用偏转：先绕 Y 轴旋转（左右偏转），再绕 X 轴旋转（上下偏转）
        // 注意：这里使用局部旋转，将世界方向转换到局部空间处理，更符合直觉
        Vector3 localForward = transform.InverseTransformDirection(baseDirection);
        Quaternion rotationOffset = Quaternion.Euler(pitchOffset, yawOffset, 0);
        Vector3 finalLocalDirection = rotationOffset * localForward;
        Vector3 finalWorldDirection = transform.TransformDirection(finalLocalDirection).normalized;

        // 5. 施加力
        rb.AddForce(finalWorldDirection * power, ForceMode.Impulse);
    }
}